module;
#include <algorithm>
#include <charconv>
#include <cstdint>
#include <cstring>
#include <memory>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.rsb.rsb_core;
import utility.io.concurrent;
import utility.binary.unified_binary_stream;

export namespace Rsb {

inline constexpr int32_t  RSB_MAGIC        = 1920164401;
inline constexpr int32_t  RSB_MAGIC_BE     = 828535666;
inline constexpr int32_t  RSGP_MAGIC       = 1920165744;
inline constexpr int32_t  RSGP_MAGIC_BE    = 1920165744;
inline constexpr int32_t  SMF_MAGIC        = -559022380;
inline constexpr uint32_t RSB_HEAD_RSGPINFO_EACH   = 0xCC;
inline constexpr uint32_t RSB_HEAD_COMPOSITE_EACH  = 0x484;
inline constexpr uint32_t RSB_HEAD_AUTOPOOL_EACH   = 0x98;
inline constexpr uint32_t RSGP_HEAD_FILELIST_BEGIN = 0x5C;
inline constexpr int32_t  XMLDAT_MAGIC   = 1919251249;
inline constexpr int32_t  XMLDAT_VERSION = 1;

inline constexpr size_t kZlibStreamThreshold = 1024 * 1024;

[[nodiscard]] inline constexpr int64_t alignTo4K(int64_t off) noexcept {
    return (off % 0x1000 == 0) ? off : (off + 0x1000 - (off % 0x1000));
}

[[nodiscard]] inline std::string u32str(uint32_t v) {
    char b[12];
    auto r = std::to_chars(b, b + 12, v);
    return {b, r.ptr};
}

[[nodiscard]] inline std::string i32str(int32_t v) {
    char b[12];
    auto r = std::to_chars(b, b + 12, v);
    return {b, r.ptr};
}

struct ExtraInfo {
    virtual ~ExtraInfo() = default;
    [[nodiscard]] virtual int getType() const { return -1; }
};

struct RsbExtraInfo final : ExtraInfo {
    uint32_t index = 0;
    RsbExtraInfo() = default;
    explicit RsbExtraInfo(uint32_t i) : index(i) {}
    [[nodiscard]] int getType() const override { return 0; }
};

struct RsgpPart0ExtraInfo final : ExtraInfo {
    int32_t  type   = 0x0;
    uint32_t offset = 0;
    uint32_t size   = 0;
    RsgpPart0ExtraInfo() = default;
    RsgpPart0ExtraInfo(uint32_t off, uint32_t sz) : offset(off), size(sz) {}
    [[nodiscard]] int getType() const override { return 1; }
};

struct RsgpPart1ExtraInfo final : ExtraInfo {
    int32_t  type   = 0x1;
    uint32_t offset = 0;
    uint32_t size   = 0;
    uint32_t index  = 0;
    int32_t  empty1 = 0;
    int32_t  empty2 = 0;
    uint32_t width  = 0;
    uint32_t height = 0;
    RsgpPart1ExtraInfo() = default;
    RsgpPart1ExtraInfo(uint32_t off, uint32_t sz, uint32_t idx, uint32_t w, uint32_t h)
        : offset(off), size(sz), index(idx), width(w), height(h) {}
    [[nodiscard]] int getType() const override { return 2; }
};

struct CompressString {
    std::string name;
    std::shared_ptr<ExtraInfo> extraInfo;
    int type = -1;

    CompressString() = default;
    CompressString(std::string n, std::shared_ptr<ExtraInfo> ei)
        : name(std::move(n)), extraInfo(std::move(ei)) {
        type = extraInfo ? extraInfo->getType() : -1;
    }
};

struct PrefixInfo {
    std::vector<uint8_t> prefix;
    int location;
    PrefixInfo(std::vector<uint8_t> p, int loc) : prefix(std::move(p)), location(loc) {}
};

namespace detail {

inline void writeInt24(std::vector<uint8_t>& bytes, int location, int cover) noexcept {
    bytes[static_cast<size_t>(location) + 1] = static_cast<uint8_t>(cover & 0xFF);
    bytes[static_cast<size_t>(location) + 2] = static_cast<uint8_t>((cover >> 8) & 0xFF);
    bytes[static_cast<size_t>(location) + 3] = static_cast<uint8_t>((cover >> 16) & 0xFF);
}

inline void writeInt32LE(std::vector<uint8_t>& bytes, int location, uint32_t cover) noexcept {
    std::memcpy(bytes.data() + static_cast<size_t>(location), &cover, sizeof(cover));
}

}

class CompressStringList {
public:
    int listType;
    std::vector<CompressString> list;

