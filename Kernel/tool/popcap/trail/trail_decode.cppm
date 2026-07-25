module;
#include <span>
#include <string>
#include <vector>
export module tool.popcap.trail.decode;
import tool.popcap.trail.core;
import tool.popcap.trail.definition;

export namespace Trail {

[[nodiscard]] inline std::string decode(std::span<const uint8_t> input, Format format, bool xml = false, bool compressed = true) {
    Data trail;
    switch (format) {
        case Format::GameConsole: trail = Definition::GameConsoleCodec::decode(input, compressed); break;
        case Format::PC: trail = Definition::PCCodec::decode(input, compressed); break;
        case Format::Phone32: trail = Definition::Phone32Codec::decode(input, compressed); break;
        case Format::Phone64: trail = Definition::Phone64Codec::decode(input, compressed); break;
        case Format::TV: trail = Definition::TVCodec::decode(input, compressed); break;
        case Format::WP: trail = Definition::WPCodec::decode(input); break;
        default: return {};
    }
    return xml ? Definition::XmlCodec::encode(trail) : Definition::JsonCodec::encode(trail);
}

}
