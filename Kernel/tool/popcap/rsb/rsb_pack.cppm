module;
#include <algorithm>
#include <atomic>
#include <charconv>
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <memory>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <thread>
#include <utility>
#include <vector>
export module tool.popcap.rsb.rsb_pack;
import tool.popcap.rsb.rsb_core;
import tool.popcap.rsb.rsb_utils;
import tool.popcap.rsb.rsb_definition;
import utility.io;
import utility.io.concurrent;
import utility.json;
import utility.xml.xml;
import utility.binary.unified_binary_stream;
import utility.zlib.zlib_compress;

export namespace Rsb {

struct ExtractedFile {
    std::string type, id;
    uint32_t default_format = 0;
};

struct ExtractedGroup {
    std::string id;
    uint32_t compressmethod;
    uint32_t poolindex;
    std::vector<ExtractedFile> files;
};

struct RsgpBuildResult {
    bool ok = true;
    std::string err;

    uint32_t part0_Size = 0;
    uint32_t part1_Size = 0;
    uint32_t part0_ZSize = 0;
    uint32_t part1_ZSize = 0;
    uint32_t ptx_Number = 0;
    uint32_t pool_Index = 0;
    uint32_t poolP1MaxOffset = 0;

    std::vector<uint8_t> p0comp;
    std::vector<uint8_t> p1comp;
    std::vector<uint8_t> fileListBytes;
    std::vector<RsbPtxInfo> ptxInfos;
    std::vector<CompressString> fileListEntries;
    CompressString rsgpListEntry;
};

[[nodiscard]] inline int parseConfigInt(std::string_view sv, int def) noexcept {
    if (sv.empty()) return def;
    int val = def;
    std::from_chars(sv.data(), sv.data() + sv.size(), val);
    return val;
}

[[nodiscard]] inline std::vector<uint8_t> compressBuffer(
    std::span<const uint8_t> data, int level, bool emptyFallbackOnFailure, uint8_t emptyHead
) {
    if (data.empty()) return {};
    auto r = zlib_ns::Compressor::compress(data, level);
    if (r) return std::move(*r);
    return emptyFallbackOnFailure
        ? std::vector<uint8_t>{0x78, emptyHead, 0x03, 0, 0, 0, 0, 0x01}
        : std::vector<uint8_t>(data.begin(), data.end());
}

[[nodiscard]] inline uint32_t readU32FromHdr(const uint8_t* hdr, int off, bool bigEndian) noexcept {
    uint32_t v;
    std::memcpy(&v, hdr + off, sizeof(v));
    return bigEndian ? __builtin_bswap32(v) : v;
}

inline void pack(std::string_view inFolder, std::string_view outFile) {
    namespace fs = std::filesystem;

    std::string inDir(inFolder);
    if (!inDir.empty() && inDir.back() != '/' && inDir.back() != '\\') inDir += '/';

    bool isOldWay = FileUtils::isDirectory(inDir + "POPSTUDIOINFO");
    std::string studioPath = isOldWay ? (inDir + "POPSTUDIOINFO/") : inDir;
    std::string resRootDir = isOldWay ? inDir : (inDir + "resource/");

    FileUtils::createDirectory(FileUtils::getParentDirectory(std::string(outFile)));

    int version = 3;
    auto endian = UnifiedBinaryStream::Endian::Little;
    bool compressPart0 = false, compressPart1 = true;
    uint32_t ptxEachLen = 0x10;
    bool smf = false, usePoolXml = false;
    int zlibLevel = 9;
    uint8_t emptyHead = 0x9C;

    std::vector<ExtractedGroup> rgGroupsExt;
    std::vector<RsbCompositeInfo> compositeInfosExt;
    std::vector<RsbAutoPoolInfo> autopoolList;
    bool hasResourcesConfig = false;
    std::vector<uint8_t> resourcesDatBytes;

    bool useJson = !isOldWay && FileUtils::fileExists(studioPath + "manifest.json");

    if (useJson) {
        auto docStr = FileUtils::readTextFile(studioPath + "manifest.json");
        auto doc = json::Document::parse(docStr);
        if (!doc) throw std::runtime_error("Failed to parse manifest.json");
        auto root = doc.root();

        auto pi = root.obj_get("pack_info");
        if (pi) {
            version = static_cast<int>(pi.obj_get("version").get_sint());
            endian = pi.obj_get("use_big_endian").get_bool() ? UnifiedBinaryStream::Endian::Big : UnifiedBinaryStream::Endian::Little;
            uint32_t f = static_cast<uint32_t>(pi.obj_get("compress_method").get_uint());
            compressPart0 = (f & 0b10) != 0; compressPart1 = (f & 0b01) != 0;
            std::string lvlStr(pi.obj_get("compress_level").get_str_view());
            if (lvlStr == "Fastest") { zlibLevel = 1; emptyHead = 0x01; }
            else if (lvlStr == "Smallest") { zlibLevel = 9; emptyHead = 0xDA; }
            else { zlibLevel = 9; emptyHead = 0x9C; }
            ptxEachLen = static_cast<uint32_t>(pi.obj_get("ptx_info_length").get_uint());
            smf = pi.obj_get("zlib_all").get_bool();
            usePoolXml = pi.obj_get("special_pool").get_bool();
        }

        auto pools = root.obj_get("pools");
        if (usePoolXml && pools) {
            for (auto pNode : pools.array()) {
                RsbAutoPoolInfo api;
                api.ID = std::string(pNode.obj_get("id").get_str_view());
                api.type = static_cast<int32_t>(pNode.obj_get("type").get_sint());
                autopoolList.push_back(std::move(api));
            }
        }

        auto rgs = root.obj_get("resources_group_info");
        if (rgs) {
            for (auto gNode : rgs.array()) {
                ExtractedGroup eg;
                eg.id = std::string(gNode.obj_get("id").get_str_view());
                eg.compressmethod = static_cast<uint32_t>(gNode.obj_get("compress_method").get_uint());
                eg.poolindex = usePoolXml
                    ? static_cast<uint32_t>(gNode.obj_get("pool_index").get_uint())
                    : static_cast<uint32_t>(rgGroupsExt.size());

                auto files = gNode.obj_get("files");
                if (files) {
                    for (auto fNode : files.array()) {
                        auto dfNode = fNode.obj_get("default_format");
                        eg.files.push_back({
                            std::string(fNode.obj_get("type").get_str_view()),
                            std::string(fNode.obj_get("id").get_str_view()),
                            dfNode ? static_cast<uint32_t>(dfNode.get_uint()) : 0
                        });
                    }
                }
                rgGroupsExt.push_back(std::move(eg));
            }
        }
        if (!usePoolXml) autopoolList.resize(rgGroupsExt.size());

        auto comps = root.obj_get("composite_resources_info");
        if (comps) {
            for (auto cNode : comps.array()) {
                RsbCompositeInfo ci;
                ci.ID = std::string(cNode.obj_get("id").get_str_view());
                uint32_t cidx = 0;
                auto groups = cNode.obj_get("groups");
                if (groups) {
                    for (auto gc : groups.array()) {
                        ci.child_Info[cidx].index = static_cast<uint32_t>(gc.obj_get("index").get_uint());
                        ci.child_Info[cidx].ratio = static_cast<uint32_t>(gc.obj_get("res").get_uint());
                        ci.child_Info[cidx].language = std::string(gc.obj_get("loc").get_str_view());
                        cidx++;
                    }
                }
                ci.child_Number = cidx;
                compositeInfosExt.push_back(std::move(ci));
            }
        }

        auto resNode = root.obj_get("resources");
        if (resNode) {
            hasResourcesConfig = true;
            resourcesDatBytes = RsbDefinition::jsonToDat(resNode, endian);
        }
    } else {
        if (!FileUtils::fileExists(studioPath + "PACKINFO.XML"))
            throw std::runtime_error("No manifest.json or PACKINFO.XML found");

        auto docRes = xml::Document::load_file(studioPath + "PACKINFO.XML");
        if (!docRes) throw std::runtime_error("Failed to load PACKINFO.XML");
        xml::Document doc = std::move(*docRes);
        auto piRoot = doc.child("PackInfo");

        for (auto child : piRoot.children()) {
            std::string name(child.name());
            if (name == "PackageVersion") { version = parseConfigInt(child.text(), 3); }
            else if (name == "UseBigEndian") {
                std::string v(child.text());
                endian = (v == "True" || v == "true") ? UnifiedBinaryStream::Endian::Big : UnifiedBinaryStream::Endian::Little;
            } else if (name == "CompressMethod") {
                uint32_t f = static_cast<uint32_t>(parseConfigInt(child.text(), 1));
                compressPart0 = (f & 0b10) != 0; compressPart1 = (f & 0b01) != 0;
            } else if (name == "PtxInfoLength") { ptxEachLen = static_cast<uint32_t>(parseConfigInt(child.text(), 0x10)); }
            else if (name == "ZlibAll") { std::string v(child.text()); smf = (v == "True" || v == "true"); }
            else if (name == "CompressLevel") {
                std::string v(child.text());
                if (v == "Fastest") { zlibLevel = 1; emptyHead = 0x01; }
                else if (v == "Smallest") { zlibLevel = 9; emptyHead = 0xDA; }
                else { zlibLevel = 9; emptyHead = 0x9C; }
            } else if (name == "SpecialPool") { std::string v(child.text()); usePoolXml = (v == "True" || v == "true"); }
        }

        uint32_t fGlobal = 0;
        if (compressPart0) fGlobal |= 0b10;
        if (compressPart1) fGlobal |= 0b01;

        if (usePoolXml) {
            auto poolDocRes = xml::Document::load_file(studioPath + "POOL.XML");
            if (poolDocRes) {
                xml::Document poolDoc = std::move(*poolDocRes);
                auto poolRoot = poolDoc.child("PoolInfo");
                for (auto pNode : poolRoot.children("Pool")) {
                    RsbAutoPoolInfo api;
                    api.ID = std::string(pNode.attribute("id").as_string(""));
                    api.type = static_cast<int32_t>(pNode.attribute("type").as_int(1));
                    autopoolList.push_back(std::move(api));
                }
            }
        }

        auto rgDocRes = xml::Document::load_file(studioPath + "RESOURCESGROUP.XML");
        if (!rgDocRes) throw std::runtime_error("Failed to load RESOURCESGROUP.XML");
        xml::Document rgDoc = std::move(*rgDocRes);
        auto rgRoot = rgDoc.child("ResourcesGroupInfo");

        for (auto g : rgRoot.children("Group")) {
            ExtractedGroup eg;
            eg.id = std::string(g.attribute("id").as_string(""));
            auto cmAttr = g.attribute("compressmethod");
            eg.compressmethod = cmAttr ? static_cast<uint32_t>(cmAttr.as_int()) : fGlobal;
            eg.poolindex = usePoolXml
                ? static_cast<uint32_t>(g.attribute("poolindex").as_int(static_cast<int>(rgGroupsExt.size())))
                : static_cast<uint32_t>(rgGroupsExt.size());
            for (auto child : g.children()) {
                eg.files.push_back({
                    std::string(child.name()),
                    std::string(child.attribute("id").as_string("")),
                    static_cast<uint32_t>(child.attribute("defaultformat").as_int(0))
                });
            }
            rgGroupsExt.push_back(std::move(eg));
        }
        if (!usePoolXml) autopoolList.resize(rgGroupsExt.size());

        auto crDocRes = xml::Document::load_file(studioPath + "COMPOSITERESOURCES.XML");
        if (!crDocRes) throw std::runtime_error("Failed to load COMPOSITERESOURCES.XML");
        xml::Document crDoc = std::move(*crDocRes);
        auto crRoot = crDoc.child("CompositeResourcesInfo");

        for (auto g : crRoot.children("CompositeResources")) {
            RsbCompositeInfo ci;
            ci.ID = std::string(g.attribute("id").as_string(""));
            uint32_t cidx = 0;
            for (auto gc : g.children("Group")) {
                ci.child_Info[cidx].index = static_cast<uint32_t>(gc.attribute("index").as_int(0));
                ci.child_Info[cidx].ratio = static_cast<uint32_t>(gc.attribute("res").as_int(0));
                ci.child_Info[cidx].language = std::string(gc.attribute("loc").as_string(""));
                ++cidx;
            }
            ci.child_Number = cidx;
            compositeInfosExt.push_back(std::move(ci));
        }

        std::string xmlResPath = studioPath + "RESOURCES.XML";
        if (FileUtils::fileExists(xmlResPath)) {
            hasResourcesConfig = true;
            resourcesDatBytes = RsbDefinition::xmlToDat(xmlResPath, endian);
        }
    }

    const bool bigEndian = (endian == UnifiedBinaryStream::Endian::Big);
    const uint32_t rsgpCount = static_cast<uint32_t>(rgGroupsExt.size());
    std::vector<RsgpBuildResult> results(rsgpCount);

    {
        const size_t nThreads = std::max<size_t>(1, std::thread::hardware_concurrency());

        for (size_t i = 0; i < rsgpCount; i += nThreads) {
            size_t batchEnd = std::min(i + nThreads, static_cast<size_t>(rsgpCount));
            Latch latch(static_cast<int>(batchEnd - i));

            for (size_t bi = i; bi < batchEnd; ++bi) {
                getGlobalPool().submit([&, bi]() {
                    auto& res = results[bi];
                    try {
                        auto& eg = rgGroupsExt[bi];
                        uint32_t f = eg.compressmethod;
                        bool cp0 = (f & 0b10) != 0;
                        bool cp1 = (f & 0b01) != 0;

                        res.pool_Index = eg.poolindex;
                        std::string rsgpID = eg.id;

                        struct FileEntry {
                            std::string id, path;
                            bool isImg;
                            int64_t fileSize;
                            size_t dataSize;
                            size_t bufOffset;
                            uint32_t localPtxIdx;
                            uint32_t w=0, h=0, check=0, format=0, alphaSize=0, alphaFormat=0;
                        };
                        std::vector<FileEntry> entries;
                        {
                            uint32_t ptxIdx = 0;
                            for (auto& child : eg.files) {
                                FileEntry fe;
                                fe.id = child.id;
                                fe.path = resRootDir + fe.id;
                                for (char& c : fe.path) if (c == '\\') c = '/';
                                fe.isImg = (child.type == "Img");
                                fe.fileSize = FileUtils::posixFileSize(fe.path);

                                if (fe.isImg && fe.fileSize < 0) {
                                    std::string pngPath = FileUtils::changeFileExtension(fe.path, ".png");
                                    if (!FileUtils::fileExists(pngPath))
                                        pngPath = FileUtils::changeFileExtension(fe.path, ".PNG");
                                    if (FileUtils::fileExists(pngPath))
                                        fe.fileSize = FileUtils::posixFileSize(pngPath);
                                }
                                if (fe.fileSize < 0)
                                    throw std::runtime_error("File not found: " + fe.path);

                                if (fe.isImg) {
                                    fe.localPtxIdx = ptxIdx++;
                                    fe.dataSize = (fe.fileSize >= 32) ? static_cast<size_t>(fe.fileSize - 32) : 0;
                                } else {
                                    fe.localPtxIdx = 0;
                                    fe.dataSize = static_cast<size_t>(fe.fileSize);
                                }
                                entries.push_back(std::move(fe));
                            }
                        }
                        const int N = static_cast<int>(entries.size());

                        if (N > 0) {
                            Latch latch1(N);
                            for (int j = 0; j < N; ++j) {
                                getGlobalPool().submitVoid([&, j]() {
                                    auto& fe = entries[static_cast<size_t>(j)];
                                    if (fe.isImg && fe.fileSize >= 32) {
                                        uint8_t hdr[32] = {};
                                        (void)FileUtils::posixReadInto(fe.path, hdr, 32, 0);
                                        fe.w = readU32FromHdr(hdr, 8, bigEndian);
                                        fe.h = readU32FromHdr(hdr, 12, bigEndian);
                                        fe.check = readU32FromHdr(hdr, 16, bigEndian);
                                        fe.format = readU32FromHdr(hdr, 20, bigEndian);
                                        fe.alphaSize = readU32FromHdr(hdr, 24, bigEndian);
                                        fe.alphaFormat = readU32FromHdr(hdr, 28, bigEndian);
                                    }
                                    latch1.countDown();
                                });
                            }
                            latch1.wait();
                        }

                        size_t p0Total = 0, p1Total = 0;
                        for (auto& fe : entries) {
                            if (fe.isImg) {
                                fe.bufOffset = p1Total;
                                p1Total = static_cast<size_t>(alignTo4K(static_cast<int64_t>(p1Total + fe.dataSize)));
                            } else {
                                fe.bufOffset = p0Total;
                                p0Total = static_cast<size_t>(alignTo4K(static_cast<int64_t>(p0Total + fe.dataSize)));
                            }
                        }

                        std::vector<uint8_t> p0buf;
                        std::vector<uint8_t> p1buf;
                        if (p0Total > 0) { p0buf.resize(p0Total); }
                        if (p1Total > 0) { p1buf.resize(p1Total); }

                        if (N > 0) {
                            Latch latch2(N);
                            for (int j = 0; j < N; ++j) {
                                getGlobalPool().submitVoid([&, j]() {
                                    auto& fe = entries[static_cast<size_t>(j)];
                                    if (fe.dataSize > 0) {
                                        uint8_t* dst = fe.isImg ? (p1buf.data() + fe.bufOffset) : (p0buf.data() + fe.bufOffset);
                                        off_t srcOff = fe.isImg ? 32 : 0;
                                        (void)FileUtils::posixReadInto(fe.path, dst, fe.dataSize, srcOff);
                                    }
                                    latch2.countDown();
                                });
                            }
                            latch2.wait();
                        }

                        res.fileListEntries.clear();
                        std::vector<RsbPtxInfo> ptxInfos;
                        uint32_t localPtxCount = 0;

                        CompressStringList rsgpFileList(1);

                        for (auto& fe : entries) {
                            res.fileListEntries.emplace_back(fe.id, std::make_shared<RsbExtraInfo>(static_cast<uint32_t>(bi)));

                            if (fe.isImg) {
                                rsgpFileList.add(CompressString(fe.id,
                                    std::make_shared<RsgpPart1ExtraInfo>(
                                        static_cast<uint32_t>(fe.bufOffset),
                                        static_cast<uint32_t>(fe.dataSize),
                                        localPtxCount, fe.w, fe.h)));
                                RsbPtxInfo ptx(ptxEachLen);
                                ptx.width = fe.w; ptx.height = fe.h;
                                ptx.check = fe.check; ptx.format = fe.format;
                                ptx.alphaSize = fe.alphaSize; ptx.alphaFormat = fe.alphaFormat;
                                ptxInfos.push_back(ptx);
                                ++localPtxCount;
                            } else {
                                rsgpFileList.add(CompressString(fe.id,
                                    std::make_shared<RsgpPart0ExtraInfo>(
                                        static_cast<uint32_t>(fe.bufOffset),
                                        static_cast<uint32_t>(fe.dataSize))));
                            }
                        }

                        res.ptxInfos = std::move(ptxInfos);
                        res.ptx_Number = localPtxCount;
                        res.part0_Size = static_cast<uint32_t>(p0Total);
                        res.part1_Size = static_cast<uint32_t>(p1Total);
                        res.fileListBytes = rsgpFileList.write();

                        std::string upperID = rsgpID;
                        std::ranges::transform(upperID, upperID.begin(), ::toupper);
                        res.rsgpListEntry = CompressString(upperID, std::make_shared<RsbExtraInfo>(static_cast<uint32_t>(bi)));

                        Latch latch3(2);
                        std::vector<uint8_t> p0comp, p1comp;

                        getGlobalPool().submitVoid([&]() {
                            if (cp0 && !p0buf.empty()) {
                                p0comp = compressBuffer(p0buf, zlibLevel, true, emptyHead);
                                size_t al = static_cast<size_t>(alignTo4K(static_cast<int64_t>(p0comp.size())));
                                p0comp.resize(al, 0);
                            } else {
                                p0comp = std::move(p0buf);
                            }
                            latch3.countDown();
                        });

                        getGlobalPool().submitVoid([&]() {
                            if (cp1 && !p1buf.empty()) {
                                p1comp = compressBuffer(p1buf, zlibLevel, false, emptyHead);
                                size_t al = static_cast<size_t>(alignTo4K(static_cast<int64_t>(p1comp.size())));
                                p1comp.resize(al, 0);
                            } else {
                                p1comp = std::move(p1buf);
                            }
                            latch3.countDown();
                        });

                        latch3.wait();

                        res.part0_ZSize = static_cast<uint32_t>(p0comp.size());
                        res.part1_ZSize = static_cast<uint32_t>(p1comp.size());
                        res.p0comp = std::move(p0comp);
                        res.p1comp = std::move(p1comp);
                        res.poolP1MaxOffset = 0;

                    } catch (const std::exception& e) {
                        res.ok = false; res.err = e.what();
                    }
                    latch.countDown();
                });
            }
            latch.wait();
        }
    }

    for (uint32_t i = 0; i < rsgpCount; ++i)
        if (!results[i].ok)
            throw std::runtime_error("RSB pack rsgp[" + u32str(i) + "]: " + results[i].err);

    CompressStringList fileList(0), rsgpList(0), compositeList(0);
    std::vector<RsbRsgpInfo> rsgpInfos(rsgpCount);
    std::vector<RsgpInfo> rsgps(rsgpCount);
    std::vector<RsbPtxInfo> ptxList;
    uint32_t ptxNumber = 0;

    for (uint32_t i = 0; i < rsgpCount; ++i) {
        for (auto& cs : results[i].fileListEntries) fileList.add(cs);
        rsgpList.add(results[i].rsgpListEntry);
        for (auto& ptx : results[i].ptxInfos) ptxList.push_back(ptx);

        rsgpInfos[i].ID = rgGroupsExt[i].id;
        rsgpInfos[i].flags = rgGroupsExt[i].compressmethod;
        rsgpInfos[i].pool_Index = results[i].pool_Index;
        rsgpInfos[i].ptx_BeforeNum = ptxNumber;
        rsgpInfos[i].ptx_Number = results[i].ptx_Number;
        rsgpInfos[i].part0_Size = rsgpInfos[i].part0_Size2 = results[i].part0_Size;
        rsgpInfos[i].part1_Size = results[i].part1_Size;
        rsgpInfos[i].part0_ZSize = results[i].part0_ZSize;
        rsgpInfos[i].part1_ZSize = results[i].part1_ZSize;

        rsgps[i].head.flags = rsgpInfos[i].flags;
        rsgps[i].head.version = version;
        rsgps[i].head.part0_Size = results[i].part0_Size;
        rsgps[i].head.part1_Size = results[i].part1_Size;
        rsgps[i].head.part0_ZSize = results[i].part0_ZSize;
        rsgps[i].head.part1_ZSize = results[i].part1_ZSize;
        rsgps[i].head.fileList_Length = static_cast<uint32_t>(results[i].fileListBytes.size());
        rsgps[i].head.fileList_Begin = RSGP_HEAD_FILELIST_BEGIN;

        ptxNumber += results[i].ptx_Number;
    }

    UnifiedBinaryStream rsgpFile(UnifiedBinaryStream::Mode::Write);
    rsgpFile.setEndian(endian);

    for (uint32_t i = 0; i < rsgpCount; ++i) {
        uint32_t off = static_cast<uint32_t>(rsgpFile.getPosition());
        rsgpInfos[i].offset = off;
        rsgpFile.setPosition(off + RSGP_HEAD_FILELIST_BEGIN);
        rsgpFile.writeBytes(results[i].fileListBytes);

        size_t cur = rsgpFile.getPosition();
        size_t aligned = static_cast<size_t>(alignTo4K(static_cast<int64_t>(cur)));
        if (aligned > cur) {
            rsgpFile.getData().resize(aligned, 0);
            rsgpFile.setPosition(aligned);
        }
        rsgpInfos[i].fileOffset = rsgpInfos[i].part0_Offset =
            rsgps[i].head.fileOffset = rsgps[i].head.part0_Offset =
            static_cast<uint32_t>(rsgpFile.getPosition() - off);

        rsgpFile.writeBytes(results[i].p0comp);
        results[i].p0comp.clear();
        results[i].p0comp.shrink_to_fit();

        uint32_t poolP1MaxOff = rsgpInfos[i].part0_Offset + rsgpInfos[i].part0_Size;

        rsgpInfos[i].part1_Offset = rsgps[i].head.part1_Offset =
            static_cast<uint32_t>(rsgpFile.getPosition() - off);

        rsgpFile.writeBytes(results[i].p1comp);
        results[i].p1comp.clear();
        results[i].p1comp.shrink_to_fit();

        size_t savedPos = rsgpFile.getPosition();
        rsgpFile.setPosition(off);
        rsgps[i].head.write(rsgpFile);
        rsgpFile.setPosition(savedPos);

        rsgpInfos[i].size = static_cast<uint32_t>(rsgpFile.getPosition() - off);

        auto& ap = autopoolList[rsgpInfos[i].pool_Index];
        ap.part1_MaxSize = std::max(ap.part1_MaxSize, rsgpInfos[i].part1_Size);
        ap.part1_MaxOffset_InDecompress = std::max(ap.part1_MaxOffset_InDecompress, poolP1MaxOff);
        if (!usePoolXml) ap.ID = rsgpInfos[i].ID + "_AutoPool";
    }

    RsbHeadInfo head;
    head.version = version;
    head.ptxInfo_EachLen = ptxEachLen;
    head.rsgp_Number = rsgpCount;
    head.autopool_Number = static_cast<uint32_t>(autopoolList.size());
    head.ptx_Number = ptxNumber;
    head.composite_Number = static_cast<uint32_t>(compositeInfosExt.size());

    for (size_t i = 0; i < compositeInfosExt.size(); ++i) {
        std::string upperID = compositeInfosExt[i].ID;
        std::ranges::transform(upperID, upperID.begin(), ::toupper);
        compositeList.add(CompressString(upperID, std::make_shared<RsbExtraInfo>(static_cast<uint32_t>(i))));
    }

    UnifiedBinaryStream bs(UnifiedBinaryStream::Mode::Write);
    bs.setEndian(endian);

    size_t headPlaceHolder = (version == 4) ? 112 : 108;
    bs.getData().resize(headPlaceHolder, 0);
    bs.setPosition(headPlaceHolder);

    auto fl = fileList.write();
    head.fileList_Length = static_cast<uint32_t>(fl.size());
    head.fileList_Begin = static_cast<uint32_t>(bs.getPosition());
    bs.writeBytes(fl);

    auto rl = rsgpList.write();
    head.rsgpList_Length = static_cast<uint32_t>(rl.size());
    head.rsgpList_Begin = static_cast<uint32_t>(bs.getPosition());
    bs.writeBytes(rl);

    head.compositeInfo_Begin = static_cast<uint32_t>(bs.getPosition());
    for (auto& ci : compositeInfosExt) ci.write(bs);

    auto cl = compositeList.write();
    head.compositeList_Length = static_cast<uint32_t>(cl.size());
    head.compositeList_Begin = static_cast<uint32_t>(bs.getPosition());
    bs.writeBytes(cl);

    head.rsgpInfo_Begin = static_cast<uint32_t>(bs.getPosition());
    {
        size_t sz = bs.getPosition() + RSB_HEAD_RSGPINFO_EACH * rsgpCount;
        bs.getData().resize(sz, 0);
        bs.setPosition(sz);
    }

    head.autopoolInfo_Begin = static_cast<uint32_t>(bs.getPosition());
    for (auto& ap : autopoolList) ap.write(bs);

    head.ptxInfo_Begin = static_cast<uint32_t>(bs.getPosition());
    for (auto& ptx : ptxList) ptx.write(bs);

    if (hasResourcesConfig) {
        if (bigEndian) {
            size_t aligned = static_cast<size_t>(alignTo4K(static_cast<int64_t>(bs.getPosition())));
            bs.getData().resize(aligned, 0);
            bs.setPosition(aligned);
        }
        auto& datBytes = resourcesDatBytes;
        uint32_t k, p2, p3;
        std::memcpy(&k, datBytes.data() + 8, 4);
        std::memcpy(&p2, datBytes.data() + 12, 4);
        std::memcpy(&p3, datBytes.data() + 16, 4);
        head.xmlPart1_Begin = static_cast<uint32_t>(bs.getPosition());
        head.xmlPart2_Begin = head.xmlPart1_Begin + p2 - k;
        head.xmlPart3_Begin = head.xmlPart1_Begin + p3 - k;
        bs.writeBytes(datBytes.data() + 20, datBytes.size() - 20);
    }

    {
        size_t aligned = static_cast<size_t>(alignTo4K(static_cast<int64_t>(bs.getPosition())));
        bs.getData().resize(aligned, 0);
        bs.setPosition(aligned);
    }
    head.headLength = static_cast<uint32_t>(bs.getPosition());

    for (uint32_t i = 0; i < rsgpCount; ++i) rsgpInfos[i].offset += head.headLength;

    bs.writeBytes(rsgpFile.getData());
    rsgpFile.getData().clear();
    rsgpFile.getData().shrink_to_fit();

    bs.setPosition(head.rsgpInfo_Begin);
    for (uint32_t i = 0; i < rsgpCount; ++i) rsgpInfos[i].write(bs);

    bs.setPosition(0);
    head.write(bs);

    if (smf) {
        auto comp = zlibCompress(bs.getData(), 9);
        if (comp.empty()) throw std::runtime_error("RSB: SMF compress failed");
        UnifiedBinaryStream smfBs(UnifiedBinaryStream::Mode::Write);
        smfBs.writeInt32(SMF_MAGIC);
        smfBs.writeInt32(static_cast<int32_t>(bs.getData().size()));
        smfBs.writeBytes(comp);
        FileUtils::writeFileBytes(std::string(outFile), smfBs.getData());
    } else {
        FileUtils::writeFileBytes(std::string(outFile), bs.getData());
    }
}

} 