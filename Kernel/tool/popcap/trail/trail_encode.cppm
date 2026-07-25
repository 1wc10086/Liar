module;
#include <span>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.trail.encode;
import tool.popcap.trail.core;
import tool.popcap.trail.definition;

export namespace Trail {

[[nodiscard]] inline std::vector<uint8_t> encode(std::string_view input, Format format, bool xml = false, bool compressed = true) {
    const auto trail = xml ? Definition::XmlCodec::decode(input) : Definition::JsonCodec::decode(input);
    switch (format) {
        case Format::GameConsole: return Definition::GameConsoleCodec::encode(trail, compressed);
        case Format::PC: return Definition::PCCodec::encode(trail, compressed);
        case Format::Phone32: return Definition::Phone32Codec::encode(trail, compressed);
        case Format::Phone64: return Definition::Phone64Codec::encode(trail, compressed);
        case Format::TV: return Definition::TVCodec::encode(trail, compressed);
        case Format::WP: return Definition::WPCodec::encode(trail);
        default: return {};
    }
}

}
