module;
#include <algorithm>
#include <charconv>
#include <cctype>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <system_error>
#include <utility>
#include <vector>
export module tool.popcap.wwise.bnk.utils;
import utility.io;
import utility.binary.unified_binary_stream;
export import tool.popcap.wwise.bnk.core;

export namespace WwiseSoundBank::Detail {

namespace fs = std::filesystem;

class MappedInput {
public:
    explicit MappedInput(const std::string& path) {
        auto [p, n] = FileUtils::mmapReadFile(path);
        if (p) {
            data_ = p;
            size_ = n;
            mapped_ = true;
        } else {
            owned_ = FileUtils::readFileBytes(path);
            data_ = owned_.data();
            size_ = owned_.size();
        }
    }

    MappedInput(const MappedInput&) = delete;
    MappedInput& operator=(const MappedInput&) = delete;

    MappedInput(MappedInput&& other) noexcept
        : data_(std::exchange(other.data_, nullptr)),
          size_(std::exchange(other.size_, 0)),
          mapped_(std::exchange(other.mapped_, false)),
          owned_(std::move(other.owned_)) {}

    MappedInput& operator=(MappedInput&& other) noexcept {
        if (this != &other) {
            release();
            data_ = std::exchange(other.data_, nullptr);
            size_ = std::exchange(other.size_, 0);
            mapped_ = std::exchange(other.mapped_, false);
            owned_ = std::move(other.owned_);
        }
        return *this;
    }

    ~MappedInput() { release(); }

    [[nodiscard]] std::span<const uint8_t> span() const noexcept { return {data_, size_}; }
    [[nodiscard]] const uint8_t* data() const noexcept { return data_; }
    [[nodiscard]] size_t size() const noexcept { return size_; }

private:
    void release() noexcept {
        if (mapped_ && data_) FileUtils::munmapFile(data_, size_);
        data_ = nullptr;
        size_ = 0;
        mapped_ = false;
        owned_.clear();
    }

