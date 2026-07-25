module;
#include <filesystem>
#include <string>
#include <string_view>
export module tool.popcap.wwise.media.utils;
import tool.popcap.wwise.media.core;
import tool.shell.config_manager;

export namespace WwiseMedia {

[[nodiscard]] inline std::filesystem::path packedCodebooksPath(std::string_view name) {
    const auto& config = ConfigManager::get();
    auto configured = config.getSetting(kPackedCodebooksSetting, kDefaultPackedCodebooksDir);
    std::filesystem::path path(configured);
    if (path.extension() == ".bin") return path.is_relative() ? config.getScriptDir() / path : path;
    if (path.is_relative()) path = config.getScriptDir() / path;
    return path / std::filesystem::path(name);
}

[[nodiscard]] inline std::filesystem::path temporaryOutputPath(std::string_view outputPath) {
    return std::filesystem::path(outputPath).concat(".ww2ogg.tmp");
}

}