    explicit CompressStringList(int t) : listType(t) {}

    void add(CompressString cs) { list.push_back(std::move(cs)); }
    void clear() { list.clear(); }
    [[nodiscard]] int length() const { return static_cast<int>(list.size()); }
    [[nodiscard]] const CompressString& operator[](int i) const { return list[static_cast<size_t>(i)]; }

    [[nodiscard]] std::vector<uint8_t> write() {
        std::ranges::sort(list, {}, [](const CompressString& cs) -> const std::string& { return cs.name; });

        const size_t n = list.size();
        std::vector<PrefixInfo> prefixList;
        prefixList.reserve(n + 1);
        prefixList.emplace_back(std::vector<uint8_t>{}, -1);

        std::vector<uint8_t> finishedBytes;
        finishedBytes.reserve(n * 256);

        for (const auto& tls : list) {
            const std::string& fullName = tls.name;
            std::vector<uint8_t> thisRest(fullName.begin(), fullName.end());
            std::vector<int> awaitIndices;
            awaitIndices.reserve(8);
            bool removeStart = false;

            for (int i = 0; i < static_cast<int>(prefixList.size()); ++i) {
                if (removeStart) {
                    detail::writeInt24(finishedBytes, prefixList[static_cast<size_t>(i)].location, 0);
                    prefixList.erase(prefixList.begin() + i);
                    --i;
                    continue;
                }
                const auto& prefix = prefixList[static_cast<size_t>(i)].prefix;
                int j = 0;
                for (; j < static_cast<int>(prefix.size()); ++j) {
                    if (static_cast<int>(thisRest.size()) <= j || thisRest[static_cast<size_t>(j)] != prefix[static_cast<size_t>(j)])
                        break;
                }
                if (j == static_cast<int>(prefix.size()) && !prefix.empty()) {
                    awaitIndices.push_back(i);
                    std::vector<uint8_t> tmp(thisRest.begin() + j, thisRest.end());
                    thisRest = std::move(tmp);
                } else if (j > 0) {
                    int location3 = prefixList[static_cast<size_t>(i)].location + j * 4;
                    detail::writeInt24(finishedBytes, location3, static_cast<int>(finishedBytes.size()) / 4);
                    std::vector<uint8_t> frontPfx(prefix.begin(), prefix.begin() + j);
                    prefixList[static_cast<size_t>(i)].prefix = std::move(frontPfx);
                    awaitIndices.push_back(i);
                    std::vector<uint8_t> tmp(thisRest.begin() + j, thisRest.end());
                    thisRest = std::move(tmp);
                    removeStart = true;
                } else if (j == 0 && !prefix.empty()) {
                    prefixList.erase(prefixList.begin() + i);
                    --i;
                    removeStart = true;
                }
            }

            thisRest.resize(thisRest.size() + 1, 0);
            thisRest.back() = 0;
            prefixList.emplace_back(thisRest, static_cast<int>(finishedBytes.size()));
            awaitIndices.push_back(static_cast<int>(prefixList.size()) - 1);

            int infoLength = (listType == 0) ? 4 : ((tls.type == 1) ? 12 : 32);
            size_t finishedLen = thisRest.size() * 4 + static_cast<size_t>(infoLength);
            std::vector<uint8_t> thisFinished(finishedLen, 0);
            for (size_t i = 0; i < thisRest.size(); ++i) {
                thisFinished[i * 4] = thisRest[i];
            }

            if (infoLength == 4) {
                auto* ei = static_cast<RsbExtraInfo*>(tls.extraInfo.get());
                detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 4, ei ? ei->index : 0);
            } else if (infoLength == 12) {
                auto* ei = static_cast<RsgpPart0ExtraInfo*>(tls.extraInfo.get());
                if (ei) {
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 12, static_cast<uint32_t>(ei->type));
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 8, ei->offset);
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 4, ei->size);
                }
            } else {
                auto* ei = static_cast<RsgpPart1ExtraInfo*>(tls.extraInfo.get());
                if (ei) {
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 32, static_cast<uint32_t>(ei->type));
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 28, ei->offset);
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 24, ei->size);
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 20, ei->index);
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 16, static_cast<uint32_t>(ei->empty1));
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 12, static_cast<uint32_t>(ei->empty2));
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 8, ei->width);
                    detail::writeInt32LE(thisFinished, static_cast<int>(finishedLen) - 4, ei->height);
                }
            }

            size_t oldLen = finishedBytes.size();
            finishedBytes.resize(oldLen + finishedLen);
            std::memcpy(finishedBytes.data() + oldLen, thisFinished.data(), finishedLen);
            for (int idx : awaitIndices) {
                detail::writeInt24(finishedBytes, prefixList[static_cast<size_t>(idx)].location,
                                   static_cast<int>(finishedBytes.size()) / 4);
            }
        }
        for (auto& pi : prefixList) {
            if (!pi.prefix.empty()) {
                detail::writeInt24(finishedBytes, pi.location, 0);
            }
        }
        return finishedBytes;
    }

    void read(std::span<const uint8_t> bytes) {
        list.clear();
        int byteLen = static_cast<int>(bytes.size());
        int pos = 0;
        auto readByte = [&]() -> uint8_t {
            if (pos >= byteLen) throw std::runtime_error("RSB CompressStringList: unexpected EOF");
            return bytes[static_cast<size_t>(pos++)];
        };
        auto peekByte = [&]() -> uint8_t {
            if (pos >= byteLen) throw std::runtime_error("RSB CompressStringList: unexpected EOF");
            return bytes[static_cast<size_t>(pos)];
        };
        auto readUInt24 = [&]() -> uint32_t {
            uint8_t b0 = readByte(), b1 = readByte(), b2 = readByte();
            return static_cast<uint32_t>(b0) | (static_cast<uint32_t>(b1) << 8) | (static_cast<uint32_t>(b2) << 16);
        };
        auto readUInt32 = [&]() -> uint32_t {
            uint8_t b0 = readByte(), b1 = readByte(), b2 = readByte(), b3 = readByte();
            return static_cast<uint32_t>(b0) | (static_cast<uint32_t>(b1) << 8) | (static_cast<uint32_t>(b2) << 16) | (static_cast<uint32_t>(b3) << 24);
        };

        int totalUnits = byteLen / 4;
        struct DefaultEntry { std::string name; int offset; };
        std::vector<DefaultEntry> defaults = {{"", totalUnits}};

        while (pos < byteLen) {
            std::string tempHead, temp;
            int posUnit = pos / 4;
            for (int i = 0; i < static_cast<int>(defaults.size()); ++i) {
                if (posUnit < defaults[static_cast<size_t>(i)].offset) {
                    tempHead += defaults[static_cast<size_t>(i)].name;
                } else {
                    defaults.erase(defaults.begin() + i);
                    --i;
                }
            }
            int startIndex = 0;
            int tpendOffset = defaults.empty() ? totalUnits : defaults.back().offset;
            int tmpEndOffset;
            while (peekByte() != 0) {
                char ch = static_cast<char>(readByte());
                temp += ch;
                tmpEndOffset = static_cast<int>(readUInt24());
                if (tmpEndOffset != 0) {
                    if (temp.size() > 1) {
                        defaults.push_back({temp.substr(static_cast<size_t>(startIndex),
                            temp.size() - 1 - static_cast<size_t>(startIndex)), tpendOffset});
                    }
                    startIndex = static_cast<int>(temp.size()) - 1;
                    tpendOffset = tmpEndOffset;
                }
            }
            readByte();
            tmpEndOffset = static_cast<int>(readUInt24());
            if (tmpEndOffset != 0) {
                if (!temp.empty()) {
                    defaults.push_back({temp.substr(static_cast<size_t>(startIndex),
                        temp.size() - static_cast<size_t>(startIndex)), tpendOffset});
                }
            }
            std::string fullName = std::move(tempHead) + temp;
            std::shared_ptr<ExtraInfo> ei;

            if (listType == 0) {
                auto* rsbEi = new RsbExtraInfo();
                rsbEi->index = readUInt32();
                ei.reset(rsbEi);
            } else {
                if (pos + 4 > byteLen) throw std::runtime_error("RSB CompressStringList: truncated extra info");
                uint32_t firstWord;
                std::memcpy(&firstWord, bytes.data() + static_cast<size_t>(pos), sizeof(firstWord));
                if (firstWord == 0) {
                    auto* p0 = new RsgpPart0ExtraInfo();
                    p0->type   = static_cast<int32_t>(readUInt32());
                    p0->offset = readUInt32();
                    p0->size   = readUInt32();
                    ei.reset(p0);
                } else {
                    auto* p1 = new RsgpPart1ExtraInfo();
                    p1->type   = static_cast<int32_t>(readUInt32());
                    p1->offset = readUInt32();
                    p1->size   = readUInt32();
                    p1->index  = readUInt32();
                    p1->empty1 = static_cast<int32_t>(readUInt32());
                    p1->empty2 = static_cast<int32_t>(readUInt32());
                    p1->width  = readUInt32();
                    p1->height = readUInt32();
                    ei.reset(p1);
                }
            }
            list.emplace_back(std::move(fullName), std::move(ei));
        }
    }
};

