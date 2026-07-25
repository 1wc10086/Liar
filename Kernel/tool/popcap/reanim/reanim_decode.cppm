module;
#include <cstdint>
#include <span>
#include <string>
#include <utility>
#include <vector>
export module tool.popcap.reanim.decoder;
export import tool.popcap.reanim.core;
import tool.popcap.reanim.definition;
import tool.popcap.reanim.utils;

export namespace Reanim {

struct Decoder {
    [[nodiscard]] static std::string decode(std::span<const uint8_t> inputData, Format format, bool useXml = false, bool useZlib = true) {
        if (inputData.empty()) return {};

        std::vector<uint8_t> decompressed = maybeDecompress(inputData, format, useZlib);
        std::span<const uint8_t> dataToDecode = decompressed.empty() ? inputData : std::span<const uint8_t>(decompressed);

        auto reanim = decodeBinary(dataToDecode, format);
        if (!reanim) return {};
        return useXml ? XMLCodec::encode(*reanim) : JSONCodec::encode(*reanim);
    }
};

}
