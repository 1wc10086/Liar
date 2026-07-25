module;
#include <algorithm>
#include <atomic>
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <memory>
#include <mutex>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <system_error>
#include <unordered_map>
#include <unordered_set>
#include <utility>
#include <vector>
export module tool.popcap.rsb.rsb_unpack;
import tool.popcap.rsb.rsb_core;
import tool.popcap.rsb.rsb_utils;
import tool.popcap.rsb.rsb_definition;
import utility.io;
import utility.io.concurrent;
import utility.json;
import utility.binary.unified_binary_stream;
import utility.zlib.zlib_compress;
import utility.zlib.zlib_uncompress;
import tool.popcap.texture.ptx.ptx_core;

export namespace Rsb {

class LittleCursor {
public:
    explicit LittleCursor(std::span<const uint8_t> d) : m_data(d) {}

    [[nodiscard]] size_t position() const { return m_pos; }
    [[nodiscard]] size_t size() const { return m_data.size(); }
    [[nodiscard]] size_t remaining() const { return (m_pos <= m_data.size()) ? (m_data.size() - m_pos) : 0; }

    [[nodiscard]] uint32_t readU32() {
        if (remaining() < 4) throw std::runtime_error("compiled-map EOF");
        uint32_t v;
        std::memcpy(&v, m_data.data() + m_pos, sizeof(v));
        m_pos += 4;
        return v;
    }

private:
    std::span<const uint8_t> m_data;
    size_t m_pos = 0;
};

template<class Value, class Parser>
[[nodiscard]] std::vector<std::pair<std::string, Value>> decodeCompiledMapLenient(
    std::span<const uint8_t> data, Parser&& parser
) {
    LittleCursor stream(data);
    std::unordered_map<uint32_t, std::string> parentString;
    std::vector<std::pair<std::string, Value>> result;

    while (stream.remaining() >= 4) {
        try {
            std::string key;
            uint32_t positionUnit = static_cast<uint32_t>(stream.position() / 4);

            auto it = parentString.find(positionUnit);
            if (it != parentString.end()) {
                key += it->second;
                parentString.erase(it);
            }

            while (true) {
                uint32_t compositeValue = stream.readU32();
                uint32_t childStringOffset = (compositeValue >> 8);
                uint8_t currentCharacter = static_cast<uint8_t>(compositeValue & 0xFFu);

                if (childStringOffset != 0) parentString[childStringOffset] = key;
                if (currentCharacter == 0) break;
                key.push_back(static_cast<char>(currentCharacter));
            }

            if (key.empty()) break;
            Value value = parser(stream);
            result.emplace_back(std::move(key), std::move(value));
        } catch (...) { break; }
    }
    return result;
}

[[nodiscard]] std::unordered_map<uint32_t, std::string> invertIndexMap(
    const std::vector<std::pair<std::string, uint32_t>>& src
) {
    std::unordered_map<uint32_t, std::string> out;
    out.reserve(src.size());
    for (const auto& kv : src) {
        if (!kv.first.empty() && !out.count(kv.second))
            out.emplace(kv.second, kv.first);
    }
    return out;
}

struct LenientPacketEntry {
    uint32_t type   = 0;
    uint32_t offset = 0;
    uint32_t size   = 0;
    uint32_t index  = 0;
    int32_t  empty1 = 0;
    int32_t  empty2 = 0;
    uint32_t width  = 0;
    uint32_t height = 0;
};

[[nodiscard]] LenientPacketEntry parseLenientPacketEntry(LittleCursor& stream) {
    LenientPacketEntry v;
    v.type   = stream.readU32();
    v.offset = stream.readU32();
    v.size   = stream.readU32();
    if (v.type == 1 && stream.remaining() >= 20) {
        v.index  = stream.readU32();
        v.empty1 = static_cast<int32_t>(stream.readU32());
        v.empty2 = static_cast<int32_t>(stream.readU32());
        v.width  = stream.readU32();
        v.height = stream.readU32();
    }
    return v;
}

[[nodiscard]] std::vector<uint8_t> decodeSectionLenient(
    const uint8_t* wdPtr, size_t wdSize,
    size_t absoluteOffset, uint32_t zsize, uint32_t usize,
    int& compressLevel, bool& gotLevel
) {
    if (wdPtr == nullptr || absoluteOffset >= wdSize || zsize == 0) return {};
    size_t avail = std::min<size_t>(zsize, wdSize - absoluteOffset);
    auto sp = std::span<const uint8_t>(wdPtr + absoluteOffset, avail);

    if (!gotLevel && avail >= 2) {
        gotLevel = true;
        compressLevel = (uint16_t(sp[0]) | (uint16_t(sp[1]) << 8)) >> 14;
    }

    auto dec = zlib_ns::Decompressor::decompress(sp, usize);
    if (dec) return std::move(*dec);
    return std::vector<uint8_t>(sp.begin(), sp.end());
}

[[nodiscard]] std::string pickName(
    const std::unordered_map<uint32_t, std::string>& idxMap,
    uint32_t index, std::string_view fallback, const char* prefix
) {
    auto it = idxMap.find(index);
    if (it != idxMap.end() && !it->second.empty()) return it->second;
    if (!fallback.empty()) return std::string(fallback);
    return std::string(prefix) + ":" + u32str(index);
}

struct FileJsonInfo {
    std::string type, id;
    uint32_t default_format = 0;
};

[[gnu::always_inline]] inline void writePtxUint32(uint8_t* buf, int off, uint32_t v, bool bigEndian) noexcept {
    if (!bigEndian) {
        uint32_t le = v;
        std::memcpy(buf + off, &le, sizeof(le));
    } else {
        uint32_t be = __builtin_bswap32(v);
        std::memcpy(buf + off, &be, sizeof(be));
    }
}

inline void unpack(
    std::string_view inFile,
    std::string_view outFolder,
    bool changeImage = false,
    bool deleteAfterConvert = false,
    std::string_view fmt0Mode = "ARGB"
) {
    namespace fs = std::filesystem;

    if (!FileUtils::fileExists(std::string(inFile)))
        throw std::runtime_error("File not found");

    FileUtils::createDirectory(std::string(outFolder));
    std::string outDir(outFolder);
    if (!outDir.empty() && outDir.back() != '/' && outDir.back() != '\\') outDir += '/';

    auto rawData = FileUtils::readFileBytes(std::string(inFile));
    bool useSMF = false;
    std::vector<uint8_t> workData;

    {
        int32_t fw;
        std::memcpy(&fw, rawData.data(), sizeof(fw));
        if (fw == SMF_MAGIC) {
            uint32_t smfUncompressedSize;
            if (rawData.size() >= 12)
                std::memcpy(&smfUncompressedSize, rawData.data() + 4, sizeof(smfUncompressedSize));
            else smfUncompressedSize = 0;

            auto inner = std::span<const uint8_t>(rawData.data() + 8, rawData.size() - 8);
            auto dec = zlib_ns::Decompressor::decompress(inner, smfUncompressedSize);
            if (dec) { rawData.clear(); rawData.shrink_to_fit(); workData = std::move(*dec); }
            else throw std::runtime_error("RSB: SMF decompress failed");
            useSMF = true;
        } else {
            workData = std::move(rawData);
        }
    }

    const uint8_t* wdPtr = workData.data();
    const size_t wdSize = workData.size();
    UnifiedBinaryStream bs(workData);

    {
        int32_t magic = bs.peekInt32();
        if (magic == RSB_MAGIC_BE) bs.setEndian(UnifiedBinaryStream::Endian::Big);
        else if (magic != RSB_MAGIC) throw std::runtime_error("RSB: bad magic");
    }

    const bool bigEndian = (bs.getEndian() == UnifiedBinaryStream::Endian::Big);
    RsbHeadInfo head;
    head.read(bs);

    std::vector<RsbCompositeInfo> compositeInfos(head.composite_Number);
    bs.setPosition(head.compositeInfo_Begin);
    for (auto& ci : compositeInfos) ci.readImpl(bs);

    std::vector<RsbRsgpInfo> rsgpInfos(head.rsgp_Number);
    bs.setPosition(head.rsgpInfo_Begin);
    for (auto& ri : rsgpInfos) ri.read(bs);

    std::vector<RsbAutoPoolInfo> autopoolInfos(head.autopool_Number);
    bs.setPosition(head.autopoolInfo_Begin);
    for (auto& ai : autopoolInfos) ai.read(bs);

    std::vector<RsbPtxInfo> ptxInfos;
    ptxInfos.reserve(head.ptx_Number);
    bs.setPosition(head.ptxInfo_Begin);
    for (uint32_t i = 0; i < head.ptx_Number; ++i) {
        RsbPtxInfo pi(head.ptxInfo_EachLen);
        pi.read(bs);
        ptxInfos.push_back(pi);
    }

    std::vector<RsgpInfo> rsgps(head.rsgp_Number);
    for (uint32_t i = 0; i < head.rsgp_Number; ++i) {
        bs.setPosition(rsgpInfos[i].offset);
        rsgps[i].read(bs);
    }

    bool usePoolXml = false;
    if (head.rsgp_Number != head.autopool_Number) {
        usePoolXml = true;
    } else {
        for (uint32_t i = 0; i < head.rsgp_Number; ++i)
            if (rsgpInfos[i].pool_Index != i || autopoolInfos[i].type != 1) { usePoolXml = true; break; }
    }

    std::string resDir = outDir + "resource/";
    FileUtils::createDirectory(resDir);

    const uint32_t rsgpCount = head.rsgp_Number;
    std::atomic<int> atomicCompressLevel{-1};
    std::atomic<bool> atomicGotLevel{false};

    struct DecompResult {
        std::vector<uint8_t> p0, p1;
        bool ok = true;
        std::string err;
    };
    std::vector<DecompResult> decompResults(rsgpCount);

    if (rsgpCount > 0) {
        Latch latch(static_cast<int>(rsgpCount));
        for (uint32_t i = 0; i < rsgpCount; ++i) {
            getGlobalPool().submitVoid([&, i]() {
                auto& dr = decompResults[i];
                try {
                    auto decompressPart = [&](uint32_t off, uint32_t zs, uint32_t us) -> std::vector<uint8_t> {
                        size_t abs = static_cast<size_t>(rsgpInfos[i].offset) + off;
                        if (abs + zs > wdSize) throw std::runtime_error("RSB: OOB");
                        auto sp = std::span<const uint8_t>(wdPtr + abs, zs);
                        if (!atomicGotLevel.load(std::memory_order_relaxed) && zs >= 2) {
                            bool ex = false;
                            if (atomicGotLevel.compare_exchange_strong(ex, true, std::memory_order_acq_rel)) {
                                uint16_t h = uint16_t(sp[0]) | (uint16_t(sp[1]) << 8);
                                atomicCompressLevel.store(h >> 14, std::memory_order_release);
                            }
                        }
                        auto dec = zlib_ns::Decompressor::decompress(sp, us);
                        if (dec) return std::move(*dec);
                        return std::vector<uint8_t>(sp.begin(), sp.end());
                    };
                    dr.p0 = decompressPart(rsgps[i].head.part0_Offset, rsgps[i].head.part0_ZSize, rsgps[i].head.part0_Size);
                    dr.p1 = decompressPart(rsgps[i].head.part1_Offset, rsgps[i].head.part1_ZSize, rsgps[i].head.part1_Size);
                } catch (const std::exception& e) {
                    dr.ok = false; dr.err = e.what();
                }
                latch.countDown();
            });
        }
        latch.wait();
    }

    for (uint32_t i = 0; i < rsgpCount; ++i)
        if (!decompResults[i].ok)
            throw std::runtime_error("RSB decomp rsgp[" + u32str(i) + "]: " + decompResults[i].err);

    size_t totalFiles = 0;
    for (uint32_t i = 0; i < rsgpCount; ++i)
        totalFiles += static_cast<size_t>(rsgps[i].fileList.length());

    std::vector<std::vector<FileJsonInfo>> fileInfos(rsgpCount);
    for (uint32_t i = 0; i < rsgpCount; ++i)
        fileInfos[i].resize(static_cast<size_t>(rsgps[i].fileList.length()));

    {
        std::unordered_set<std::string> dirSet;
        dirSet.reserve(totalFiles / 8);
        for (uint32_t ri = 0; ri < rsgpCount; ++ri) {
            for (int fi = 0; fi < rsgps[ri].fileList.length(); ++fi) {
                std::string nname = resDir + rsgps[ri].fileList[fi].name;
                FileUtils::normalizePath(nname);
                dirSet.insert(std::string(FileUtils::getParentDir(nname)));
            }
        }
        for (auto& dir : dirSet) {
            std::error_code ec;
            fs::create_directories(dir, ec);
        }
    }

    std::atomic<bool> fileErr{false};
    std::mutex errMu;
    std::string errMsg;

    struct FTask { uint32_t ri; int fi; };
    std::vector<FTask> allTasks;
    allTasks.reserve(totalFiles);
    for (uint32_t i = 0; i < rsgpCount; ++i)
        for (int j = 0; j < rsgps[i].fileList.length(); ++j)
            allTasks.push_back({i, j});

    if (!allTasks.empty()) {
        const int total = static_cast<int>(allTasks.size());
        const int nThreads = static_cast<int>(getGlobalPool().threadCount());
        const int stripe = (total + nThreads - 1) / nThreads;
        const int nSubmit = std::min(nThreads, (total + stripe - 1) / stripe);
        Latch latch(nSubmit);

        for (int t = 0; t < nSubmit; ++t) {
            int s = t * stripe;
            int e = std::min(s + stripe, total);
            getGlobalPool().submitVoid([&, s, e]() {
                if (!fileErr.load(std::memory_order_relaxed)) {
                    try {
                        uint8_t ptxBuf[32];
                        for (int idx = s; idx < e; ++idx) {
                            const FTask& ft = allTasks[static_cast<size_t>(idx)];
                            const CompressString& str = rsgps[ft.ri].fileList[ft.fi];
                            const uint8_t* p0 = decompResults[ft.ri].p0.data();
                            const uint8_t* p1 = decompResults[ft.ri].p1.data();
                            std::string nname = resDir + str.name;
                            FileUtils::normalizePath(nname);

                            FileJsonInfo& fInfo = fileInfos[ft.ri][static_cast<size_t>(ft.fi)];
                            fInfo.id = str.name;

                            if (str.type == 1) {
                                auto* p0ei = static_cast<RsgpPart0ExtraInfo*>(str.extraInfo.get());
                                if (p0ei) FileUtils::posixWrite(nname, p0 + p0ei->offset, p0ei->size);
                                fInfo.type = "Res";
                            } else if (str.type == 2) {
                                auto* p1ei = static_cast<RsgpPart1ExtraInfo*>(str.extraInfo.get());
                                if (!p1ei) continue;
                                uint32_t ptxIdx = rsgpInfos[ft.ri].ptx_BeforeNum + p1ei->index;
                                const auto& ptx = ptxInfos[ptxIdx];

                                writePtxUint32(ptxBuf, 0,  0x70747831u, bigEndian);
                                writePtxUint32(ptxBuf, 4,  1u, bigEndian);
                                writePtxUint32(ptxBuf, 8,  ptx.width, bigEndian);
                                writePtxUint32(ptxBuf, 12, ptx.height, bigEndian);
                                writePtxUint32(ptxBuf, 16, ptx.check, bigEndian);
                                writePtxUint32(ptxBuf, 20, ptx.format, bigEndian);
                                writePtxUint32(ptxBuf, 24, ptx.alphaSize, bigEndian);
                                writePtxUint32(ptxBuf, 28, ptx.alphaFormat, bigEndian);
                                FileUtils::posixWriteV(nname, ptxBuf, 32, p1 + p1ei->offset, p1ei->size);

                                if (changeImage) {
                                    std::string png = FileUtils::changeFileExtension(nname, ".png");
                                    try {
                                        ImagePtxCodec::decode(nname, png, true, "", 0, 0, std::string(fmt0Mode));
                                        if (deleteAfterConvert) FileUtils::deleteFile(nname);
                                    } catch (...) {}
                                }
                                fInfo.type = "Img";
                                fInfo.default_format = ptx.format;
                            }
                        }
                    } catch (const std::exception& ex) {
                        std::lock_guard<std::mutex> lk(errMu);
                        if (!fileErr.exchange(true)) errMsg = ex.what();
                    }
                }
                latch.countDown();
            });
        }
        latch.wait();
    }

    if (fileErr.load()) throw std::runtime_error("RSB file extract: " + errMsg);
    decompResults.clear();
    decompResults.shrink_to_fit();

    json::MutDocument jdoc;
    auto root = jdoc.mut_obj();
    jdoc.set_root(root);

    auto packInfo = jdoc.mut_obj();
    jdoc.obj_add_int(packInfo, "version", head.version);
    jdoc.obj_add_bool(packInfo, "use_big_endian", bigEndian);
    packInfo.obj_add(jdoc.mut_str("compress_method"), jdoc.mut_uint(!rsgps.empty() ? (rsgps[0].head.flags & 0b11) : 1u));
    int lvl = atomicCompressLevel.load(std::memory_order_acquire);
    const char* ls = lvl < 0 ? "Optimal" : lvl == 0 ? "Fastest" : lvl == 3 ? "Smallest" : "Optimal";
    packInfo.obj_add(jdoc.mut_str("compress_level"), jdoc.mut_str(ls));
    packInfo.obj_add(jdoc.mut_str("ptx_info_length"), jdoc.mut_uint(head.ptxInfo_EachLen));
    jdoc.obj_add_bool(packInfo, "zlib_all", useSMF);
    jdoc.obj_add_bool(packInfo, "special_pool", usePoolXml);
    root.obj_add(jdoc.mut_str("pack_info"), packInfo);

    if (usePoolXml) {
        auto pools = jdoc.mut_arr();
        for (auto& ai : autopoolInfos) {
            auto pool = jdoc.mut_obj();
            pool.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(ai.ID));
            jdoc.obj_add_int(pool, "type", ai.type);
            pools.arr_append(pool);
        }
        root.obj_add(jdoc.mut_str("pools"), pools);
    }