struct RsbHeadInfo {
    int32_t  version            = 3;
    uint32_t headLength         = 0;
    uint32_t fileList_Length    = 0;
    uint32_t fileList_Begin     = 0;
    uint32_t rsgpList_Length    = 0;
    uint32_t rsgpList_Begin     = 0;
    uint32_t rsgp_Number        = 0;
    uint32_t rsgpInfo_Begin     = 0;
    uint32_t composite_Number   = 0;
    uint32_t compositeInfo_Begin = 0;
    uint32_t compositeList_Length= 0;
    uint32_t compositeList_Begin = 0;
    uint32_t autopool_Number    = 0;
    uint32_t autopoolInfo_Begin = 0;
    uint32_t ptx_Number         = 0;
    uint32_t ptxInfo_Begin      = 0;
    uint32_t ptxInfo_EachLen    = 0x10;
    uint32_t xmlPart1_Begin     = 0;
    uint32_t xmlPart2_Begin     = 0;
    uint32_t xmlPart3_Begin     = 0;
    uint32_t rsbInfo_Length     = 0;

    void write(UnifiedBinaryStream& bs) const {
        bs.writeInt32(RSB_MAGIC);
        bs.writeInt32(version);
        bs.writeInt32(0);
        bs.writeUInt32(headLength);
        bs.writeUInt32(fileList_Length);
        bs.writeUInt32(fileList_Begin);
        bs.writeInt32(0);  bs.writeInt32(0);
        bs.writeUInt32(rsgpList_Length);
        bs.writeUInt32(rsgpList_Begin);
        bs.writeUInt32(rsgp_Number);
        bs.writeUInt32(rsgpInfo_Begin);
        bs.writeUInt32(RSB_HEAD_RSGPINFO_EACH);
        bs.writeUInt32(composite_Number);
        bs.writeUInt32(compositeInfo_Begin);
        bs.writeUInt32(RSB_HEAD_COMPOSITE_EACH);
        bs.writeUInt32(compositeList_Length);
        bs.writeUInt32(compositeList_Begin);
        bs.writeUInt32(autopool_Number);
        bs.writeUInt32(autopoolInfo_Begin);
        bs.writeUInt32(RSB_HEAD_AUTOPOOL_EACH);
        bs.writeUInt32(ptx_Number);
        bs.writeUInt32(ptxInfo_Begin);
        bs.writeUInt32(ptxInfo_EachLen);
        bs.writeUInt32(xmlPart1_Begin);
        bs.writeUInt32(xmlPart2_Begin);
        bs.writeUInt32(xmlPart3_Begin);
        if (version == 4) {
            uint32_t infoLen = (rsbInfo_Length == 0)
                ? (xmlPart1_Begin == 0 ? headLength : xmlPart1_Begin)
                : rsbInfo_Length;
            bs.writeUInt32(infoLen);
        }
    }

