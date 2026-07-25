module;
#include <cstdint>
#include <cstring>
#include <cstddef>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>
export module tool.popcap.wwise.bnk.pack;
import utility.io;
import tool.popcap.wwise.bnk.core;
import tool.popcap.wwise.bnk.utils;
import tool.popcap.wwise.bnk.definition;

export namespace WwiseSoundBank::Pack {

inline bool pack(const std::string& jsonFile, const std::string& wemsFolder, const std::string& outputFile) {
    auto bank = Definition::fromJsonString(FileUtils::readTextFile(jsonFile));
    auto idsToPack = bank.embeddedMedia.empty() ? Detail::collectNumericWemIds(wemsFolder) : bank.embeddedMedia;

    std::vector<DidxEntry> didx;
    std::vector<uint8_t> dataBlob;
    didx.reserve(idsToPack.size());

    uint32_t offset = 0;
    for (const uint32_t id : idsToPack) {
        const auto wemPath = FileUtils::joinPath(wemsFolder, std::to_string(id) + ".wem");
        if (!FileUtils::fileExists(wemPath)) continue;

        auto bytes = FileUtils::readFileBytes(wemPath);
        const uint32_t size = static_cast<uint32_t>(bytes.size());
        const uint32_t padding = Detail::align16(size);
        didx.push_back({.id = id, .offset = offset, .size = size});

        const size_t oldSize = dataBlob.size();
        dataBlob.resize(oldSize + size + padding);
        if (size) std::memcpy(dataBlob.data() + oldSize, bytes.data(), size);
        offset += size + padding;
    }

    bank.dataIndex = std::move(didx);
    if (bank.embeddedMedia.empty()) bank.embeddedMedia = std::move(idsToPack);

    const auto out = Detail::buildBankBinary(bank, dataBlob);
    if (!FileUtils::writeFileBytes(outputFile, out)) throw std::runtime_error("Failed to write BNK");
    return true;
}

inline bool packDirectory(const std::string& inputFolder, const std::string& outputFile) {
    return pack(FileUtils::joinPath(inputFolder, "definition.json"), FileUtils::joinPath(inputFolder, "data"), outputFile);
}

} 