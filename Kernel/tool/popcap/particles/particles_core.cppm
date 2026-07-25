module;
#include <cstdint>
#include <optional>
#include <string>
#include <vector>
export module tool.popcap.particles.core;

export namespace Particles {

struct TrackNode {
    float time = 0.0F;
    std::optional<float> lowValue;
    std::optional<float> highValue;
    std::optional<int32_t> curveType;
    std::optional<int32_t> distribution;
};

struct Field {
    std::optional<int32_t> type;
    std::vector<TrackNode> x;
    std::vector<TrackNode> y;
};

struct Emitter {
    std::string name;
    std::string image;
    std::string imagePath;
    std::optional<int32_t> imageCol;
    std::optional<int32_t> imageRow;
    std::optional<int32_t> imageFrames;
    std::optional<int32_t> animated;
    int32_t particleFlags = 0;
    std::optional<int32_t> type;
    std::string onDuration;

    std::vector<TrackNode> systemDuration, crossFadeDuration, spawnRate, spawnMinActive, spawnMaxActive;
    std::vector<TrackNode> spawnMaxLaunched, emitterRadius, emitterOffsetX, emitterOffsetY, emitterBoxX;
    std::vector<TrackNode> emitterBoxY, emitterPath, emitterSkewX, emitterSkewY, particleDuration;
    std::vector<TrackNode> systemRed, systemGreen, systemBlue, systemAlpha, systemBrightness;
    std::vector<TrackNode> launchSpeed, launchAngle;
    std::vector<Field> field;
    std::vector<Field> systemField;
    std::vector<TrackNode> particleRed, particleGreen, particleBlue, particleAlpha, particleBrightness;
    std::vector<TrackNode> particleSpinAngle, particleSpinSpeed, particleScale, particleStretch;
    std::vector<TrackNode> collisionReflect, collisionSpin, clipTop, clipBottom, clipLeft, clipRight;
    std::vector<TrackNode> animationRate;
};

struct Data {
    std::vector<Emitter> emitters;
};

enum class Format : uint8_t {
    PC, Phone32, Phone64, JSON, TV, GameConsole, WP, XML
};

}