    const uint8_t* data_{};
    size_t size_{};
    bool mapped_{};
    std::vector<uint8_t> owned_;
};

[[nodiscard]] inline bool iequalsAscii(std::string_view a, std::string_view b) noexcept {
    return a.size() == b.size() && std::equal(a.begin(), a.end(), b.begin(), [](unsigned char x, unsigned char y) {
        return std::tolower(x) == std::tolower(y);
    });
}

[[nodiscard]] inline uint8_t hexNibble(char c) {
    if (c >= '0' && c <= '9') return static_cast<uint8_t>(c - '0');
    if (c >= 'A' && c <= 'F') return static_cast<uint8_t>(c - 'A' + 10);
    if (c >= 'a' && c <= 'f') return static_cast<uint8_t>(c - 'a' + 10);
    throw std::runtime_error("Invalid hex character");
}

[[nodiscard]] inline size_t hexSpaceByteCount(std::string_view s) {
    size_t n = 0;
    for (unsigned char c : s) n += !std::isspace(c);
    if (n & 1u) throw std::runtime_error("Invalid hex length");
    return n >> 1u;
}

inline void writeHexSpaceBytes(UnifiedBinaryStream& out, std::string_view s) {
    int hi = -1;
    for (char c : s) {
        if (std::isspace(static_cast<unsigned char>(c))) continue;
        const int v = hexNibble(c);
        if (hi < 0) hi = v;
        else {
            out.writeUInt8(static_cast<uint8_t>((hi << 4) | v));
            hi = -1;
        }
    }
    if (hi >= 0) throw std::runtime_error("Invalid hex tail");
    if (out.hasErrorOccurred()) throw std::runtime_error("Binary write failed");
}

[[nodiscard]] inline std::string bytesToHexSpace(std::span<const uint8_t> bytes) {
    static constexpr char lut[] = "0123456789ABCDEF";
    if (bytes.empty()) return {};
    std::string out(bytes.size() * 3 - 1, ' ');
    size_t p = 0;
    for (size_t i = 0; i < bytes.size(); ++i) {
        const uint8_t b = bytes[i];
        out[p++] = lut[b >> 4];
        out[p++] = lut[b & 0x0F];
        if (i + 1 != bytes.size()) ++p;
    }
    return out;
}

[[nodiscard]] inline std::vector<uint8_t> hexSpaceToBytes(std::string_view s) {
    std::vector<uint8_t> out;
    out.reserve(hexSpaceByteCount(s));
    int hi = -1;
    for (char c : s) {
        if (std::isspace(static_cast<unsigned char>(c))) continue;
        const int v = hexNibble(c);
        if (hi < 0) hi = v;
        else {
            out.push_back(static_cast<uint8_t>((hi << 4) | v));
            hi = -1;
        }
    }
    if (hi >= 0) throw std::runtime_error("Invalid hex tail");
    return out;
}

[[nodiscard]] inline std::span<const uint8_t> readSpan(UnifiedBinaryStream& s, size_t n) {
    const size_t pos = s.getPosition();
    if (!s.checkBounds(pos, n)) throw std::runtime_error("Unexpected EOF");
    const auto* ptr = s.getBufferPtr() + pos;
    s.setPosition(pos + n);
    return {ptr, n};
}

[[nodiscard]] inline std::string readNullTermString(UnifiedBinaryStream& s) {
    const size_t start = s.getPosition();
    const size_t len = s.getLength();
    const auto* buf = s.getBufferPtr();
    size_t pos = start;
    while (pos < len && buf[pos]) ++pos;
    if (pos >= len) throw std::runtime_error("Unterminated string");
    s.setPosition(pos + 1);
    return {reinterpret_cast<const char*>(buf + start), pos - start};
}

inline void writeNullTermString(UnifiedBinaryStream& s, std::string_view str) {
    s.writeBytes(reinterpret_cast<const uint8_t*>(str.data()), str.size());
    s.writeUInt8(0);
    if (s.hasErrorOccurred()) throw std::runtime_error("Binary write failed");
}

inline void expectFourCC(UnifiedBinaryStream& s, std::string_view cc) {
    const auto got = readSpan(s, 4);
    if (cc.size() != 4 || std::memcmp(got.data(), cc.data(), 4) != 0) throw std::runtime_error("Invalid BNK magic");
}

inline void writeFourCC(UnifiedBinaryStream& s, std::string_view cc) {
    if (cc.size() != 4) throw std::runtime_error("Invalid FourCC");
    s.writeBytes(reinterpret_cast<const uint8_t*>(cc.data()), 4);
    if (s.hasErrorOccurred()) throw std::runtime_error("Binary write failed");
}

[[nodiscard]] inline std::vector<uint8_t> finishStream(UnifiedBinaryStream& s) {
    if (s.hasErrorOccurred()) throw std::runtime_error("Binary stream error");
    return s.toByteArray();
}

inline void appendChunk(UnifiedBinaryStream& out, std::string_view id, const std::vector<uint8_t>& body) {
    writeFourCC(out, id);
    out.writeUInt32(static_cast<uint32_t>(body.size()));
    out.writeBytes(body);
    if (out.hasErrorOccurred()) throw std::runtime_error("Binary write failed");
}

[[nodiscard]] inline constexpr uint32_t align16(uint32_t size) noexcept {
    return (16u - (size & 15u)) & 15u;
}

[[nodiscard]] inline std::vector<uint32_t> collectNumericWemIds(const std::string& wemsPath) {
    std::vector<uint32_t> ids;
    std::error_code ec;
    if (!fs::is_directory(wemsPath, ec)) return ids;
    for (const auto& entry : fs::directory_iterator(wemsPath, ec)) {
        if (!entry.is_regular_file(ec) || !iequalsAscii(entry.path().extension().string(), ".wem")) continue;
        const auto stem = entry.path().stem().string();
        uint32_t id = 0;
        auto [ptr, err] = std::from_chars(stem.data(), stem.data() + stem.size(), id);
        if (err == std::errc{} && ptr == stem.data() + stem.size()) ids.push_back(id);
    }
    std::sort(ids.begin(), ids.end());
    return ids;
}

[[nodiscard]] inline std::vector<uint8_t> buildHeaderBody(const BankHeader& h) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    out.writeUInt32(h.version);
    out.writeUInt32(h.id);
    out.writeUInt32(h.language);
    writeHexSpaceBytes(out, h.headExpand);
    return finishStream(out);
}

[[nodiscard]] inline std::vector<uint8_t> buildDidxBody(const std::vector<DidxEntry>& didx) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    for (const auto& e : didx) {
        out.writeUInt32(e.id);
        out.writeUInt32(e.offset);
        out.writeUInt32(e.size);
    }
    return finishStream(out);
}