    void read(UnifiedBinaryStream& bs) {
        (void)bs.readInt32();
        version           = bs.readInt32();
        (void)bs.readInt32();
        headLength        = bs.readUInt32();
        fileList_Length   = bs.readUInt32();
        fileList_Begin    = bs.readUInt32();
        (void)bs.readInt32(); (void)bs.readInt32();
        rsgpList_Length   = bs.readUInt32();
        rsgpList_Begin    = bs.readUInt32();
        rsgp_Number       = bs.readUInt32();
        rsgpInfo_Begin    = bs.readUInt32();
        (void)bs.readUInt32();
        composite_Number  = bs.readUInt32();
        compositeInfo_Begin= bs.readUInt32();
        (void)bs.readUInt32();
        compositeList_Length= bs.readUInt32();
        compositeList_Begin = bs.readUInt32();
        autopool_Number   = bs.readUInt32();
        autopoolInfo_Begin= bs.readUInt32();
        (void)bs.readUInt32();
        ptx_Number        = bs.readUInt32();
        ptxInfo_Begin     = bs.readUInt32();
        ptxInfo_EachLen   = bs.readUInt32();
        xmlPart1_Begin    = bs.readUInt32();
        xmlPart2_Begin    = bs.readUInt32();
        xmlPart3_Begin    = bs.readUInt32();
        if (version == 4) rsbInfo_Length = bs.readUInt32();
    }
};

