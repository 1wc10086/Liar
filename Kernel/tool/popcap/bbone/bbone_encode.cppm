module;
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <map>
#include <memory>
#include <set>
#include <string>
#include <string_view>
#include <utility>
#include <vector>
export module tool.popcap.bbone.encode;
import utility.binary.unified_binary_stream;
import utility.io;
import utility.json;
import utility.png.png;
import utility.xml.xml;
import utility.zlib.zlib_compress;
import tool.popcap.bbone.core;
import tool.popcap.bbone.definition;
import tool.popcap.bbone.utils;

export namespace BBone {

class Encoder {
    using Stream = Detail::Stream;

    static void encodeChild(Stream& bs, json::Value node) {
        int16_t flags = 0;
        const auto matrix = node.obj_get("matrix");
        const bool hasXY = matrix && (Detail::getFloat(matrix, "tx") != 0.0f || Detail::getFloat(matrix, "ty") != 0.0f);
        const bool hasA = matrix && Detail::getFloat(matrix, "a", 1.0f) != 1.0f;
        const bool hasB = matrix && Detail::getFloat(matrix, "b") != 0.0f;
        const bool hasC = matrix && Detail::getFloat(matrix, "c") != 0.0f;
        const bool hasD = matrix && Detail::getFloat(matrix, "d", 1.0f) != 1.0f;
        const auto color = node.obj_get("color");
        const bool hasColorTransform = color && static_cast<bool>(color.obj_get("redMultiplier"));
        const bool hasAlpha = color && !hasColorTransform && Detail::getFloat(color, "alphaMultiplier", 1.0f) != 1.0f;
        const bool hasBlend = static_cast<bool>(node.obj_get("blendMode"));
        const auto children = node.obj_get("children");
        const bool hasChildren = children && children.is_arr() && children.arr_size() > 0;
        const bool hasBatch = static_cast<bool>(node.obj_get("references_shared_animation"));

        if (hasXY) flags |= 1;
        if (hasA) flags |= 2;
        if (hasB) flags |= 4;
        if (hasC) flags |= 8;
        if (hasD) flags |= 16;
        if (hasAlpha) flags |= 32;
        if (hasColorTransform) flags |= 64;
        if (hasBlend) flags |= 128;
        if (hasChildren) flags |= 256;
        if (hasBatch) flags |= 512;

        bs.writeInt16(flags);
        Detail::writeUtf(bs, node.obj_get("name") ? node.obj_get("name").get_str_view() : std::string_view{});
        if (hasXY) { bs.writeFloat32(Detail::getFloat(matrix, "tx")); bs.writeFloat32(Detail::getFloat(matrix, "ty")); }
        if (hasA) bs.writeFloat32(Detail::getFloat(matrix, "a", 1.0f));
        if (hasB) bs.writeFloat32(Detail::getFloat(matrix, "b"));
        if (hasC) bs.writeFloat32(Detail::getFloat(matrix, "c"));
        if (hasD) bs.writeFloat32(Detail::getFloat(matrix, "d", 1.0f));
        if (hasAlpha) bs.writeFloat32(Detail::getFloat(color, "alphaMultiplier", 1.0f));
        if (hasColorTransform) {
            bs.writeFloat32(Detail::getFloat(color, "redMultiplier", 1.0f));
            bs.writeFloat32(Detail::getFloat(color, "greenMultiplier", 1.0f));
            bs.writeFloat32(Detail::getFloat(color, "blueMultiplier", 1.0f));
            bs.writeFloat32(Detail::getFloat(color, "alphaMultiplier", 1.0f));
            bs.writeFloat32(Detail::getFloat(color, "redOffset"));
            bs.writeFloat32(Detail::getFloat(color, "greenOffset"));
            bs.writeFloat32(Detail::getFloat(color, "blueOffset"));
            bs.writeFloat32(Detail::getFloat(color, "alphaOffset"));
        }
        if (hasBlend) Detail::writeUtf(bs, node.obj_get("blendMode").get_str_view());
        if (hasChildren) {
            bs.writeInt16(static_cast<int16_t>(children.arr_size()));
            for (auto child : children.array()) encodeChild(bs, child);
        }
    }

    static void encodeFrame(Stream& bs, json::Value frame) {
        auto children = frame.obj_get("children");
        bs.writeInt32(children && children.is_arr() ? static_cast<int32_t>(children.arr_size()) : 0);
        if (children && children.is_arr()) for (auto child : children.array()) encodeChild(bs, child);
    }