[[nodiscard]] inline std::vector<uint8_t> buildInitBody(const std::vector<InitEntry>& init) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    out.writeUInt32(static_cast<uint32_t>(init.size()));
    for (const auto& e : init) {
        out.writeUInt32(e.id);
        writeNullTermString(out, e.name);
    }
    return finishStream(out);
}

[[nodiscard]] inline std::vector<uint8_t> buildStmgBody(const GameSync& stmg, uint32_t version) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    writeHexSpaceBytes(out, stmg.volumeThreshold);
    writeHexSpaceBytes(out, stmg.maxVoiceInstances);
    if (version >= 140) out.writeUInt16(stmg.unknownType1);

    out.writeUInt32(static_cast<uint32_t>(stmg.stageGroup.size()));
    for (const auto& s : stmg.stageGroup) {
        out.writeUInt32(s.id);
        writeHexSpaceBytes(out, s.data.defaultTransitionTime);
        out.writeUInt32(static_cast<uint32_t>(s.data.customTransition.size()));
        for (const auto& c : s.data.customTransition) writeHexSpaceBytes(out, c);
    }

    out.writeUInt32(static_cast<uint32_t>(stmg.switchGroup.size()));
    for (const auto& s : stmg.switchGroup) {
        out.writeUInt32(s.id);
        out.writeUInt32(s.data.parameter);
        if (version >= 112) out.writeUInt8(s.data.parameterCategory);
        out.writeUInt32(static_cast<uint32_t>(s.data.point.size()));
        for (const auto& p : s.data.point) writeHexSpaceBytes(out, p);
    }

    out.writeUInt32(static_cast<uint32_t>(stmg.gameParameter.size()));
    for (const auto& p : stmg.gameParameter) {
        out.writeUInt32(p.id);
        writeHexSpaceBytes(out, p.data);
    }

    if (version >= 140) out.writeUInt32(stmg.unknownType2);
    return finishStream(out);
}

inline void writeEnvironmentItem(UnifiedBinaryStream& out, const EnvironmentItem& item, uint32_t version) {
    writeHexSpaceBytes(out, item.volume.volumeValue);
    out.writeUInt16(static_cast<uint16_t>(item.volume.volumePoint.size()));
    for (const auto& p : item.volume.volumePoint) writeHexSpaceBytes(out, p);

    writeHexSpaceBytes(out, item.lowPassFilter.value);
    out.writeUInt16(static_cast<uint16_t>(item.lowPassFilter.point.size()));
    for (const auto& p : item.lowPassFilter.point) writeHexSpaceBytes(out, p);

    if (version >= 112) {
        if (item.highPassFilter) {
            writeHexSpaceBytes(out, item.highPassFilter->value);
            out.writeUInt16(static_cast<uint16_t>(item.highPassFilter->point.size()));
            for (const auto& p : item.highPassFilter->point) writeHexSpaceBytes(out, p);
        } else {
            out.writeUInt16(0);
            out.writeUInt16(0);
        }
    }
}

[[nodiscard]] inline std::vector<uint8_t> buildEnvsBody(const Environments& envs, uint32_t version) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    writeEnvironmentItem(out, envs.obstruction, version);
    writeEnvironmentItem(out, envs.occlusion, version);
    return finishStream(out);
}

[[nodiscard]] inline std::vector<uint8_t> buildHircBody(const std::vector<HircObject>& hirc) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    out.writeUInt32(static_cast<uint32_t>(hirc.size()));
    for (const auto& obj : hirc) {
        out.writeUInt8(obj.objType);
        out.writeUInt32(static_cast<uint32_t>(hexSpaceByteCount(obj.data) + 4));
        out.writeUInt32(obj.id);
        writeHexSpaceBytes(out, obj.data);
    }
    return finishStream(out);
}

[[nodiscard]] inline std::vector<uint8_t> buildStidBody(const Reference& ref) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    out.writeUInt32(ref.unknownType);
    out.writeUInt32(static_cast<uint32_t>(ref.entries.size()));
    for (const auto& e : ref.entries) {
        if (e.name.size() > 255) throw std::runtime_error("STID name too long");
        out.writeUInt32(e.id);
        out.writeUInt8(static_cast<uint8_t>(e.name.size()));
        out.writeBytes(reinterpret_cast<const uint8_t*>(e.name.data()), e.name.size());
    }
    return finishStream(out);
}

