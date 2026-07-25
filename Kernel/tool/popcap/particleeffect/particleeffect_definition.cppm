module;
#include <array>
#include <cstdint>
export module tool.popcap.particleeffect.definition;

export namespace PopCap::ParticleEffect {

inline constexpr std::array<uint8_t, 5> kPpfMagic{0x04, 'P', 'P', 'F', '1'};
inline constexpr uint32_t kPpfVersion = 1;

}
