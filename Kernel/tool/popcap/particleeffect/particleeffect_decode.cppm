module;
#include <span>
#include <cstdint>
#include <string>
export module tool.popcap.particleeffect.decode;
import tool.popcap.particleeffect.core;
import tool.popcap.particleeffect.utils;

export namespace PopCap::ParticleEffect {

[[nodiscard]] std::string decode(std::span<const uint8_t> data) {
    return Json::toJsonText(decodeEffect(data));
}

}