[[nodiscard]] inline std::vector<uint8_t> buildPlatBody(const PlatformSetting& plat) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    writeNullTermString(out, plat.platform);
    return finishStream(out);
}

[[nodiscard]] inline std::vector<uint8_t> buildBankBinary(const Bank& bank, std::span<const uint8_t> dataBlob = {}) {
    UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
    appendChunk(out, "BKHD", buildHeaderBody(bank.header));
    if (!bank.dataIndex.empty()) appendChunk(out, "DIDX", buildDidxBody(bank.dataIndex));
    if (bank.initialization) appendChunk(out, "INIT", buildInitBody(*bank.initialization));
    if (bank.gameSync) appendChunk(out, "STMG", buildStmgBody(*bank.gameSync, bank.header.version));
    if (bank.environments) appendChunk(out, "ENVS", buildEnvsBody(*bank.environments, bank.header.version));
    if (!bank.hierarchy.empty()) appendChunk(out, "HIRC", buildHircBody(bank.hierarchy));
    if (bank.reference) appendChunk(out, "STID", buildStidBody(*bank.reference));
    if (bank.platform) appendChunk(out, "PLAT", buildPlatBody(*bank.platform));
    if (!dataBlob.empty()) {
        writeFourCC(out, "DATA");
        out.writeUInt32(static_cast<uint32_t>(dataBlob.size()));
        out.writeBytes(dataBlob);
        if (out.hasErrorOccurred()) throw std::runtime_error("Binary write failed");
    }
    return finishStream(out);
}

