module;
#include <cstdint>
#include <optional>
#include <string>
#include <vector>
export module tool.popcap.reanim.core;

export namespace Reanim {

enum class Format : uint8_t {
    PC = 0,
    Phone32 = 1,
    Phone64 = 2,
    TV = 3,
    GameConsole = 4,
    WP = 5
};

inline constexpr float kSentinel = -10000.0f;
inline constexpr float kWpSentinel = -99999.0f;

inline constexpr uint32_t kPopCapZlibMagicLE = 0xDEADFED4;
inline constexpr uint32_t kPopCapZlibMagicBE = 0xD4FEADDE;

inline constexpr uint32_t kPcMagic = 0xB39294C0;
inline constexpr uint32_t kPhone32Magic = 0xFF2565B5;
inline constexpr uint32_t kPhone64Magic = 0xC046E5B0;

struct Transform {
    std::optional<float> x;
    std::optional<float> y;
    std::optional<float> kx;
    std::optional<float> ky;
    std::optional<float> sx;
    std::optional<float> sy;
    std::optional<float> f;
    std::optional<float> a;
    std::optional<std::string> i;
    std::optional<int32_t> iInt;
    std::optional<std::string> resource;
    std::optional<std::string> i2;
    std::optional<std::string> resource2;
    std::optional<std::string> font;
    std::optional<std::string> text;

    [[nodiscard]] bool emptyWp() const noexcept {
        return !x && !y && !sx && !sy && !kx && !ky && !f && !a && !i && !font && !text;
    }
};

struct Track {
    std::string name;
    std::vector<Transform> transforms;
};

struct Data {
    std::optional<int8_t> doScale;
    float fps = 12.0f;
    std::vector<Track> tracks;
};

}
