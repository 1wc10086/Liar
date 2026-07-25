module;
#include <cstdint>
#include <span>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.reanim.encoder;
export import tool.popcap.reanim.core;
import tool.popcap.reanim.definition;
import tool.popcap.reanim.utils;

export namespace Reanim {

struct Encoder {
    [[nodiscard]] static std::vector<uint8_t> encode(std::string_view textData, Format format, bool isXml = false, bool useZlib = true) {
        if (textData.empty()) return {};

        std::span<const uint8_t> dataSpan(reinterpret_cast<const uint8_t*>(textData.data()), textData.size());
        auto reanim = isXml ? XMLCodec::decode(dataSpan) : JSONCodec::decode(dataSpan);
        if (!reanim) return {};

        auto uncompressed = encodeBinary(*reanim, format);
        if (uncompressed.empty()) return {};
        return maybeCompress(uncompressed, format, useZlib);
    }
};

}
