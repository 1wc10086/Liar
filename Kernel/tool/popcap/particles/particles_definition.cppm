module;
#include <cstdint>
#include <optional>
#include <span>
#include <vector>
export module tool.popcap.particles.definition;
import tool.popcap.particles.core;
import tool.popcap.particles.utils;
import utility.binary.unified_binary_stream;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;

export namespace Particles::Definition {

inline constexpr int32_t compressedMagic = static_cast<int32_t>(0xDEADFED4U);

[[nodiscard]] inline std::vector<uint8_t> compress(std::span<const uint8_t> data, bool is64Bit, UnifiedBinaryStream::Endian endian, bool enabled) {
    if (!enabled) return {data.begin(), data.end()};
    if (endian == UnifiedBinaryStream::Endian::Little) {
        if (auto result = zlib_ns::PopCapCompressor::compress(data, is64Bit)) return std::move(*result);
        return {data.begin(), data.end()};
    }
    auto result = zlib_ns::Compressor::compress(data);
    if (!result) return {data.begin(), data.end()};
    UnifiedBinaryStream stream(UnifiedBinaryStream::Mode::Write, endian);
    stream.writeInt32(compressedMagic);
    stream.writeInt32(static_cast<int32_t>(data.size()));
    stream.writeBytes(*result);
    return stream.toByteArray();
}

[[nodiscard]] inline std::vector<uint8_t> decompress(std::span<const uint8_t> data, bool is64Bit, UnifiedBinaryStream::Endian endian, bool enabled) {
    if (!enabled || data.size() < 8) return {data.begin(), data.end()};
    UnifiedBinaryStream stream(data, endian);
    if (stream.peekInt32() != compressedMagic) return {data.begin(), data.end()};
    if (endian == UnifiedBinaryStream::Endian::Little) {
        if (auto result = zlib_ns::PopCapDecompressor::decompress(data, is64Bit)) return std::move(*result);
    } else {
        stream.setPosition(4);
        const auto uncompressedSize = stream.readInt32();
        if (uncompressedSize > 0) {
            if (auto result = zlib_ns::Decompressor::decompress(data.subspan(8), static_cast<size_t>(uncompressedSize))) return std::move(*result);
        }
    }
    return {data.begin(), data.end()};
}

inline void writeHeader(UnifiedBinaryStream& stream, const Emitter& emitter, int32_t padding) {
    const auto wide = padding == 9;
    stream.writeInt32(0);
    if (wide) stream.writeInt32(0);

    stream.writeInt32(emitter.imageCol.value_or(0));
    stream.writeInt32(emitter.imageRow.value_or(0));
    stream.writeInt32(emitter.imageFrames.value_or(1));
    stream.writeInt32(emitter.animated.value_or(0));
    stream.writeInt32(emitter.particleFlags);
    stream.writeInt32(emitter.type.value_or(1));

    for (int32_t i = 0; i < (wide ? 92 : 46); ++i) stream.writeInt32(0);
    stream.writeInt32(0);
    if (wide) stream.writeInt32(0);
    stream.writeInt32(static_cast<int32_t>(emitter.field.size()));
    for (int32_t i = 0; i < (wide ? 3 : 1); ++i) stream.writeInt32(0);
    stream.writeInt32(static_cast<int32_t>(emitter.systemField.size()));
    for (int32_t i = 0; i < (wide ? 65 : 32); ++i) stream.writeInt32(0);
}

inline void readHeader(UnifiedBinaryStream& stream, Emitter& emitter, int32_t padding) {
    stream.setPosition(stream.getPosition() + (padding == 9 ? 8 : 4));
    if (const auto value = stream.readInt32(); value != 0) emitter.imageCol = value;
    if (const auto value = stream.readInt32(); value != 0) emitter.imageRow = value;
    if (const auto value = stream.readInt32(); value != 1) emitter.imageFrames = value;
    if (const auto value = stream.readInt32(); value != 0) emitter.animated = value;
    emitter.particleFlags = stream.readInt32();
    if (const auto value = stream.readInt32(); value != 1) emitter.type = value;
    stream.setPosition(stream.getPosition() + (padding == 9 ? 376 : 188));
    if (const auto count = stream.readInt32(); count > 0) emitter.field.resize(static_cast<size_t>(count));
    stream.setPosition(stream.getPosition() + (padding == 9 ? 12 : 4));
    if (const auto count = stream.readInt32(); count > 0) emitter.systemField.resize(static_cast<size_t>(count));
    stream.setPosition(stream.getPosition() + (padding == 9 ? 256 : 128));
}

inline void writeTracks1(UnifiedBinaryStream& s, const Emitter& e) {
    using Utils::writeTrackNodes;
    writeTrackNodes(s, e.systemDuration);
    s.writeStringByInt32Head(e.onDuration);
    writeTrackNodes(s, e.crossFadeDuration);
    writeTrackNodes(s, e.spawnRate);
    writeTrackNodes(s, e.spawnMinActive);
    writeTrackNodes(s, e.spawnMaxActive);
    writeTrackNodes(s, e.spawnMaxLaunched);
    writeTrackNodes(s, e.emitterRadius);
    writeTrackNodes(s, e.emitterOffsetX);
    writeTrackNodes(s, e.emitterOffsetY);
    writeTrackNodes(s, e.emitterBoxX);
    writeTrackNodes(s, e.emitterBoxY);
    writeTrackNodes(s, e.emitterPath);
    writeTrackNodes(s, e.emitterSkewX);
    writeTrackNodes(s, e.emitterSkewY);
    writeTrackNodes(s, e.particleDuration);
    writeTrackNodes(s, e.systemRed);
    writeTrackNodes(s, e.systemGreen);
    writeTrackNodes(s, e.systemBlue);
    writeTrackNodes(s, e.systemAlpha);
    writeTrackNodes(s, e.systemBrightness);
    writeTrackNodes(s, e.launchSpeed);
    writeTrackNodes(s, e.launchAngle);
}
inline void readTracks1(UnifiedBinaryStream& s, Emitter& e) {
    using Utils::readTrackNodes;
    e.systemDuration=readTrackNodes(s); e.onDuration=s.readStringByInt32Head(); e.crossFadeDuration=readTrackNodes(s); e.spawnRate=readTrackNodes(s); e.spawnMinActive=readTrackNodes(s); e.spawnMaxActive=readTrackNodes(s); e.spawnMaxLaunched=readTrackNodes(s); e.emitterRadius=readTrackNodes(s); e.emitterOffsetX=readTrackNodes(s); e.emitterOffsetY=readTrackNodes(s); e.emitterBoxX=readTrackNodes(s); e.emitterBoxY=readTrackNodes(s); e.emitterPath=readTrackNodes(s); e.emitterSkewX=readTrackNodes(s); e.emitterSkewY=readTrackNodes(s); e.particleDuration=readTrackNodes(s); e.systemRed=readTrackNodes(s); e.systemGreen=readTrackNodes(s); e.systemBlue=readTrackNodes(s); e.systemAlpha=readTrackNodes(s); e.systemBrightness=readTrackNodes(s); e.launchSpeed=readTrackNodes(s); e.launchAngle=readTrackNodes(s);
}
inline void writeTracks2(UnifiedBinaryStream& s, const Emitter& e) {
    using Utils::writeTrackNodes;
    writeTrackNodes(s, e.particleRed);
    writeTrackNodes(s, e.particleGreen);
    writeTrackNodes(s, e.particleBlue);
    writeTrackNodes(s, e.particleAlpha);
    writeTrackNodes(s, e.particleBrightness);
    writeTrackNodes(s, e.particleSpinAngle);
    writeTrackNodes(s, e.particleSpinSpeed);
    writeTrackNodes(s, e.particleScale);
    writeTrackNodes(s, e.particleStretch);
    writeTrackNodes(s, e.collisionReflect);
    writeTrackNodes(s, e.collisionSpin);
    writeTrackNodes(s, e.clipTop);
    writeTrackNodes(s, e.clipBottom);
    writeTrackNodes(s, e.clipLeft);
    writeTrackNodes(s, e.clipRight);
    writeTrackNodes(s, e.animationRate);
}
inline void readTracks2(UnifiedBinaryStream& s, Emitter& e) {
    using Utils::readTrackNodes;
    e.particleRed = readTrackNodes(s);
    e.particleGreen = readTrackNodes(s);
    e.particleBlue = readTrackNodes(s);
    e.particleAlpha = readTrackNodes(s);
    e.particleBrightness = readTrackNodes(s);
    e.particleSpinAngle = readTrackNodes(s);
    e.particleSpinSpeed = readTrackNodes(s);
    e.particleScale = readTrackNodes(s);
    e.particleStretch = readTrackNodes(s);
    e.collisionReflect = readTrackNodes(s);
    e.collisionSpin = readTrackNodes(s);
    e.clipTop = readTrackNodes(s);
    e.clipBottom = readTrackNodes(s);
    e.clipLeft = readTrackNodes(s);
    e.clipRight = readTrackNodes(s);
    e.animationRate = readTrackNodes(s);
}

}