struct ChildRsgpInfo {
    uint32_t    index    = 0;
    uint32_t    ratio    = 0;
    std::string language;

    void write(UnifiedBinaryStream& bs) const {
        bs.writeUInt32(index);
        bs.writeUInt32(ratio);
        char buf[4] = {0};
        size_t n = std::min(language.size(), size_t{4});
        for (size_t i = 0; i < n; ++i) buf[i] = language[i];
        bs.writeUInt8(static_cast<uint8_t>(buf[3]));
        bs.writeUInt8(static_cast<uint8_t>(buf[2]));
        bs.writeUInt8(static_cast<uint8_t>(buf[1]));
        bs.writeUInt8(static_cast<uint8_t>(buf[0]));
        bs.writeInt32(0);
    }

    void read(UnifiedBinaryStream& bs) {
        index = bs.readUInt32();
        ratio = bs.readUInt32();
        uint8_t buf[4];
        for (int i = 0; i < 4; ++i) buf[i] = bs.readUInt8();
        language.clear();
        for (int i = 3; i >= 0; --i)
            if (buf[i] != 0) language += static_cast<char>(buf[i]);
        while (!language.empty() && language.back() == '\0') language.pop_back();
        (void)bs.readInt32();
    }
};

struct RsbCompositeInfo {
    std::string   ID;
    ChildRsgpInfo child_Info[0x40];
    uint32_t      child_Number = 0;

    void write(UnifiedBinaryStream& bs) const {
        uint8_t buf[0x80] = {};
        size_t n = std::min(ID.size(), size_t{0x80});
        std::memcpy(buf, ID.data(), n);
        bs.writeBytes(buf, 0x80);
        for (int i = 0; i < 0x40; ++i) child_Info[i].write(bs);
        bs.writeUInt32(child_Number);
    }

    void readImpl(UnifiedBinaryStream& bs) {
        auto idBytes = bs.readBytes(0x80);
        ID.clear();
        for (auto b : idBytes) if (b != 0) ID += static_cast<char>(b);
        for (int i = 0; i < 0x40; ++i) child_Info[i].read(bs);
        child_Number = bs.readUInt32();
    }
};