    [[nodiscard]] static Stream buildAtlasPlugin(json::Value doc, const std::string& inputDir) {
        Stream p(Stream::Mode::Write, Stream::Endian::Big);
        auto plist = doc.obj_get("plist");
        if (!plist || !plist.is_arr()) {
            p.writeUInt8(0xFF);
            p.writeUInt16(0);
            p.writeUInt16(0);
            return p;
        }

        int maxBitmapId = -1;
        for (auto entry : plist.array()) maxBitmapId = std::max(maxBitmapId, Detail::getInt(entry, "bitmap_id"));
        const int bitmapCount = std::max(maxBitmapId + 1, 1);

        struct AtlasInfo { int maxW{}; int maxH{}; std::map<std::string, Rect> rects; };
        std::vector<AtlasInfo> atlases(static_cast<size_t>(bitmapCount));
        std::map<std::string, std::unique_ptr<ImageBitmap>, std::less<>> images;

        for (auto entry : plist.array()) {
            const int id = std::clamp(Detail::getInt(entry, "bitmap_id"), 0, bitmapCount - 1);
            const std::string name = entry.obj_get("name") ? std::string(entry.obj_get("name").get_str()) : std::string{};
            const Rect r{Detail::getInt(entry, "rect_x"), Detail::getInt(entry, "rect_y"), Detail::getInt(entry, "rect_w"), Detail::getInt(entry, "rect_h")};
            atlases[id].rects[name] = r;
            atlases[id].maxW = std::max(atlases[id].maxW, r.x + r.w);
            atlases[id].maxH = std::max(atlases[id].maxH, r.y + r.h);
            if (!images.contains(name)) {
                try {
                    images[name].reset(ImageBitmap::create(FileUtils::joinPath(inputDir, name + ".png")));
                } catch (...) {
                    auto fallback = std::unique_ptr<ImageBitmap>(ImageBitmap::create(std::max(r.w, 1), std::max(r.h, 1)));
                    std::fill_n(fallback->getPixels(), fallback->getSize(), ImageColor(0, 0, 0, 0));
                    images[name] = std::move(fallback);
                }
            }
        }

        p.writeUInt8(0xFF);
        p.writeUInt16(static_cast<uint16_t>(bitmapCount));
        for (int id = 0; id < bitmapCount; ++id) {
            const int w = Detail::nextPowerOfTwo(std::max(atlases[id].maxW, 1));
            const int h = Detail::nextPowerOfTwo(std::max(atlases[id].maxH, 1));
            auto atlas = std::unique_ptr<ImageBitmap>(ImageBitmap::create(w, h));
            auto* dst = atlas->getPixels();
            std::fill_n(dst, atlas->getSize(), ImageColor(0, 0, 0, 0));

            for (const auto& [name, r] : atlases[id].rects) {
                auto it = images.find(name);
                if (it == images.end()) continue;
                const auto& img = *it->second;
                const auto* src = img.getPixels();
                const int sw = img.getWidth();
                const int sh = img.getHeight();
                for (int cy = 0; cy < r.h && cy < sh; ++cy) {
                    const int dy = r.y + cy;
                    if (dy < 0 || dy >= h) continue;
                    for (int cx = 0; cx < r.w && cx < sw; ++cx) {
                        const int dx = r.x + cx;
                        if (dx >= 0 && dx < w) dst[static_cast<size_t>(dy) * w + dx] = src[static_cast<size_t>(cy) * sw + cx];
                    }
                }
            }

            p.writeUInt16(static_cast<uint16_t>(w));
            p.writeUInt16(static_cast<uint16_t>(h));
            for (int i = 0; i < atlas->getSize(); ++i) {
                auto c = dst[i];
                if (i == 0 && c.a == 0xFF && c.r == 0xD7) c.r = 0xD6;
                p.writeUInt8(c.a);
                p.writeUInt8(c.r);
                p.writeUInt8(c.g);
                p.writeUInt8(c.b);
            }
        }

        std::map<std::string, std::vector<json::Value>, std::less<>> grouped;
        std::vector<std::string> order;
        for (auto entry : plist.array()) {
            const std::string name = entry.obj_get("name") ? std::string(entry.obj_get("name").get_str()) : std::string{};
            if (!grouped.contains(name)) order.push_back(name);
            grouped[name].push_back(entry);
        }

        p.writeUInt16(static_cast<uint16_t>(order.size()));
        for (const auto& name : order) {
            Detail::writeUtf(p, name);
            const auto& entries = grouped[name];
            p.writeUInt32(static_cast<uint32_t>(entries.size()));
            for (auto entry : entries) {
                p.writeUInt8(0xFF);
                p.writeUInt16(static_cast<uint16_t>(Detail::getInt(entry, "bitmap_id")));
                p.writeInt16(static_cast<int16_t>(Detail::getInt(entry, "rect_x")));
                p.writeInt16(static_cast<int16_t>(Detail::getInt(entry, "rect_y")));
                p.writeUInt16(static_cast<uint16_t>(Detail::getInt(entry, "rect_w")));
                p.writeUInt16(static_cast<uint16_t>(Detail::getInt(entry, "rect_h")));
                p.writeFloat32(Detail::getFloat(entry, "origin_x"));
                p.writeFloat32(Detail::getFloat(entry, "origin_y"));
                p.writeFloat32(Detail::getFloat(entry, "scale_x", 1.0f));
                p.writeFloat32(Detail::getFloat(entry, "scale_y", 1.0f));
                p.writeFloat32(Detail::getFloat(entry, "rotation"));
            }
        }
        return p;
    }