    auto composites = jdoc.mut_arr();
    for (auto& ci : compositeInfos) {
        auto comp = jdoc.mut_obj();
        comp.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(ci.ID));
        auto groups = jdoc.mut_arr();
        for (uint32_t j = 0; j < ci.child_Number; ++j) {
            auto grp = jdoc.mut_obj();
            grp.obj_add(jdoc.mut_str("index"), jdoc.mut_uint(ci.child_Info[j].index));
            grp.obj_add(jdoc.mut_str("res"), jdoc.mut_uint(ci.child_Info[j].ratio));
            grp.obj_add(jdoc.mut_str("loc"), jdoc.mut_strdup(ci.child_Info[j].language));
            groups.arr_append(grp);
        }
        comp.obj_add(jdoc.mut_str("groups"), groups);
        composites.arr_append(comp);
    }
    root.obj_add(jdoc.mut_str("composite_resources_info"), composites);

    auto resGroups = jdoc.mut_arr();
    for (uint32_t i = 0; i < rsgpCount; ++i) {
        auto grp = jdoc.mut_obj();
        grp.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(rsgpInfos[i].ID));
        grp.obj_add(jdoc.mut_str("compress_method"), jdoc.mut_uint(rsgpInfos[i].flags));
        if (usePoolXml) grp.obj_add(jdoc.mut_str("pool_index"), jdoc.mut_uint(rsgpInfos[i].pool_Index));

        auto files = jdoc.mut_arr();
        for (auto& f : fileInfos[i]) {
            auto fileObj = jdoc.mut_obj();
            fileObj.obj_add(jdoc.mut_str("type"), jdoc.mut_strdup(f.type));
            fileObj.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(f.id));
            if (f.type == "Img") fileObj.obj_add(jdoc.mut_str("default_format"), jdoc.mut_uint(f.default_format));
            files.arr_append(fileObj);
        }
        grp.obj_add(jdoc.mut_str("files"), files);
        resGroups.arr_append(grp);
    }
    root.obj_add(jdoc.mut_str("resources_group_info"), resGroups);

    if (head.xmlPart1_Begin != 0 && head.headLength > head.xmlPart1_Begin && head.headLength <= wdSize) {
        size_t sz = head.headLength - head.xmlPart1_Begin;
        std::vector<uint8_t> dat;
        dat.reserve(20 + sz);
        auto au32 = [&](uint32_t v) {
            dat.push_back(v & 0xFF); dat.push_back((v >> 8) & 0xFF);
            dat.push_back((v >> 16) & 0xFF); dat.push_back((v >> 24) & 0xFF);
        };
        au32(static_cast<uint32_t>(XMLDAT_MAGIC));
        au32(static_cast<uint32_t>(XMLDAT_VERSION));
        au32(0x14u);
        au32(head.xmlPart2_Begin - head.xmlPart1_Begin + 0x14u);
        au32(head.xmlPart3_Begin - head.xmlPart1_Begin + 0x14u);
        dat.insert(dat.end(), wdPtr + head.xmlPart1_Begin, wdPtr + head.xmlPart1_Begin + sz);
        auto resJson = RsbDefinition::datToJson(dat, jdoc);
        root.obj_add(jdoc.mut_str("resources"), resJson);
    }

    std::string jsonStr = jdoc.write(json::WriteFlag::Pretty | json::WriteFlag::EscapeUnicode);
    FileUtils::posixWrite(outDir + "manifest.json", jsonStr.data(), jsonStr.size());
}