struct RsbAutoPoolInfo {
    std::string ID;
    uint32_t    part1_MaxOffset_InDecompress = 0;
    uint32_t    part1_MaxSize  = 0;
    int32_t     type           = 0x1;

    void write(UnifiedBinaryStream& bs) const {
        uint8_t buf[0x80] = {};
        size_t n = std::min(ID.size(), size_t{0x80});
        std::memcpy(buf, ID.data(), n);
        bs.writeBytes(buf, 0x80);
        bs.writeUInt32(part1_MaxOffset_InDecompress);
        bs.writeUInt32(part1_MaxSize);
        bs.writeInt32(type);
        bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0);
    }

    void read(UnifiedBinaryStream& bs) {
        auto idBytes = bs.readBytes(0x80);
        ID.clear();
        for (auto b : idBytes) if (b != 0) ID += static_cast<char>(b);
        part1_MaxOffset_InDecompress = bs.readUInt32();
        part1_MaxSize                = bs.readUInt32();
        type                         = bs.readInt32();
        (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32();
    }
};

struct RsbPtxInfo {
    uint32_t ptxEachLen = 0x10;
    uint32_t width      = 0;
    uint32_t height     = 0;
    uint32_t check      = 0;
    uint32_t format     = 0;
    uint32_t alphaSize  = 0;
    uint32_t alphaFormat= 0;

    explicit RsbPtxInfo(uint32_t eachLen = 0x10) : ptxEachLen(eachLen) {
        if (eachLen != 0x10 && eachLen != 0x14 && eachLen != 0x18)
            throw std::runtime_error("Invalid ptxEachLen");
    }

    void write(UnifiedBinaryStream& bs) const {
        bs.writeUInt32(width);
        bs.writeUInt32(height);
        bs.writeUInt32(check);
        bs.writeUInt32(format);
        if (ptxEachLen >= 0x14) bs.writeUInt32(alphaSize);
        if (ptxEachLen == 0x18) bs.writeUInt32(alphaFormat);
    }

    void read(UnifiedBinaryStream& bs) {
        width  = bs.readUInt32();
        height = bs.readUInt32();
        check  = bs.readUInt32();
        format = bs.readUInt32();
        if (ptxEachLen >= 0x14) {
            alphaSize = bs.readUInt32();
            alphaFormat = (ptxEachLen == 0x18) ? bs.readUInt32()
                        : (alphaSize == 0 ? 0x0u : 0x64u);
        }
    }
};

struct RsbRsgpInfo {
    std::string ID;
    uint32_t    offset        = 0;
    uint32_t    size          = 0;
    uint32_t    pool_Index    = 0;
    uint32_t    flags         = 0b1;
    uint32_t    fileOffset    = 0;
    uint32_t    part0_Offset  = 0;
    uint32_t    part0_ZSize   = 0;
    uint32_t    part0_Size    = 0;
    uint32_t    part0_Size2   = 0;
    uint32_t    part1_Offset  = 0;
    uint32_t    part1_ZSize   = 0;
    uint32_t    part1_Size    = 0;
    uint32_t    ptx_Number    = 0;
    uint32_t    ptx_BeforeNum = 0;

    void write(UnifiedBinaryStream& bs) const {
        uint8_t buf[0x80] = {};
        size_t n = std::min(ID.size(), size_t{0x80});
        std::memcpy(buf, ID.data(), n);
        bs.writeBytes(buf, 0x80);
        bs.writeUInt32(offset);
        bs.writeUInt32(size);
        bs.writeUInt32(pool_Index);
        bs.writeUInt32(flags);
        bs.writeUInt32(fileOffset);
        bs.writeUInt32(part0_Offset);
        bs.writeUInt32(part0_ZSize);
        bs.writeUInt32(part0_Size);
        bs.writeUInt32(part0_Size2 == 0 ? part0_Size : part0_Size2);
        bs.writeUInt32(part1_Offset);
        bs.writeUInt32(part1_ZSize);
        bs.writeUInt32(part1_Size);
        bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0);
        bs.writeInt32(0); bs.writeInt32(0);
        bs.writeUInt32(ptx_Number);
        bs.writeUInt32(ptx_BeforeNum);
    }

    void read(UnifiedBinaryStream& bs) {
        auto idBytes = bs.readBytes(0x80);
        ID.clear();
        for (auto b : idBytes) if (b != 0) ID += static_cast<char>(b);
        offset        = bs.readUInt32();
        size          = bs.readUInt32();
        pool_Index    = bs.readUInt32();
        flags         = bs.readUInt32();
        fileOffset    = bs.readUInt32();
        part0_Offset  = bs.readUInt32();
        part0_ZSize   = bs.readUInt32();
        part0_Size    = bs.readUInt32();
        part0_Size2   = bs.readUInt32();
        part1_Offset  = bs.readUInt32();
        part1_ZSize   = bs.readUInt32();
        part1_Size    = bs.readUInt32();
        (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32();
        (void)bs.readInt32(); (void)bs.readInt32();
        ptx_Number    = bs.readUInt32();
        ptx_BeforeNum = bs.readUInt32();
    }
};