[[nodiscard]] inline Bank parseBnk(std::span<const uint8_t> bytes) {
    UnifiedBinaryStream s(bytes, UnifiedBinaryStream::Endian::Little);
    Bank bank;

    expectFourCC(s, "BKHD");
    const uint32_t bkhdLength = s.readUInt32();
    const size_t bkhdStart = s.getPosition();
    if (bkhdLength < 12) throw std::runtime_error("Invalid BKHD length");

    bank.header.version = s.readUInt32();
    bank.header.id = s.readUInt32();
    bank.header.language = s.readUInt32();
    bank.header.headExpand = bytesToHexSpace(readSpan(s, bkhdLength - (s.getPosition() - bkhdStart)));
    s.setPosition(bkhdStart + bkhdLength);

    const size_t fileEnd = s.getLength();
    while (s.getPosition() + 8 <= fileEnd) {
        const auto idSpan = readSpan(s, 4);
        const uint32_t chunkSize = s.readUInt32();
        const size_t chunkStart = s.getPosition();
        if (chunkStart + chunkSize > fileEnd) throw std::runtime_error("Invalid BNK chunk size");
        const std::string_view id(reinterpret_cast<const char*>(idSpan.data()), 4);

        if (id == "DIDX") {
            const uint32_t count = chunkSize / 12u;
            bank.dataIndex.reserve(bank.dataIndex.size() + count);
            bank.embeddedMedia.reserve(bank.embeddedMedia.size() + count);
            for (uint32_t i = 0; i < count; ++i) {
                auto& e = bank.dataIndex.emplace_back();
                e.id = s.readUInt32();
                e.offset = s.readUInt32();
                e.size = s.readUInt32();
                bank.embeddedMedia.push_back(e.id);
            }
        } else if (id == "DATA") {
            bank.dataChunkOffset = static_cast<uint64_t>(chunkStart);
        } else if (id == "INIT") {
            std::vector<InitEntry> init;
            const uint32_t count = s.readUInt32();
            init.reserve(count);
            for (uint32_t i = 0; i < count; ++i) {
                auto& e = init.emplace_back();
                e.id = s.readUInt32();
                e.name = readNullTermString(s);
            }
            bank.initialization = std::move(init);
        } else if (id == "STMG") {
            GameSync stmg;
            stmg.volumeThreshold = bytesToHexSpace(readSpan(s, 4));
            stmg.maxVoiceInstances = bytesToHexSpace(readSpan(s, 2));
            if (bank.header.version >= 140) stmg.unknownType1 = s.readUInt16();

            const uint32_t stageCount = s.readUInt32();
            stmg.stageGroup.reserve(stageCount);
            for (uint32_t i = 0; i < stageCount; ++i) {
                auto& g = stmg.stageGroup.emplace_back();
                g.id = s.readUInt32();
                g.data.defaultTransitionTime = bytesToHexSpace(readSpan(s, 4));
                const uint32_t customCount = s.readUInt32();
                g.data.customTransition.reserve(customCount);
                for (uint32_t j = 0; j < customCount; ++j) g.data.customTransition.push_back(bytesToHexSpace(readSpan(s, 12)));
            }

            const uint32_t switchCount = s.readUInt32();
            stmg.switchGroup.reserve(switchCount);
            for (uint32_t i = 0; i < switchCount; ++i) {
                auto& g = stmg.switchGroup.emplace_back();
                g.id = s.readUInt32();
                g.data.parameter = s.readUInt32();
                if (bank.header.version >= 112) g.data.parameterCategory = s.readUInt8();
                const uint32_t pointCount = s.readUInt32();
                g.data.point.reserve(pointCount);
                for (uint32_t j = 0; j < pointCount; ++j) g.data.point.push_back(bytesToHexSpace(readSpan(s, 12)));
            }

            const uint32_t paramCount = s.readUInt32();
            stmg.gameParameter.reserve(paramCount);
            for (uint32_t i = 0; i < paramCount; ++i) {
                auto& p = stmg.gameParameter.emplace_back();
                p.id = s.readUInt32();
                p.data = bytesToHexSpace(readSpan(s, bank.header.version >= 112 ? 17u : 4u));
            }

            if (bank.header.version >= 140) stmg.unknownType2 = s.readUInt32();
            bank.gameSync = std::move(stmg);
        } else if (id == "ENVS") {
            auto parseEnvItem = [&] -> EnvironmentItem {
                EnvironmentItem item;
                item.volume.volumeValue = bytesToHexSpace(readSpan(s, 2));
                const uint16_t volCount = s.readUInt16();
                item.volume.volumePoint.reserve(volCount);
                for (uint16_t i = 0; i < volCount; ++i) item.volume.volumePoint.push_back(bytesToHexSpace(readSpan(s, 12)));
                item.lowPassFilter.value = bytesToHexSpace(readSpan(s, 2));
                const uint16_t lpCount = s.readUInt16();
                item.lowPassFilter.point.reserve(lpCount);
                for (uint16_t i = 0; i < lpCount; ++i) item.lowPassFilter.point.push_back(bytesToHexSpace(readSpan(s, 12)));
                if (bank.header.version >= 112) {
                    EnvironmentFilter hp;
                    hp.value = bytesToHexSpace(readSpan(s, 2));
                    const uint16_t hpCount = s.readUInt16();
                    hp.point.reserve(hpCount);
                    for (uint16_t i = 0; i < hpCount; ++i) hp.point.push_back(bytesToHexSpace(readSpan(s, 12)));
                    item.highPassFilter = std::move(hp);
                }
                return item;
            };
            bank.environments = Environments{parseEnvItem(), parseEnvItem()};
        } else if (id == "HIRC") {
            const uint32_t count = s.readUInt32();
            bank.hierarchy.reserve(count);
            for (uint32_t i = 0; i < count; ++i) {
                auto& obj = bank.hierarchy.emplace_back();
                obj.objType = s.readUInt8();
                const uint32_t length = s.readUInt32();
                if (length < 4) throw std::runtime_error("Invalid HIRC object length");
                obj.id = s.readUInt32();
                obj.data = bytesToHexSpace(readSpan(s, length - 4u));
            }
        } else if (id == "STID") {
            Reference ref;
            ref.unknownType = s.readUInt32();
            const uint32_t count = s.readUInt32();
            ref.entries.reserve(count);
            for (uint32_t i = 0; i < count; ++i) {
                auto& e = ref.entries.emplace_back();
                e.id = s.readUInt32();
                const uint8_t nameLen = s.readUInt8();
                const auto nameBytes = readSpan(s, nameLen);
                e.name.assign(reinterpret_cast<const char*>(nameBytes.data()), nameBytes.size());
            }
            bank.reference = std::move(ref);
        } else if (id == "PLAT") {
            bank.platform = PlatformSetting{readNullTermString(s)};
        }

        s.setPosition(chunkStart + chunkSize);
    }

    return bank;
}

}