module;
#include <cstdint>
#include <span>
#include <stdexcept>
#include <string>
export module tool.popcap.wwise.bnk.unpack;
import utility.io;
import tool.popcap.wwise.bnk.utils;
import tool.popcap.wwise.bnk.definition;

export namespace WwiseSoundBank::Unpack {

inline bool unpack(const std::string& inputFile, const std::string& outputFolder) {
    Detail::MappedInput mapped(inputFile);
    const auto bank = Detail::parseBnk(mapped.span());

    FileUtils::createDirectory(outputFolder);
    const auto dataFolder = FileUtils::joinPath(outputFolder, "data");
    FileUtils::createDirectory(dataFolder);

    if (!FileUtils::writeTextFile(FileUtils::joinPath(outputFolder, "definition.json"), Definition::toJsonString(bank))) {
        throw std::runtime_error("Failed to write definition.json");
    }

    if (bank.dataChunkOffset && !bank.dataIndex.empty()) {
        const uint64_t dataStart = *bank.dataChunkOffset;
        for (const auto& entry : bank.dataIndex) {
            const uint64_t begin = dataStart + static_cast<uint64_t>(entry.offset);
            const uint64_t end = begin + static_cast<uint64_t>(entry.size);
            if (end > mapped.size()) throw std::runtime_error("DATA entry out of range");
            const auto outPath = FileUtils::joinPath(dataFolder, std::to_string(entry.id) + ".wem");
            if (!FileUtils::writeFileBytes(outPath, std::span<const uint8_t>(mapped.data() + begin, entry.size))) {
                throw std::runtime_error("Failed to write WEM");
            }
        }
    }

    return true;
}

}