inline void unpackLenient(
    std::string_view inFile,
    std::string_view outFolder,
    bool changeImage = false,
    bool deleteAfterConvert = false,
    std::string_view fmt0Mode = "ARGB"
) {
    namespace fs = std::filesystem;

    if (!FileUtils::fileExists(std::string(inFile)))
        throw std::runtime_error("File not found");

    FileUtils::createDirectory(std::string(outFolder));
    std::string outDir(outFolder);
    if (!outDir.empty() && outDir.back() != '/' && outDir.back() != '\\') outDir += '/';

    auto rawData = FileUtils::readFileBytes(std::string(inFile));
    bool useSMF = false;
    std::vector<uint8_t> workData;

    if (rawData.size() < 4) throw std::runtime_error("RSB: file too small");

    {
        int32_t fw;
        std::memcpy(&fw, rawData.data(), sizeof(fw));
        if (fw == SMF_MAGIC) {
            if (rawData.size() < 8) throw std::runtime_error("RSB: bad SMF header");
            uint32_t smfUncompressedSize;
            std::memcpy(&smfUncompressedSize, rawData.data() + 4, sizeof(smfUncompressedSize));
            auto inner = std::span<const uint8_t>(rawData.data() + 8, rawData.size() - 8);

            auto dec = zlib_ns::Decompressor::decompress(inner, smfUncompressedSize);
            if (dec) { rawData.clear(); rawData.shrink_to_fit(); workData = std::move(*dec); }
            else throw std::runtime_error("RSB: SMF decompress failed");
            useSMF = true;
        } else {
            workData = std::move(rawData);
        }
    }

    if (workData.size() < 0x20) throw std::runtime_error("RSB: decompressed data too small");

    const uint8_t* wdPtr = workData.data();
    const size_t wdSize = workData.size();

    UnifiedBinaryStream bs(workData);
    {
        int32_t magic = bs.peekInt32();
        if (magic == RSB_MAGIC_BE) bs.setEndian(UnifiedBinaryStream::Endian::Big);
        else if (magic != RSB_MAGIC) throw std::runtime_error("RSB: bad magic");
    }
    const bool bigEndian = (bs.getEndian() == UnifiedBinaryStream::Endian::Big);

    RsbHeadInfo head;
    head.read(bs);

    const uint32_t safePtxEachLen =
        (head.ptxInfo_EachLen == 0x10 || head.ptxInfo_EachLen == 0x14 || head.ptxInfo_EachLen == 0x18)
        ? head.ptxInfo_EachLen : 0x10;

    const uint32_t compositeCount = safeClampCountByRegion(
        wdSize, head.compositeInfo_Begin, head.composite_Number, RSB_HEAD_COMPOSITE_EACH);
    const uint32_t rsgpCount = safeClampCountByRegion(
        wdSize, head.rsgpInfo_Begin, head.rsgp_Number, RSB_HEAD_RSGPINFO_EACH);
    const uint32_t autopoolCount = safeClampCountByRegion(
        wdSize, head.autopoolInfo_Begin, head.autopool_Number, RSB_HEAD_AUTOPOOL_EACH);
    const uint32_t ptxCount = safeClampCountByRegion(
        wdSize, head.ptxInfo_Begin, head.ptx_Number, safePtxEachLen);

    std::vector<std::string> lenientErrors;

    auto compositeListRaw = readCanonicalListBytes(
        wdPtr, wdSize, head.compositeList_Begin, head.compositeList_Length, bigEndian);
    auto rsgpListRaw = readCanonicalListBytes(
        wdPtr, wdSize, head.rsgpList_Begin, head.rsgpList_Length, bigEndian);

    auto compositeNameIndex = decodeCompiledMapLenient<uint32_t>(
        compositeListRaw, [](LittleCursor& c) -> uint32_t { return c.readU32(); });
    auto rsgpNameIndex = decodeCompiledMapLenient<uint32_t>(
        rsgpListRaw, [](LittleCursor& c) -> uint32_t { return c.readU32(); });

    auto compositeIndexName = invertIndexMap(compositeNameIndex);
    auto rsgpIndexName = invertIndexMap(rsgpNameIndex);

    std::vector<RsbCompositeInfo> compositeInfos;
    compositeInfos.reserve(compositeCount);
    try {
        bs.setPosition(head.compositeInfo_Begin);
        for (uint32_t i = 0; i < compositeCount; ++i) {
            RsbCompositeInfo ci;
            ci.readImpl(bs);
            ci.ID = pickName(compositeIndexName, i, ci.ID, "<unknown_group>");
            compositeInfos.push_back(std::move(ci));
        }
    } catch (const std::exception& e) {
        lenientErrors.emplace_back("read composite infos failed: " + std::string(e.what()));
    }

    std::vector<RsbRsgpInfo> rsgpInfos;
    rsgpInfos.reserve(rsgpCount);
    try {
        bs.setPosition(head.rsgpInfo_Begin);
        for (uint32_t i = 0; i < rsgpCount; ++i) {
            RsbRsgpInfo ri;
            ri.read(bs);
            ri.ID = pickName(rsgpIndexName, i, ri.ID, "<unknown_subgroup>");
            rsgpInfos.push_back(std::move(ri));
        }
    } catch (const std::exception& e) {
        lenientErrors.emplace_back("read rsgp infos failed: " + std::string(e.what()));
    }

    std::vector<RsbAutoPoolInfo> autopoolInfos;
    autopoolInfos.reserve(autopoolCount);
    try {
        bs.setPosition(head.autopoolInfo_Begin);
        for (uint32_t i = 0; i < autopoolCount; ++i) {
            RsbAutoPoolInfo ai;
            ai.read(bs);
            autopoolInfos.push_back(std::move(ai));
        }
    } catch (const std::exception& e) {
        lenientErrors.emplace_back("read autopool infos failed: " + std::string(e.what()));
    }

    std::vector<RsbPtxInfo> ptxInfos;
    ptxInfos.reserve(ptxCount);
    try {
        bs.setPosition(head.ptxInfo_Begin);
        for (uint32_t i = 0; i < ptxCount; ++i) {
            RsbPtxInfo pi(safePtxEachLen);
            pi.read(bs);
            ptxInfos.push_back(pi);
        }
    } catch (const std::exception& e) {
        lenientErrors.emplace_back("read ptx infos failed: " + std::string(e.what()));
    }

    bool usePoolXml = false;
    if (rsgpInfos.size() != autopoolInfos.size()) {
        usePoolXml = true;
    } else {
        for (uint32_t i = 0; i < static_cast<uint32_t>(rsgpInfos.size()); ++i) {
            if (rsgpInfos[i].pool_Index != i || autopoolInfos[i].type != 1) { usePoolXml = true; break; }
        }
    }

    std::string resDir = outDir + "resource/";
    FileUtils::createDirectory(resDir);

    std::vector<std::vector<FileJsonInfo>> fileInfos(rsgpInfos.size());
    int compressLevel = -1;
    bool gotCompressLevel = false;

    uint8_t ptxBuf[32];

    for (uint32_t i = 0; i < static_cast<uint32_t>(rsgpInfos.size()); ++i) {
        try {
            const auto& ri = rsgpInfos[i];
            if (ri.offset >= wdSize) {
                lenientErrors.emplace_back("rsgp[" + u32str(i) + "] packet offset OOB");
                continue;
            }

            bs.setPosition(ri.offset);
            RsgpHeadInfo phead;
            phead.read(bs);

            uint32_t fileListBegin = (phead.fileList_Begin != 0) ? phead.fileList_Begin : RSGP_HEAD_FILELIST_BEGIN;
            uint32_t fileListLength = phead.fileList_Length;

            uint32_t part0Offset = (phead.part0_Offset != 0) ? phead.part0_Offset : ri.part0_Offset;
            uint32_t part0ZSize  = (phead.part0_ZSize  != 0) ? phead.part0_ZSize  : ri.part0_ZSize;
            uint32_t part0Size   = (phead.part0_Size   != 0) ? phead.part0_Size   : ri.part0_Size;

            uint32_t part1Offset = (phead.part1_Offset != 0) ? phead.part1_Offset : ri.part1_Offset;
            uint32_t part1ZSize  = (phead.part1_ZSize  != 0) ? phead.part1_ZSize  : ri.part1_ZSize;
            uint32_t part1Size   = (phead.part1_Size   != 0) ? phead.part1_Size   : ri.part1_Size;

            auto packetListRaw = readCanonicalListBytes(
                wdPtr, wdSize, ri.offset + fileListBegin, fileListLength, bigEndian);

            auto packetEntries = decodeCompiledMapLenient<LenientPacketEntry>(
                packetListRaw,
                [](LittleCursor& c) -> LenientPacketEntry { return parseLenientPacketEntry(c); });

            auto p0 = decodeSectionLenient(
                wdPtr, wdSize, ri.offset + part0Offset,
                part0ZSize, part0Size, compressLevel, gotCompressLevel);

            auto p1 = decodeSectionLenient(
                wdPtr, wdSize, ri.offset + part1Offset,
                part1ZSize, part1Size, compressLevel, gotCompressLevel);

            fileInfos[i].reserve(packetEntries.size());

            for (auto& kv : packetEntries) {
                const std::string& relName = kv.first;
                const auto& ent = kv.second;

                FileJsonInfo fj;
                fj.id = relName;

                std::string nname = resDir + relName;
                FileUtils::normalizePath(nname);
                {
                    std::error_code ec;
                    fs::create_directories(fs::path(nname).parent_path(), ec);
                }

                if (ent.type == 0) {
                    if (ent.offset > p0.size()) {
                        lenientErrors.emplace_back("rsgp[" + u32str(i) + "] file '" + relName + "' part0 offset OOB");
                        continue;
                    }
                    size_t avail = std::min<size_t>(ent.size, p0.size() - ent.offset);
                    FileUtils::posixWrite(nname, p0.data() + ent.offset, avail);
                    fj.type = "Res";
                } else if (ent.type == 1) {
                    if (ent.offset > p1.size()) {
                        lenientErrors.emplace_back("rsgp[" + u32str(i) + "] file '" + relName + "' part1 offset OOB");
                        continue;
                    }
                    size_t pixelAvail = std::min<size_t>(ent.size, p1.size() - ent.offset);

                    uint32_t width = ent.width, height = ent.height;
                    uint32_t check = 0, format = 0, alphaSize = 0, alphaFormat = 0;

                    uint32_t ptxIdx = ri.ptx_BeforeNum + ent.index;
                    if (ptxIdx < ptxInfos.size()) {
                        const auto& ptx = ptxInfos[ptxIdx];
                        width = ptx.width; height = ptx.height;
                        check = ptx.check; format = ptx.format;
                        alphaSize = ptx.alphaSize; alphaFormat = ptx.alphaFormat;
                    }

                    writePtxUint32(ptxBuf, 0,  0x70747831u, bigEndian);
                    writePtxUint32(ptxBuf, 4,  1u, bigEndian);
                    writePtxUint32(ptxBuf, 8,  width, bigEndian);
                    writePtxUint32(ptxBuf, 12, height, bigEndian);
                    writePtxUint32(ptxBuf, 16, check, bigEndian);
                    writePtxUint32(ptxBuf, 20, format, bigEndian);
                    writePtxUint32(ptxBuf, 24, alphaSize, bigEndian);
                    writePtxUint32(ptxBuf, 28, alphaFormat, bigEndian);

                    FileUtils::posixWriteV(nname, ptxBuf, 32, p1.data() + ent.offset, pixelAvail);

                    if (changeImage) {
                        std::string png = FileUtils::changeFileExtension(nname, ".png");
                        try {
                            ImagePtxCodec::decode(nname, png, true, "", 0, 0, std::string(fmt0Mode));
                            if (deleteAfterConvert) FileUtils::deleteFile(nname);
                        } catch (...) {}
                    }

                    fj.type = "Img";
                    fj.default_format = format;
                } else {
                    lenientErrors.emplace_back("rsgp[" + u32str(i) + "] file '" + relName + "' unknown entry type: " + u32str(ent.type));
                    continue;
                }

                fileInfos[i].push_back(std::move(fj));
            }
        } catch (const std::exception& e) {
            lenientErrors.emplace_back("rsgp[" + u32str(i) + "] failed: " + std::string(e.what()));
        }
    }

    json::MutDocument jdoc;
    auto root = jdoc.mut_obj();
    jdoc.set_root(root);

    auto packInfo = jdoc.mut_obj();
    jdoc.obj_add_int(packInfo, "version", head.version);
    jdoc.obj_add_bool(packInfo, "use_big_endian", bigEndian);
    uint32_t cm = !rsgpInfos.empty() ? (rsgpInfos[0].flags & 0b11u) : 1u;
    packInfo.obj_add(jdoc.mut_str("compress_method"), jdoc.mut_uint(cm));
    const char* lvlStr = compressLevel < 0 ? "Optimal" : compressLevel == 0 ? "Fastest" : "Smallest";
    packInfo.obj_add(jdoc.mut_str("compress_level"), jdoc.mut_str(lvlStr));
    packInfo.obj_add(jdoc.mut_str("ptx_info_length"), jdoc.mut_uint(head.ptxInfo_EachLen));
    jdoc.obj_add_bool(packInfo, "zlib_all", useSMF);
    jdoc.obj_add_bool(packInfo, "special_pool", usePoolXml);
    root.obj_add(jdoc.mut_str("pack_info"), packInfo);

    if (usePoolXml) {
        auto pools = jdoc.mut_arr();
        for (uint32_t i = 0; i < static_cast<uint32_t>(autopoolInfos.size()); ++i) {
            auto& ai = autopoolInfos[i];
            auto pool = jdoc.mut_obj();
            pool.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(ai.ID.empty() ? ("<pool>:" + u32str(i)) : ai.ID));
            jdoc.obj_add_int(pool, "type", ai.type);
            pools.arr_append(pool);
        }
        root.obj_add(jdoc.mut_str("pools"), pools);
    }

    auto composites = jdoc.mut_arr();
    for (uint32_t i = 0; i < static_cast<uint32_t>(compositeInfos.size()); ++i) {
        auto& ci = compositeInfos[i];
        auto comp = jdoc.mut_obj();
        comp.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(
            ci.ID.empty() ? pickName(compositeIndexName, i, ci.ID, "<unknown_group>") : ci.ID));
        auto groups = jdoc.mut_arr();
        uint32_t childCount = std::min<uint32_t>(ci.child_Number, 0x40u);
        for (uint32_t j = 0; j < childCount; ++j) {
            auto grp = jdoc.mut_obj();
            grp.obj_add(jdoc.mut_str("index"), jdoc.mut_uint(ci.child_Info[j].index));
            grp.obj_add(jdoc.mut_str("res"), jdoc.mut_uint(ci.child_Info[j].ratio));
            grp.obj_add(jdoc.mut_str("loc"), jdoc.mut_strdup(ci.child_Info[j].language));
            groups.arr_append(grp);
        }
        comp.obj_add(jdoc.mut_str("groups"), groups);
        composites.arr_append(comp);
    }
    root.obj_add(jdoc.mut_str("composite_resources_info"), composites);

    auto resGroups = jdoc.mut_arr();
    for (uint32_t i = 0; i < static_cast<uint32_t>(rsgpInfos.size()); ++i) {
        auto grp = jdoc.mut_obj();
        grp.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(
            rsgpInfos[i].ID.empty() ? pickName(rsgpIndexName, i, rsgpInfos[i].ID, "<unknown_subgroup>") : rsgpInfos[i].ID));
        grp.obj_add(jdoc.mut_str("compress_method"), jdoc.mut_uint(rsgpInfos[i].flags));
        if (usePoolXml) grp.obj_add(jdoc.mut_str("pool_index"), jdoc.mut_uint(rsgpInfos[i].pool_Index));
        auto files = jdoc.mut_arr();
        for (auto& f : fileInfos[i]) {
            auto fo = jdoc.mut_obj();
            fo.obj_add(jdoc.mut_str("type"), jdoc.mut_strdup(f.type));
            fo.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(f.id));
            if (f.type == "Img") fo.obj_add(jdoc.mut_str("default_format"), jdoc.mut_uint(f.default_format));
            files.arr_append(fo);
        }
        grp.obj_add(jdoc.mut_str("files"), files);
        resGroups.arr_append(grp);
    }
    root.obj_add(jdoc.mut_str("resources_group_info"), resGroups);

    if (head.xmlPart1_Begin != 0 && head.headLength > head.xmlPart1_Begin && head.headLength <= wdSize) {
        try {
            size_t sz = head.headLength - head.xmlPart1_Begin;
            std::vector<uint8_t> dat;
            dat.reserve(20 + sz);
            auto au32 = [&](uint32_t v) {
                dat.push_back(v & 0xFF); dat.push_back((v >> 8) & 0xFF);
                dat.push_back((v >> 16) & 0xFF); dat.push_back((v >> 24) & 0xFF);
            };
            au32(static_cast<uint32_t>(XMLDAT_MAGIC)); au32(static_cast<uint32_t>(XMLDAT_VERSION));
            au32(0x14u);
            au32(head.xmlPart2_Begin - head.xmlPart1_Begin + 0x14u);
            au32(head.xmlPart3_Begin - head.xmlPart1_Begin + 0x14u);
            dat.insert(dat.end(), wdPtr + head.xmlPart1_Begin, wdPtr + head.xmlPart1_Begin + sz);
            auto resJson = RsbDefinition::datToJson(dat, jdoc);
            root.obj_add(jdoc.mut_str("resources"), resJson);
        } catch (const std::exception& e) {
            lenientErrors.emplace_back("resources/xmldat parse failed: " + std::string(e.what()));
        }
    }

    if (!lenientErrors.empty()) {
        auto errs = jdoc.mut_arr();
        for (auto& e : lenientErrors) errs.arr_append(jdoc.mut_strdup(e));
        root.obj_add(jdoc.mut_str("lenient_errors"), errs);
    }

    std::string jsonStr = jdoc.write(json::WriteFlag::Pretty | json::WriteFlag::EscapeUnicode);
    FileUtils::posixWrite(outDir + "manifest.json", jsonStr.data(), jsonStr.size());
}

}