    [[nodiscard]] static Stream buildLabelsPlugin(json::Value doc) {
        Stream p(Stream::Mode::Write, Stream::Endian::Big);
        auto labels = doc.obj_get("labels");
        if (!labels || !labels.is_obj()) { p.writeUInt32(0); return p; }

        p.writeUInt32(static_cast<uint32_t>(labels.obj_size()));
        for (auto [k, v] : labels.object()) {
            auto s = k.get_str_view();
            p.writeUInt16(static_cast<uint16_t>(s.size()));
            p.writeBytes(reinterpret_cast<const uint8_t*>(s.data()), s.size());
            p.writeUInt32(v.is_uint() ? static_cast<uint32_t>(v.get_uint()) : v.is_int() ? static_cast<uint32_t>(v.get_sint()) : 0u);
        }
        return p;
    }

    [[nodiscard]] static Stream buildAnimationPlugin(json::Value doc) {
        Stream p(Stream::Mode::Write, Stream::Endian::Big);
        auto anim = doc.obj_get("animation");
        if (!anim) { p.writeInt16(0); p.writeInt32(0); return p; }

        auto shared = anim.obj_get("shared_animations");
        if (shared && shared.is_obj()) {
            p.writeInt16(static_cast<int16_t>(shared.obj_size()));
            for (auto [k, v] : shared.object()) {
                Detail::writeUtf(p, k.get_str_view());
                p.writeInt16(v && v.is_arr() ? static_cast<int16_t>(v.arr_size()) : 0);
                if (v && v.is_arr()) for (auto frame : v.array()) encodeFrame(p, frame);
            }
        } else {
            p.writeInt16(0);
        }

        auto frames = anim.obj_get("frames");
        p.writeInt32(frames && frames.is_arr() ? static_cast<int32_t>(frames.arr_size()) : 0);
        if (frames && frames.is_arr()) for (auto frame : frames.array()) encodeFrame(p, frame);
        return p;
    }

public:
    [[nodiscard]] static bool encode(const std::string& inputDir, const std::string& outputFile) {
        try {
            auto text = FileUtils::readTextFile(FileUtils::joinPath(inputDir, "animation.json"));
            if (text.empty()) return false;

            auto parsed = json::Document::parse(text, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
            if (!parsed) return false;
            auto root = parsed.root();

            auto p1 = buildAtlasPlugin(root, inputDir);
            auto p3 = buildLabelsPlugin(root);
            auto p2 = buildAnimationPlugin(root);

            std::vector<std::pair<uint8_t, const std::vector<uint8_t>*>> plugins;
            if (p1.getLength()) plugins.emplace_back(1, &p1.getData());
            if (p3.getLength()) plugins.emplace_back(3, &p3.getData());
            if (p2.getLength()) plugins.emplace_back(2, &p2.getData());

            Stream payload(Stream::Mode::Write, Stream::Endian::Big);
            uint32_t offset = 0;
            for (const auto& [id, data] : plugins) {
                payload.writeUInt8(id);
                payload.writeUInt32(offset);
                payload.writeUInt32(static_cast<uint32_t>(data->size()));
                offset += static_cast<uint32_t>(data->size());
            }
            payload.writeUInt8(0);
            for (const auto& plugin : plugins) payload.writeBytes(*plugin.second);

            auto compressedOpt = zlib_ns::Compressor::compress(payload.getData());
            if (!compressedOpt) return false;
            const auto& compressed = *compressedOpt;

            Stream out(Stream::Mode::Write, Stream::Endian::Big);
            out.writeUInt16(MAGIC);
            out.writeUInt16(32);
            out.writeUInt32(static_cast<uint32_t>(compressed.size()));
            out.writeUInt32(static_cast<uint32_t>(payload.getLength()));
            out.writeUInt32(0x01000000);
            for (int i = 0; i < 16; ++i) out.writeUInt8(0);
            out.writeBytes(compressed);

            return FileUtils::writeFileBytes(outputFile, out.getData());
        } catch (...) {
            return false;
        }
    }
};

namespace XFL {

class Encoder {
    static void writeXml(const std::string& path, xml::Document& doc) {
        FileUtils::createDirectory(FileUtils::getParentDirectory(path));
        doc.save_file(path);
    }

