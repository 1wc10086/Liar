module;
#include <cstdint>
#include <string_view>
#include <vector>
export module tool.popcap.particleeffect.encode;
import tool.popcap.particleeffect.core;
import tool.popcap.particleeffect.utils;

export namespace PopCap::ParticleEffect {

[[nodiscard]] std::vector<uint8_t> encode(std::string_view text) {
    return encodeEffect(Json::fromJsonText(text));
}

}
