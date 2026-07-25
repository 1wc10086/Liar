module;
#include "lib/ww2ogg/wwriff.h"
#include <filesystem>
#include <fstream>
#include <stdexcept>
#include <string>
#include <string_view>
export module tool.popcap.wwise.media.decode;
import tool.popcap.wwise.media.utils;
import tool.shell.js_engine;

export namespace WwiseMedia {

class Decoder {
public:
    static void decode(std::string_view inputPath, std::string_view outputPath, std::string_view codebooksName) {
        const std::filesystem::path input(inputPath);
        const std::filesystem::path output(outputPath);
        const auto codebooks = packedCodebooksPath(codebooksName);
        const auto temporary = temporaryOutputPath(outputPath);
        const auto repaired = std::filesystem::path(outputPath).concat(".revorb.tmp");

        if (!std::filesystem::is_regular_file(input)) throw std::runtime_error("WEM input file does not exist");
        if (!std::filesystem::is_regular_file(codebooks)) throw std::runtime_error("Selected ww2ogg packed codebooks file does not exist");

        std::error_code ec;
        if (const auto parent = output.parent_path(); !parent.empty()) {
            std::filesystem::create_directories(parent, ec);
            if (ec) throw std::runtime_error("Cannot create WEM output directory");
        }
        std::filesystem::remove(temporary, ec);
        std::filesystem::remove(repaired, ec);

        try {
            {
                std::ofstream ogg(temporary, std::ios::binary | std::ios::trunc);
                if (!ogg) throw std::runtime_error("Cannot create temporary OGG file");
                Wwise_RIFF_Vorbis decoder(input.string(), codebooks.string(), false, false, kNoForcePacketFormat);
                decoder.generate_ogg(ogg);
                if (!ogg) throw std::runtime_error("Cannot write temporary OGG file");
            }
            if (!JsEngine::get().revorb(temporary.string(), repaired.string()))
                throw std::runtime_error("ReVorb granule-position repair failed");
            std::filesystem::rename(repaired, output, ec);
            if (ec) throw std::runtime_error("Cannot publish decoded OGG file");
            std::filesystem::remove(temporary, ec);
        } catch (...) {
            std::filesystem::remove(temporary, ec);
            std::filesystem::remove(repaired, ec);
            throw;
        }
    }
};

}