    static void compose(json::Value bone, const Matrix& parent, float parentAlpha, int frameIndex, json::Value shared, std::vector<FrameEntry>& out) {
        const std::string name = bone.obj_get("name") ? std::string(bone.obj_get("name").get_str()) : std::string{};
        Matrix m;
        if (auto mat = bone.obj_get("matrix")) {
            m.a = Detail::getFloat(mat, "a", 1.0f);
            m.b = Detail::getFloat(mat, "b");
            m.c = Detail::getFloat(mat, "c");
            m.d = Detail::getFloat(mat, "d", 1.0f);
            m.tx = Detail::getFloat(mat, "tx");
            m.ty = Detail::getFloat(mat, "ty");
        }
        const Matrix world = parent.mul(m);
        float alpha = parentAlpha;
        if (auto color = bone.obj_get("color")) alpha *= Detail::getFloat(color, "alphaMultiplier", 1.0f);
        out.push_back({name, world, alpha});

        auto children = bone.obj_get("children");
        if (auto ref = bone.obj_get("references_shared_animation")) {
            auto frames = shared ? shared.obj_get(ref.get_str_view()) : json::Value{};
            if (frames && frames.is_arr() && frames.arr_size()) {
                auto frame = frames.arr_get(static_cast<size_t>(frameIndex) % frames.arr_size());
                if (frame) children = frame.obj_get("children");
            }
        }
        if (children && children.is_arr()) for (auto child : children.array()) compose(child, world, alpha, frameIndex, shared, out);
    }

