module;
#include <array>
#include <cstdint>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.trail.core;

export namespace Trail {

enum class Format : uint8_t { GameConsole, PC, Phone32, Phone64, TV, WP, Json, RawXml, Unknown };

struct TrackNode {
    float time = 0.0f;
    float lowValue = 0.0f;
    float highValue = 0.0f;
    int32_t curveType = 1;
    int32_t distribution = 1;
};

struct Data {
    int32_t maxPoints = 2;
    float minPointDistance = 1.0f;
    int32_t flags = 0;
    std::string image;
    std::string imageResource;
    std::vector<TrackNode> widthOverLength;
    std::vector<TrackNode> widthOverTime;
    std::vector<TrackNode> alphaOverLength;
    std::vector<TrackNode> alphaOverTime;
    std::vector<TrackNode> duration;
};

inline constexpr std::array<std::string_view, 14> kCurveNames{
    "Constant", "Linear", "EaseIn", "EaseOut", "EaseInOut", "EaseInOutWeak", "FastInOut",
    "FastInOutWeak", "WeakFastInOut", "Bounce", "BounceFastMiddle", "BounceSlowMiddle", "SinWave", "EaseSinWave"
};

}