struct RsgpHeadInfo {
    int32_t  magic_t          = RSGP_MAGIC;
    int32_t  version          = 0x3;
    uint32_t flags            = 0b1;
    uint32_t fileOffset       = 0;
    uint32_t part0_Offset     = 0;
    uint32_t part0_ZSize      = 0;
    uint32_t part0_Size       = 0;
    uint32_t part1_Offset     = 0;
    uint32_t part1_ZSize      = 0;
    uint32_t part1_Size       = 0;
    uint32_t fileList_Length  = 0;
    uint32_t fileList_Begin   = RSGP_HEAD_FILELIST_BEGIN;

    void write(UnifiedBinaryStream& bs) const {
        bs.writeInt32(magic_t);
        bs.writeInt32(version);
        bs.writeInt32(0); bs.writeInt32(0);
        bs.writeUInt32(flags);
        bs.writeUInt32(fileOffset);
        bs.writeUInt32(part0_Offset);
        bs.writeUInt32(part0_ZSize);
        bs.writeUInt32(part0_Size);
        bs.writeInt32(0);
        bs.writeUInt32(part1_Offset);
        bs.writeUInt32(part1_ZSize);
        bs.writeUInt32(part1_Size);
        bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0);
        bs.writeInt32(0); bs.writeInt32(0);
        bs.writeUInt32(fileList_Length);
        bs.writeUInt32(fileList_Begin);
        bs.writeInt32(0); bs.writeInt32(0); bs.writeInt32(0);
    }

    void read(UnifiedBinaryStream& bs) {
        magic_t  = bs.readInt32();
        version  = bs.readInt32();
        (void)bs.readInt32(); (void)bs.readInt32();
        flags        = bs.readUInt32();
        fileOffset   = bs.readUInt32();
        part0_Offset = bs.readUInt32();
        part0_ZSize  = bs.readUInt32();
        part0_Size   = bs.readUInt32();
        (void)bs.readInt32();
        part1_Offset = bs.readUInt32();
        part1_ZSize  = bs.readUInt32();
        part1_Size   = bs.readUInt32();
        (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32();
        (void)bs.readInt32(); (void)bs.readInt32();
        fileList_Length = bs.readUInt32();
        fileList_Begin  = bs.readUInt32();
        (void)bs.readInt32(); (void)bs.readInt32(); (void)bs.readInt32();
    }
};

struct RsgpInfo {
    RsgpHeadInfo       head;
    CompressStringList fileList{1};

    void read(UnifiedBinaryStream& bs) {
        size_t back = bs.getPosition();
        head.read(bs);
        bs.setPosition(back + static_cast<size_t>(head.fileList_Begin));
        size_t listBytes = static_cast<size_t>(head.fileList_Length);
        if (listBytes == 0) return;
        UnifiedBinaryStream tmp(UnifiedBinaryStream::Mode::Write);
        uint32_t divideFour = static_cast<uint32_t>(listBytes) >> 2;
        for (uint32_t i = 0; i < divideFour; ++i)
            tmp.writeInt32(bs.readInt32());
        fileList.read(tmp.getData());
    }
};

}