module;
#include <algorithm>
#include <cstdint>
#include <cstring>
#include <memory>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>
export module tool.popcap.rsb.rsb_definition;
import tool.popcap.rsb.rsb_core;
import utility.io;
import utility.json;
import utility.xml.xml;
import utility.binary.unified_binary_stream;

export namespace Rsb {


struct XmlPtxInfo {
    uint16_t type=0, aflags=0, x=0, y=0, ax=0, ay=0, aw=0, ah=0, rows=1, cols=1;
    std::string parent;
};

struct XmlPropertiesInfo {
    std::string key, value;
};

struct XmlResourcesInfo {
    uint16_t type=0;
    std::string id, path;
    XmlPtxInfo* ptxInfo=nullptr;
    int ptxInfoBegin=0, ptxInfoEnd=0;
    std::vector<XmlPropertiesInfo> properties;
    int infoOffsetInPart2=0;
    ~XmlResourcesInfo() { delete ptxInfo; }
};

struct XmlRsgpInfo {
    int resolutionRatio=0;
    std::string language, id;
    std::vector<XmlResourcesInfo*> resources;
    ~XmlRsgpInfo() { for (auto* p : resources) delete p; }
};

struct XmlCompositeInfo {
    std::string id;
    std::vector<XmlRsgpInfo*> rsgps;
    ~XmlCompositeInfo() { for (auto* p : rsgps) delete p; }
};

[[nodiscard]] std::string_view gsPart3(int off, const char* base3, size_t lim3) {
    if (off < 0 || static_cast<size_t>(off) >= lim3) return {};
    const char* p = base3 + off;
    return {p, ::strnlen(p, lim3 - static_cast<size_t>(off))};
}

[[nodiscard]] std::vector<std::unique_ptr<XmlCompositeInfo>> parseDatToStructs(const std::vector<uint8_t>& dat) {
    UnifiedBinaryStream bs(dat);
    (void)bs.readInt32(); (void)bs.readInt32();
    int p1b=bs.readInt32(), p2b=bs.readInt32(), p3b=bs.readInt32();
    const char* base3 = reinterpret_cast<const char*>(dat.data() + static_cast<size_t>(p3b));
    size_t lim3 = dat.size() - static_cast<size_t>(p3b);

    std::vector<std::unique_ptr<XmlCompositeInfo>> cl;
    bs.setPosition(static_cast<size_t>(p1b));

    while (static_cast<int>(bs.getPosition()) < p2b) {
        auto comp = std::make_unique<XmlCompositeInfo>();
        comp->id = gsPart3(bs.readInt32(), base3, lim3);
        int rn = bs.readInt32(); (void)bs.readInt32();
        comp->rsgps.resize(static_cast<size_t>(rn));

        for (int j = 0; j < rn; ++j) {
            auto rsgp = std::make_unique<XmlRsgpInfo>();
            rsgp->resolutionRatio = bs.readInt32();
            uint8_t lb[4]; for (int k = 0; k < 4; ++k) lb[k] = bs.readUInt8();
            for (int k = 0; k < 4; ++k) if (lb[k]) rsgp->language += static_cast<char>(lb[k]);
            rsgp->id = gsPart3(bs.readInt32(), base3, lim3);
            int resn = bs.readInt32();
            rsgp->resources.resize(static_cast<size_t>(resn));

            for (int k = 0; k < resn; ++k) {
                auto r = std::make_unique<XmlResourcesInfo>();
                r->infoOffsetInPart2 = bs.readInt32();
                rsgp->resources[static_cast<size_t>(k)] = r.release();
            }
            comp->rsgps[static_cast<size_t>(j)] = rsgp.release();
        }

        size_t sv = bs.getPosition();
        for (int j = 0; j < rn; ++j) {
            for (int k = 0; k < static_cast<int>(comp->rsgps[static_cast<size_t>(j)]->resources.size()); ++k) {
                auto* res = comp->rsgps[static_cast<size_t>(j)]->resources[static_cast<size_t>(k)];
                bs.setPosition(static_cast<size_t>(p2b) + static_cast<size_t>(res->infoOffsetInPart2));
                (void)bs.readInt32(); res->type = bs.readUInt16(); (void)bs.readUInt16();
                int pe = bs.readInt32(), pb = bs.readInt32();
                res->id = gsPart3(bs.readInt32(), base3, lim3);
                res->path = gsPart3(bs.readInt32(), base3, lim3);
                int pn = bs.readInt32(); res->ptxInfoBegin = pb; res->ptxInfoEnd = pe;

                if (pb && pe) {
                    auto ptx = std::make_unique<XmlPtxInfo>();
                    ptx->type = bs.readUInt16(); ptx->aflags = bs.readUInt16();
                    ptx->x = bs.readUInt16();    ptx->y = bs.readUInt16();
                    ptx->ax = bs.readUInt16();   ptx->ay = bs.readUInt16();
                    ptx->aw = bs.readUInt16();   ptx->ah = bs.readUInt16();
                    ptx->rows = bs.readUInt16(); ptx->cols = bs.readUInt16();
                    ptx->parent = gsPart3(bs.readInt32(), base3, lim3);
                    res->ptxInfo = ptx.release();
                }
                res->properties.resize(static_cast<size_t>(pn));
                for (int l = 0; l < pn; ++l) {
                    int k2 = bs.readInt32(); (void)bs.readInt32(); int v2 = bs.readInt32();
                    res->properties[static_cast<size_t>(l)].key = gsPart3(k2, base3, lim3);
                    res->properties[static_cast<size_t>(l)].value = gsPart3(v2, base3, lim3);
                }
            }
        }
        bs.setPosition(sv);
        cl.push_back(std::move(comp));
    }
    return cl;
}


class RsbDefinition {
public:
    [[nodiscard]] static std::vector<uint8_t> xmlToDat(std::string_view xmlPath, UnifiedBinaryStream::Endian endian) {
        auto docRes = xml::Document::load_file(xmlPath);
        if (!docRes) throw std::runtime_error("Cannot parse XML");

        xml::Document doc = std::move(*docRes);
        auto root = doc.child("ResourceManifest");
        if (!root) throw std::runtime_error("No ResourceManifest in XML");

        UnifiedBinaryStream part1(UnifiedBinaryStream::Mode::Write);
        UnifiedBinaryStream part2(UnifiedBinaryStream::Mode::Write);
        UnifiedBinaryStream part3(UnifiedBinaryStream::Mode::Write);
        part1.setEndian(endian); part2.setEndian(endian); part3.setEndian(endian);

        std::unordered_map<std::string, int> strPool;
        part3.writeUInt8(0); strPool[""] = 0;

        auto throwInPool = [&](std::string_view key) -> int {
            auto svKey = std::string(key);
            auto it = strPool.find(svKey);
            if (it != strPool.end()) return it->second;
            int off = static_cast<int>(part3.getPosition());
            strPool[svKey] = off;
            for (char c : key) part3.writeUInt8(static_cast<uint8_t>(c));
            part3.writeUInt8(0);
            return off;
        };

        static const std::vector<std::string> knownAttrs = {
            "type","id","path","imagetype","aflags","x","y","ax","ay",
            "aw","ah","rows","cols","parent"
        };

        for (auto compNode : root.children("CompositeResources")) {
            std::string compId(compNode.attribute("id").as_string(""));
            int idOff = throwInPool(compId);
            std::vector<xml::Node> groups;
            for (auto g : compNode.children("Group")) groups.push_back(g);

            part1.writeInt32(idOff);
            part1.writeInt32(static_cast<int32_t>(groups.size()));
            part1.writeInt32(0x10);

            for (auto& gNode : groups) {
                int res = static_cast<int>(gNode.attribute("res").as_int(0));
                std::string loc(gNode.attribute("loc").as_string(""));
                std::string gid(gNode.attribute("id").as_string(""));
                int gidOff = throwInPool(gid);

                std::vector<xml::Node> resNodes;
                for (auto rn : gNode.children("Res")) resNodes.push_back(rn);

                part1.writeInt32(res);
                uint8_t lb[4]={0};
                for (int k = 0; k < 4 && k < static_cast<int>(loc.size()); ++k)
                    lb[k] = static_cast<uint8_t>(loc[k]);
                part1.writeUInt8(lb[3]); part1.writeUInt8(lb[2]);
                part1.writeUInt8(lb[1]); part1.writeUInt8(lb[0]);
                part1.writeInt32(gidOff);
                part1.writeInt32(static_cast<int32_t>(resNodes.size()));

                for (auto& rNode : resNodes) {
                    int resOff = static_cast<int>(part2.getPosition());
                    part1.writeInt32(resOff);
                    uint16_t rtype = static_cast<uint16_t>(rNode.attribute("type").as_int(0));
                    std::string rid(rNode.attribute("id").as_string(""));
                    std::string rpath(rNode.attribute("path").as_string(""));
                    int ridOff = throwInPool(rid), rpathOff = throwInPool(rpath);

                    std::vector<std::pair<std::string, std::string>> extraProps;
                    for (auto attr : rNode.attributes()) {
                        std::string an(attr.name());
                        bool known = false;
                        for (auto& k : knownAttrs) if (k == an) { known = true; break; }
                        if (!known) extraProps.emplace_back(std::move(an), std::string(attr.as_string("")));
                    }

                    part2.writeInt32(0); part2.writeUInt16(rtype); part2.writeUInt16(0x1C);
                    size_t ptxEndPos = part2.getPosition();
                    part2.writeInt32(0); part2.writeInt32(0);
                    part2.writeInt32(ridOff); part2.writeInt32(rpathOff);
                    part2.writeInt32(static_cast<int32_t>(extraProps.size()));

                    if (rtype == 0) {
                        int ptxBegin = static_cast<int>(part2.getPosition());
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("imagetype").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("aflags").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("x").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("y").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("ax").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("ay").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("aw").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("ah").as_int(0)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("rows").as_int(1)));
                        part2.writeUInt16(static_cast<uint16_t>(rNode.attribute("cols").as_int(1)));
                        part2.writeInt32(throwInPool(std::string(rNode.attribute("parent").as_string(""))));

                        int ptxEnd = static_cast<int>(part2.getPosition());
                        size_t sp = part2.getPosition();
                        part2.setPosition(ptxEndPos);
                        part2.writeInt32(ptxEnd); part2.writeInt32(ptxBegin);
                        part2.setPosition(sp);
                    }

                    for (auto& [k, v] : extraProps) {
                        part2.writeInt32(throwInPool(k));
                        part2.writeInt32(0);
                        part2.writeInt32(throwInPool(v));
                    }
                }
            }
        }

        UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write);
        out.setEndian(endian);
        out.writeInt32(XMLDAT_MAGIC); out.writeInt32(1); out.writeInt32(0x14);
        out.writeInt32(static_cast<int32_t>(0x14 + part1.getPosition()));
        out.writeInt32(static_cast<int32_t>(0x14 + part1.getPosition() + part2.getPosition()));
        out.writeBytes(part1.getData()); out.writeBytes(part2.getData()); out.writeBytes(part3.getData());
        return out.getData();
    }

    [[nodiscard]] static std::vector<uint8_t> jsonToDat(json::Value rootArr, UnifiedBinaryStream::Endian endian) {
        UnifiedBinaryStream part1(UnifiedBinaryStream::Mode::Write);
        UnifiedBinaryStream part2(UnifiedBinaryStream::Mode::Write);
        UnifiedBinaryStream part3(UnifiedBinaryStream::Mode::Write);
        part1.setEndian(endian); part2.setEndian(endian); part3.setEndian(endian);

        std::unordered_map<std::string, int> strPool;
        part3.writeUInt8(0); strPool[""] = 0;

        auto throwInPool = [&](std::string_view key) -> int {
            auto svKey = std::string(key);
            auto it = strPool.find(svKey);
            if (it != strPool.end()) return it->second;
            int off = static_cast<int>(part3.getPosition());
            strPool[svKey] = off;
            for (char c : key) part3.writeUInt8(static_cast<uint8_t>(c));
            part3.writeUInt8(0);
            return off;
        };

        for (auto compNode : rootArr.array()) {
            std::string compId(compNode.obj_get("id").get_str_view());
            int idOff = throwInPool(compId);
            auto groupsArr = compNode.obj_get("groups");

            part1.writeInt32(idOff);
            part1.writeInt32(static_cast<int32_t>(groupsArr.arr_size()));
            part1.writeInt32(0x10);

            for (auto gNode : groupsArr.array()) {
                int res = static_cast<int>(gNode.obj_get("res").get_sint());
                std::string loc(gNode.obj_get("loc").get_str_view());
                std::string gid(gNode.obj_get("id").get_str_view());
                int gidOff = throwInPool(gid);

                auto resArr = gNode.obj_get("resources");

                part1.writeInt32(res);
                uint8_t lb[4]={0};
                for (int k = 0; k < 4 && k < static_cast<int>(loc.size()); ++k)
                    lb[k] = static_cast<uint8_t>(loc[k]);
                part1.writeUInt8(lb[3]); part1.writeUInt8(lb[2]);
                part1.writeUInt8(lb[1]); part1.writeUInt8(lb[0]);
                part1.writeInt32(gidOff);
                part1.writeInt32(static_cast<int32_t>(resArr.arr_size()));

                for (auto rNode : resArr.array()) {
                    int resOff = static_cast<int>(part2.getPosition());
                    part1.writeInt32(resOff);
                    uint16_t rtype = static_cast<uint16_t>(rNode.obj_get("type").get_uint());
                    std::string rid(rNode.obj_get("id").get_str_view());
                    std::string rpath(rNode.obj_get("path").get_str_view());
                    int ridOff = throwInPool(rid), rpathOff = throwInPool(rpath);

                    std::vector<std::pair<std::string, std::string>> extraProps;
                    if (auto propsNode = rNode.obj_get("properties")) {
                        for (auto [k, v] : propsNode.object()) {
                            extraProps.emplace_back(std::string(k.get_str_view()), std::string(v.get_str_view()));
                        }
                    }

                    part2.writeInt32(0); part2.writeUInt16(rtype); part2.writeUInt16(0x1C);
                    size_t ptxEndPos = part2.getPosition();
                    part2.writeInt32(0); part2.writeInt32(0);
                    part2.writeInt32(ridOff); part2.writeInt32(rpathOff);
                    part2.writeInt32(static_cast<int32_t>(extraProps.size()));

                    if (rtype == 0) {
                        int ptxBegin = static_cast<int>(part2.getPosition());
                        auto getU16 = [&](json::Value p, const char* key, uint16_t def) -> uint16_t {
                            auto v = p.obj_get(key);
                            return v ? static_cast<uint16_t>(v.get_uint()) : def;
                        };

                        auto ptx = rNode.obj_get("ptx_info");
                        if (ptx) {
                            part2.writeUInt16(getU16(ptx, "imagetype", 0));
                            part2.writeUInt16(getU16(ptx, "aflags", 0));
                            part2.writeUInt16(getU16(ptx, "x", 0));
                            part2.writeUInt16(getU16(ptx, "y", 0));
                            part2.writeUInt16(getU16(ptx, "ax", 0));
                            part2.writeUInt16(getU16(ptx, "ay", 0));
                            part2.writeUInt16(getU16(ptx, "aw", 0));
                            part2.writeUInt16(getU16(ptx, "ah", 0));
                            part2.writeUInt16(getU16(ptx, "rows", 1));
                            part2.writeUInt16(getU16(ptx, "cols", 1));
                            auto parentNode = ptx.obj_get("parent");
                            std::string parentStr = parentNode ? std::string(parentNode.get_str_view()) : "";
                            part2.writeInt32(throwInPool(parentStr));
                        } else {
                            for (int i = 0; i < 8; ++i) part2.writeUInt16(0);
                            part2.writeUInt16(1); part2.writeUInt16(1);
                            part2.writeInt32(throwInPool(""));
                        }

                        int ptxEnd = static_cast<int>(part2.getPosition());
                        size_t sp = part2.getPosition();
                        part2.setPosition(ptxEndPos);
                        part2.writeInt32(ptxEnd); part2.writeInt32(ptxBegin);
                        part2.setPosition(sp);
                    }

                    for (auto& [k, v] : extraProps) {
                        part2.writeInt32(throwInPool(k));
                        part2.writeInt32(0);
                        part2.writeInt32(throwInPool(v));
                    }
                }
            }
        }

        UnifiedBinaryStream out(UnifiedBinaryStream::Mode::Write);
        out.setEndian(endian);
        out.writeInt32(XMLDAT_MAGIC); out.writeInt32(1); out.writeInt32(0x14);
        out.writeInt32(static_cast<int32_t>(0x14 + part1.getPosition()));
        out.writeInt32(static_cast<int32_t>(0x14 + part1.getPosition() + part2.getPosition()));
        out.writeBytes(part1.getData()); out.writeBytes(part2.getData()); out.writeBytes(part3.getData());
        return out.getData();
    }

    [[nodiscard]] static json::MutValue datToJson(std::span<const uint8_t> dat, json::MutDocument& jdoc) {
        auto cl = parseDatToStructs({dat.begin(), dat.end()});

        auto outArr = jdoc.mut_arr();
        for (auto& comp : cl) {
            auto compObj = jdoc.mut_obj();
            compObj.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(comp->id));
            auto grpArr = jdoc.mut_arr();

            for (auto* rsgp : comp->rsgps) {
                auto grpObj = jdoc.mut_obj();
                grpObj.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(rsgp->id));
                jdoc.obj_add_int(grpObj, "res", rsgp->resolutionRatio);
                grpObj.obj_add(jdoc.mut_str("loc"), jdoc.mut_strdup(rsgp->language));

                auto resArr = jdoc.mut_arr();
                for (auto* res : rsgp->resources) {
                    auto resObj = jdoc.mut_obj();
                    resObj.obj_add(jdoc.mut_str("type"), jdoc.mut_uint(res->type));
                    resObj.obj_add(jdoc.mut_str("id"), jdoc.mut_strdup(res->id));
                    resObj.obj_add(jdoc.mut_str("path"), jdoc.mut_strdup(res->path));

                    if (res->ptxInfo && res->ptxInfoBegin && res->ptxInfoEnd) {
                        auto ptxObj = jdoc.mut_obj();
                        auto* p = res->ptxInfo;
                        ptxObj.obj_add(jdoc.mut_str("imagetype"), jdoc.mut_uint(p->type));
                        ptxObj.obj_add(jdoc.mut_str("aflags"), jdoc.mut_uint(p->aflags));
                        if (p->x) ptxObj.obj_add(jdoc.mut_str("x"), jdoc.mut_uint(p->x));
                        if (p->y) ptxObj.obj_add(jdoc.mut_str("y"), jdoc.mut_uint(p->y));
                        if (p->ax) ptxObj.obj_add(jdoc.mut_str("ax"), jdoc.mut_uint(p->ax));
                        if (p->ay) ptxObj.obj_add(jdoc.mut_str("ay"), jdoc.mut_uint(p->ay));
                        if (p->aw) ptxObj.obj_add(jdoc.mut_str("aw"), jdoc.mut_uint(p->aw));
                        if (p->ah) ptxObj.obj_add(jdoc.mut_str("ah"), jdoc.mut_uint(p->ah));
                        if (p->rows != 1) ptxObj.obj_add(jdoc.mut_str("rows"), jdoc.mut_uint(p->rows));
                        if (p->cols != 1) ptxObj.obj_add(jdoc.mut_str("cols"), jdoc.mut_uint(p->cols));
                        ptxObj.obj_add(jdoc.mut_str("parent"), jdoc.mut_strdup(p->parent));
                        resObj.obj_add(jdoc.mut_str("ptx_info"), ptxObj);
                    }
                    if (!res->properties.empty()) {
                        auto propsObj = jdoc.mut_obj();
                        for (auto& pr : res->properties) {
                            propsObj.obj_add(jdoc.mut_strdup(pr.key), jdoc.mut_strdup(pr.value));
                        }
                        resObj.obj_add(jdoc.mut_str("properties"), propsObj);
                    }
                    resArr.arr_append(resObj);
                }
                grpObj.obj_add(jdoc.mut_str("resources"), resArr);
                grpArr.arr_append(grpObj);
            }
            compObj.obj_add(jdoc.mut_str("groups"), grpArr);
            outArr.arr_append(compObj);
        }
        return outArr;
    }

    static void datToXml(std::span<const uint8_t> dat, std::string_view outFile) {
        auto cl = parseDatToStructs({dat.begin(), dat.end()});

        std::string out;
        out.reserve(65536);
        out += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
        out += "<!-- DO NOT EDIT THIS FILE. This file is generated by PopStudio. "
               "(unless you want to change the cutting method of some pictures) -->\n";
        out += "<ResourceManifest version=\"3\">\n";

        for (auto& comp : cl) {
            out += "\n<CompositeResources id=\"";
            out += comp->id;
            out += "\">\n";
            for (auto* rsgp : comp->rsgps) {
                out += "  <Group id=\"";
                out += rsgp->id;
                out += "\" res=\"";
                out += i32str(rsgp->resolutionRatio);
                out += "\" loc=\"";
                out += rsgp->language;
                out += "\">\n";
                for (auto* res : rsgp->resources) {
                    out += "    <Res type=\"";
                    out += u32str(res->type);
                    out += "\" id=\"";
                    out += res->id;
                    out += "\" path=\"";
                    out += res->path;
                    out += "\" ";
                    if (res->ptxInfo && res->ptxInfoBegin && res->ptxInfoEnd) {
                        auto* p = res->ptxInfo;
                        out += "imagetype=\"";
                        out += u32str(p->type);
                        out += "\" ";
                        out += "aflags=\"";
                        out += u32str(p->aflags);
                        out += "\" ";
                        if (p->x)   { out += "x=\"";    out += u32str(p->x);    out += "\" "; }
                        if (p->y)   { out += "y=\"";    out += u32str(p->y);    out += "\" "; }
                        if (p->ax)  { out += "ax=\"";   out += u32str(p->ax);   out += "\" "; }
                        if (p->ay)  { out += "ay=\"";   out += u32str(p->ay);   out += "\" "; }
                        if (p->aw)  { out += "aw=\"";   out += u32str(p->aw);   out += "\" "; }
                        if (p->ah)  { out += "ah=\"";   out += u32str(p->ah);   out += "\" "; }
                        if (p->rows != 1) { out += "rows=\""; out += u32str(p->rows); out += "\" "; }
                        if (p->cols != 1) { out += "cols=\""; out += u32str(p->cols); out += "\" "; }
                        out += "parent=\"";
                        out += p->parent;
                        out += "\" ";
                    }
                    for (auto& pr : res->properties) {
                        out += pr.key;
                        out += "=\"";
                        out += pr.value;
                        out += "\" ";
                    }
                    out += "/>\n";
                }
                out += "  </Group>\n";
            }
            out += "</CompositeResources>\n";
        }
        out += "\n</ResourceManifest>";

        FileUtils::createDirectory(FileUtils::getParentDirectory(std::string(outFile)));
        FileUtils::posixWrite(std::string(outFile), out.data(), out.size());
    }
};

}