    static void drawOrder(json::Value node, int frameIndex, json::Value shared, std::vector<std::string>& order) {
        std::string name = node.obj_get("name") ? Detail::sanitize(std::string(node.obj_get("name").get_str())) : std::string{};
        if (!name.empty()) order.push_back(std::move(name));

        auto children = node.obj_get("children");
        if (auto ref = node.obj_get("references_shared_animation")) {
            auto frames = shared ? shared.obj_get(ref.get_str_view()) : json::Value{};
            if (frames && frames.is_arr() && frames.arr_size()) {
                auto frame = frames.arr_get(static_cast<size_t>(frameIndex) % frames.arr_size());
                if (frame) children = frame.obj_get("children");
            }
        }
        if (children && children.is_arr()) for (auto child : children.array()) drawOrder(child, frameIndex, shared, order);
    }

public:
    [[nodiscard]] static bool encode(const std::string& inputPath, const std::string& outputDir) {
        try {
            const std::string jsonPath = FileUtils::isRegularFile(inputPath) ? inputPath : FileUtils::joinPath(inputPath, "animation.json");
            const std::string inputDir = FileUtils::getParentDirectory(jsonPath);
            auto text = FileUtils::readTextFile(jsonPath);
            if (text.empty()) return false;

            auto parsed = json::Document::parse(text, json::ReadFlag::AllowComments | json::ReadFlag::AllowTrailingCommas);
            if (!parsed) return false;
            auto doc = parsed.root();

            constexpr double dpik = 0.78125;
            constexpr int fps = 30;
            const double stageW = (doc.obj_get("width") ? doc.obj_get("width").get_num() : 390.0) * dpik;
            const double stageH = (doc.obj_get("height") ? doc.obj_get("height").get_num() : 390.0) * dpik;

            const auto libDir = FileUtils::joinPath(outputDir, "library");
            const auto mediaDir = FileUtils::joinPath(libDir, "media");
            const auto imageDir = FileUtils::joinPath(libDir, "image");
            const auto spriteDir = FileUtils::joinPath(libDir, "sprite");
            FileUtils::createDirectory(mediaDir);
            FileUtils::createDirectory(imageDir);
            FileUtils::createDirectory(spriteDir);

            auto pngFiles = FileUtils::collectFilesByExtension(inputDir, {".png"});
            std::map<std::string, std::string, std::less<>> pngIndex;
            for (const auto& p : pngFiles) {
                const auto name = FileUtils::getFileName(p);
                const auto stem = FileUtils::getFileNameWithoutExtension(p);
                FileUtils::copyFile(p, FileUtils::joinPath(mediaDir, name));
                pngIndex[stem] = name;
                pngIndex[Detail::lowerAscii(stem)] = name;
            }

            xml::Document dom;
            auto root = dom.append_child("DOMDocument");
            root.append_attribute("xmlns:xsi").set_value("http://www.w3.org/2001/XMLSchema-instance");
            root.append_attribute("xmlns").set_value("http://ns.adobe.com/xfl/2008/");
            root.append_attribute("frameRate").set_value(static_cast<int64_t>(fps));
            root.append_attribute("width").set_value(stageW);
            root.append_attribute("height").set_value(stageH);
            root.append_attribute("xflVersion").set_value("2.971");

            auto folders = root.append_child("folders");
            for (auto name : {"media", "image", "sprite"}) {
                auto f = folders.append_child("DOMFolderItem");
                f.append_attribute("name").set_value(name);
                f.append_attribute("isExpanded").set_value("true");
            }

            auto media = root.append_child("media");
            auto symbols = root.append_child("symbols");
            auto layers = root.append_child("timelines").append_child("DOMTimeline").append_child("layers");

            int bitId = 1;
            for (const auto& p : pngFiles) {
                const auto file = FileUtils::getFileName(p);
                const auto stem = FileUtils::getFileNameWithoutExtension(p);
                auto item = media.append_child("DOMBitmapItem");
                item.append_attribute("name").set_value("media/" + stem);
                item.append_attribute("href").set_value("media/" + file);
                item.append_attribute("itemID").set_value("bit_" + std::to_string(bitId++));
                item.append_attribute("allowSmoothing").set_value("true");
                item.append_attribute("useImportedJPEGData").set_value("false");
            }

            auto plist = doc.obj_get("plist");
            auto anim = doc.obj_get("animation");
            auto frames = anim ? anim.obj_get("frames") : json::Value{};
            auto shared = anim ? anim.obj_get("shared_animations") : json::Value{};

            {
                json::MutDocument shadowDoc;
                auto shadow = shadowDoc.mut_obj();
                shadowDoc.set_root(shadow);
                if (plist) shadow.obj_add(shadowDoc.mut_str("plist"), shadowDoc.mut_copy(plist));
                else shadow.obj_add(shadowDoc.mut_str("plist"), shadowDoc.mut_arr());
                root.append_child("Description").set_text(shadowDoc.write());
            }

            auto labelsLayer = layers.append_child("DOMLayer");
            labelsLayer.append_attribute("name").set_value("Labels");
            labelsLayer.append_attribute("color").set_value("#FF0000");
            auto labelFrames = labelsLayer.append_child("frames");
            if (auto labels = doc.obj_get("labels"); labels && labels.is_obj()) {
                for (auto [k, v] : labels.object()) {
                    auto frame = labelFrames.append_child("DOMFrame");
                    frame.append_attribute("index").set_value(v.is_num() ? v.get_sint() : 0);
                    frame.append_attribute("duration").set_value("1");
                    frame.append_attribute("name").set_value(k.get_str());
                    frame.append_child("elements");
                }
            }

            std::set<std::string> names;
            if (frames && frames.is_arr()) {
                for (auto frame : frames.array()) {
                    if (auto children = frame.obj_get("children")) {
                        for (auto child : children.array()) {
                            std::vector<std::string> order;
                            drawOrder(child, 0, shared, order);
                            names.insert(order.begin(), order.end());
                        }
                    }
                }
            }
            if (plist && plist.is_arr()) for (auto p : plist.array()) if (auto n = p.obj_get("name")) names.insert(n.get_str());

            std::map<std::string, std::string, std::less<>> nameToImage;
            for (const auto& name : names) {
                std::string stem = name;
                if (auto it = pngIndex.find(name); it != pngIndex.end()) stem = FileUtils::getFileNameWithoutExtension(it->second);
                else if (auto low = pngIndex.find(Detail::lowerAscii(name)); low != pngIndex.end()) stem = FileUtils::getFileNameWithoutExtension(low->second);

                float ox = 0.0f, oy = 0.0f, sx = 1.0f, sy = 1.0f;
                if (plist && plist.is_arr()) {
                    for (auto p : plist.array()) {
                        if (auto n = p.obj_get("name"); n && n.get_str_view() == name) {
                            ox = Detail::getFloat(p, "origin_x");
                            oy = Detail::getFloat(p, "origin_y");
                            sx = Detail::getFloat(p, "scale_x", 1.0f);
                            sy = Detail::getFloat(p, "scale_y", 1.0f);
                            break;
                        }
                    }
                }

                xml::Document imageDoc;
                auto imageRoot = imageDoc.append_child("DOMSymbolItem");
                imageRoot.append_attribute("xmlns:xsi").set_value("http://www.w3.org/2001/XMLSchema-instance");
                imageRoot.append_attribute("xmlns").set_value("http://ns.adobe.com/xfl/2008/");
                imageRoot.append_attribute("name").set_value("image/" + stem);
                imageRoot.append_attribute("symbolType").set_value("graphic");
                auto bitmap = imageRoot.append_child("timeline").append_child("DOMTimeline").append_child("layers").append_child("DOMLayer").append_child("frames").append_child("DOMFrame").append_child("elements").append_child("DOMBitmapInstance");
                bitmap.append_attribute("libraryItemName").set_value("media/" + stem);
                auto mat = bitmap.append_child("matrix").append_child("Matrix");
                mat.append_attribute("a").set_value(static_cast<double>(sx));
                mat.append_attribute("d").set_value(static_cast<double>(sy));
                mat.append_attribute("tx").set_value(static_cast<double>(ox));
                mat.append_attribute("ty").set_value(static_cast<double>(oy));
                writeXml(FileUtils::joinPath(imageDir, stem + ".xml"), imageDoc);
                symbols.append_child("Include").append_attribute("href").set_value("image/" + stem + ".xml");
                nameToImage[name] = "image/" + stem;

                const auto spriteName = Detail::sanitize(name);
                xml::Document spriteDoc;
                auto spriteRoot = spriteDoc.append_child("DOMSymbolItem");
                spriteRoot.append_attribute("xmlns:xsi").set_value("http://www.w3.org/2001/XMLSchema-instance");
                spriteRoot.append_attribute("xmlns").set_value("http://ns.adobe.com/xfl/2008/");
                spriteRoot.append_attribute("name").set_value("sprite/" + spriteName);
                spriteRoot.append_attribute("symbolType").set_value("graphic");
                auto inst = spriteRoot.append_child("timeline").append_child("DOMTimeline").append_child("layers").append_child("DOMLayer").append_child("frames").append_child("DOMFrame").append_child("elements").append_child("DOMSymbolInstance");
                inst.append_attribute("libraryItemName").set_value(nameToImage[name]);
                inst.append_attribute("firstFrame").set_value("0");
                inst.append_attribute("symbolType").set_value("graphic");
                inst.append_attribute("loop").set_value("loop");
                writeXml(FileUtils::joinPath(spriteDir, spriteName + ".xml"), spriteDoc);
                symbols.append_child("Include").append_attribute("href").set_value("sprite/" + spriteName + ".xml");
            }

            const int frameCount = frames && frames.is_arr() ? static_cast<int>(frames.arr_size()) : 0;
            std::map<std::string, std::map<int, FrameEntry>, std::less<>> layerFrames;
            for (int i = 0; i < frameCount; ++i) {
                auto frame = frames.arr_get(i);
                auto children = frame.obj_get("children");
                if (!children || !children.is_arr()) continue;
                for (auto bone : children.array()) {
                    std::vector<FrameEntry> flat;
                    compose(bone, Matrix{}, 1.0f, i, shared, flat);
                    for (auto& entry : flat) {
                        auto key = Detail::sanitize(entry.name);
                        if (!nameToImage.contains(key) && !nameToImage.contains(entry.name)) continue;
                        layerFrames[key][i] = {"sprite/" + key, entry.matrix, entry.alpha};
                    }
                }
            }

            std::vector<std::string> bottomToTop;
            if (frameCount > 0) {
                if (auto children = frames.arr_get(0).obj_get("children"))
                    for (auto bone : children.array()) drawOrder(bone, 0, shared, bottomToTop);
            }

            std::vector<std::string> topFirst;
            std::set<std::string> seen;
            for (auto it = bottomToTop.rbegin(); it != bottomToTop.rend(); ++it) {
                auto key = Detail::sanitize(*it);
                if (layerFrames.contains(key) && seen.insert(key).second) topFirst.push_back(std::move(key));
            }
            for (const auto& [key, _] : layerFrames) if (seen.insert(key).second) topFirst.push_back(key);

            for (const auto& layerName : topFirst) {
                auto layer = layers.append_child("DOMLayer");
                layer.append_attribute("name").set_value(layerName);
                auto frameNode = layer.append_child("frames");
                const auto& fmap = layerFrames[layerName];
                for (int i = 0; i < frameCount; ++i) {
                    auto it = fmap.find(i);
                    if (it == fmap.end()) continue;
                    const auto& entry = it->second;
                    auto frame = frameNode.append_child("DOMFrame");
                    frame.append_attribute("index").set_value(static_cast<int64_t>(i));
                    frame.append_attribute("duration").set_value("1");
                    auto inst = frame.append_child("elements").append_child("DOMSymbolInstance");
                    inst.append_attribute("libraryItemName").set_value(entry.name);
                    inst.append_attribute("firstFrame").set_value("0");
                    inst.append_attribute("symbolType").set_value("graphic");
                    inst.append_attribute("loop").set_value("loop");
                    auto m = inst.append_child("matrix").append_child("Matrix");
                    m.append_attribute("a").set_value(static_cast<double>(entry.matrix.a * dpik));
                    m.append_attribute("b").set_value(static_cast<double>(entry.matrix.b * dpik));
                    m.append_attribute("c").set_value(static_cast<double>(entry.matrix.c * dpik));
                    m.append_attribute("d").set_value(static_cast<double>(entry.matrix.d * dpik));
                    m.append_attribute("tx").set_value(static_cast<double>(entry.matrix.tx * dpik));
                    m.append_attribute("ty").set_value(static_cast<double>(entry.matrix.ty * dpik));
                    if (std::abs(entry.alpha - 1.0f) > 0.001f) {
                        auto col = inst.append_child("color").append_child("Color");
                        col.append_attribute("redMultiplier").set_value("1.0");
                        col.append_attribute("greenMultiplier").set_value("1.0");
                        col.append_attribute("blueMultiplier").set_value("1.0");
                        col.append_attribute("alphaMultiplier").set_value(static_cast<double>(entry.alpha));
                    }
                }
            }

            writeXml(FileUtils::joinPath(outputDir, "DOMDocument.xml"), dom);

            xml::Document xfl;
            auto xroot = xfl.append_child("DOMFlashFile");
            xroot.append_attribute("version").set_value("2");
            xroot.append_attribute("xmlns:xsi").set_value("http://www.w3.org/2001/XMLSchema-instance");
            xroot.append_attribute("xmlns").set_value("http://ns.adobe.com/xfl/2008/");
            auto files = xroot.append_child("files");
            auto domFile = files.append_child("DOMFile");
            domFile.append_attribute("path").set_value("DOMDocument.xml");
            domFile.append_attribute("type").set_value("application/vnd.adobe.xfl.document");
            for (const auto& p : FileUtils::collectFiles(libDir)) {
                std::string rel = p.substr(outputDir.size());
                if (!rel.empty() && (rel.front() == '/' || rel.front() == '\\')) rel.erase(0, 1);
                std::ranges::replace(rel, '\\', '/');
                auto f = files.append_child("DOMFile");
                f.append_attribute("path").set_value(rel);
                if (FileUtils::getFileExtension(p) == ".png") f.append_attribute("type").set_value("image/png");
            }
            writeXml(FileUtils::joinPath(outputDir, "main.xfl"), xfl);
            return true;
        } catch (...) {
            return false;
        }
    }
};

}

}
