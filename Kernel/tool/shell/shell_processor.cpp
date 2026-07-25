module;
#include <algorithm>
#include <atomic>
#include <chrono>
#include <filesystem>
#include <string>
#include <string_view>
#include <unordered_set>
#include <vector>
module tool.shell.shell_processor;
import utility.io;
import tool.shell.plugin_base;
import tool.shell.core_utils;

bool ShellProcessor::executeFunction(const FunctionConfig& cfg, const PluginParamMap& params, double& elapsed) {
    auto t0 = std::chrono::high_resolution_clock::now();
    bool ok = PluginFactory::get().execute(cfg.functionId, params).success;
    auto t1 = std::chrono::high_resolution_clock::now();
    elapsed = std::chrono::duration<double>(t1 - t0).count();
    return ok;
}

BatchResult ShellProcessor::executeBatch(const FunctionConfig& cfg, std::string_view inputFolder, std::string_view outputFolder, const PluginParamMap& extraParams) {
    BatchResult res;
    if (!PluginFactory::get().has(cfg.functionId)) return res;
    auto t0 = std::chrono::high_resolution_clock::now();

    std::string normInput(inputFolder);
    while (!normInput.empty() && (normInput.back() == '/' || normInput.back() == '\\')) normInput.pop_back();

    bool inputIsFolder = false;
    std::string targetExt;
    if (!cfg.params.empty()) {
        inputIsFolder = cfg.params[0].folder;
        if (!cfg.params[0].extensions.empty()) targetExt = cfg.params[0].extensions.front();
    }

    std::vector<std::string> targets;
    if (inputIsFolder) {
        if (ShellCore::endsWithIC(normInput, targetExt)) {
            targets.push_back(normInput);
        } else if (FileUtils::isDirectory(normInput)) {
            std::error_code ec;
            for (const auto& e : std::filesystem::directory_iterator(normInput, ec))
                if (e.is_directory(ec) && ShellCore::endsWithIC(e.path().string(), targetExt))
                    targets.push_back(e.path().string());
        }
    } else {
        for (auto& f : FileUtils::collectFiles(normInput))
            if (ShellCore::endsWithIC(f, targetExt))
                targets.push_back(std::move(f));
    }

    const size_t n = targets.size();
    if (!n) return res;

    std::vector<std::string> outputs(n);
    FileUtils::createDirectory(std::string(outputFolder));
    std::unordered_set<std::string> dirs;
    dirs.reserve(n);
    const size_t prefixLen = normInput.size();

    for (size_t i = 0; i < n; ++i) {
        std::string rel;
        if (inputIsFolder && targets[i] == normInput) {
            rel = FileUtils::getFileName(targets[i]);
        } else {
            size_t skip = (targets[i].size() > prefixLen && (targets[i][prefixLen] == '/' || targets[i][prefixLen] == '\\')) ? 1 : 0;
            rel = targets[i].substr(prefixLen + skip);
        }
        outputs[i] = cfg.generateOutputPath(FileUtils::joinPath(std::string(outputFolder), rel));
        if (auto pos = outputs[i].rfind('/'); pos != std::string::npos)
            dirs.emplace(outputs[i].substr(0, pos));
        else if (auto pos2 = outputs[i].rfind('\\'); pos2 != std::string::npos)
            dirs.emplace(outputs[i].substr(0, pos2));
    }
    for (const auto& dir : dirs) FileUtils::createDirectory(dir);

    const std::string inName = cfg.params.size() > 0 ? cfg.params[0].name : "InputFile";
    const std::string outName = cfg.params.size() > 1 ? cfg.params[1].name : "OutputFile";
    std::atomic<int> okCount{0}, failCount{0};
    const size_t batchSize = 128;
    const size_t batches = (n + batchSize - 1) / batchSize;
    Latch latch(static_cast<int>(batches));
    std::string id = cfg.functionId;

    for (size_t b = 0; b < batches; ++b) {
        const size_t begin = b * batchSize;
        const size_t end = std::min(begin + batchSize, n);
        getGlobalPool().submitVoid([=, &targets, &outputs, &extraParams, &okCount, &failCount, &latch] {
            PluginParamMap p(extraParams.begin(), extraParams.end());
            for (size_t i = begin; i < end; ++i) {
                p[inName] = targets[i];
                p[outName] = outputs[i];
                if (PluginFactory::get().execute(id, p).success)
                    okCount.fetch_add(1, std::memory_order_relaxed);
                else {
                    failCount.fetch_add(1, std::memory_order_relaxed);
                    if (FileUtils::isDirectory(outputs[i])) FileUtils::deleteDirectory(outputs[i]);
                    else if (FileUtils::fileExists(outputs[i])) FileUtils::deleteFile(outputs[i]);
                }
            }
            latch.countDown();
        });
    }
    latch.wait();

    res.successCount = okCount.load(std::memory_order_relaxed);
    res.failCount = failCount.load(std::memory_order_relaxed);
    res.elapsed = std::chrono::duration<double>(std::chrono::high_resolution_clock::now() - t0).count();
    return res;
}
