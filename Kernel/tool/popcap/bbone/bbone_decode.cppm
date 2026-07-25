module;
#include <algorithm>
#include <cstdint>
#include <filesystem>
#include <map>
#include <memory>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.bbone.decode;
import utility.binary.unified_binary_stream;
import utility.io;
import utility.json;
import utility.png.png;
import utility.xml.xml;
import utility.zlib.zlib_uncompress;
import tool.popcap.bbone.core;
import tool.popcap.bbone.utils;

namespace fs = std::filesystem;

export namespace BBone {

class Decoder {
    using Stream = Detail::Stream;

    static void addMatrix(json::MutDocument& doc, json::MutValue node, float a, float b, float c, float d, float tx, float ty) {
        auto matrix = doc.mut_obj();
        doc.obj_add_real(matrix, "a", a);
        doc.obj_add_real(matrix, "b", b);
        doc.obj_add_real(matrix, "c", c);
        doc.obj_add_real(matrix, "d", d);
        doc.obj_add_real(matrix, "tx", tx);
        doc.obj_add_real(matrix, "ty", ty);
        node.obj_add(doc.mut_str("matrix"), matrix);
    }

    static void parseChildNode(Stream& bs, json::MutDocument& doc, json::MutValue parent) {
        const int16_t flags = bs.readInt16();
        const std::string name = Detail::readUtf(bs);

        float a = 1.0f, b = 0.0f, c = 0.0f, d = 1.0f, tx = 0.0f, ty = 0.0f;
        if (flags & 1) { tx = bs.readFloat32(); ty = bs.readFloat32(); }
        if (flags & 2) a = bs.readFloat32();
        if (flags & 4) b = bs.readFloat32();
        if (flags & 8) c = bs.readFloat32();
        if (flags & 16) d = bs.readFloat32();

        auto node = doc.mut_obj();
        doc.obj_add_str(node, "name", name);
        addMatrix(doc, node, a, b, c, d, tx, ty);

        auto color = doc.mut_obj();
        if (flags & 64) {
            doc.obj_add_real(color, "redMultiplier", bs.readFloat32());
            doc.obj_add_real(color, "greenMultiplier", bs.readFloat32());
            doc.obj_add_real(color, "blueMultiplier", bs.readFloat32());
            doc.obj_add_real(color, "alphaMultiplier", bs.readFloat32());
            doc.obj_add_real(color, "redOffset", bs.readFloat32());
            doc.obj_add_real(color, "greenOffset", bs.readFloat32());
            doc.obj_add_real(color, "blueOffset", bs.readFloat32());
            doc.obj_add_real(color, "alphaOffset", bs.readFloat32());
        } else {
            doc.obj_add_real(color, "alphaMultiplier", (flags & 32) ? bs.readFloat32() : 1.0);
        }
        node.obj_add(doc.mut_str("color"), color);

        if (flags & 128) doc.obj_add_str(node, "blendMode", Detail::readUtf(bs));

        auto children = doc.mut_arr();
        if (flags & 256) {
            const int16_t n = bs.readInt16();
            for (int i = 0; i < n && !bs.hasErrorOccurred(); ++i) parseChildNode(bs, doc, children);
        }
        node.obj_add(doc.mut_str("children"), children);

        if (flags & 512) doc.obj_add_str(node, "references_shared_animation", name);
        parent.arr_append(node);
    }

    [[nodiscard]] static json::MutValue parseFrame(Stream& bs, json::MutDocument& doc) {
        auto frame = doc.mut_obj();
        auto children = doc.mut_arr();
        const int32_t n = bs.readInt32();
        for (int32_t i = 0; i < n && !bs.hasErrorOccurred(); ++i) parseChildNode(bs, doc, children);
        frame.obj_add(doc.mut_str("children"), children);
        return frame;
    }

    [[nodiscard]] static json::MutValue decodeAnimation(std::span<const uint8_t> data, json::MutDocument& doc) {
        Stream bs(data, Stream::Endian::Big);
        auto root = doc.mut_obj();
        auto shared = doc.mut_obj();

        const int16_t sharedCount = bs.readInt16();
        for (int16_t i = 0; i < sharedCount && !bs.hasErrorOccurred(); ++i) {
            auto name = Detail::readUtf(bs);
            const int16_t frameCount = bs.readInt16();
            auto frames = doc.mut_arr();
            for (int16_t j = 0; j < frameCount && !bs.hasErrorOccurred(); ++j) frames.arr_append(parseFrame(bs, doc));
            shared.obj_add(doc.mut_strdup(name), frames);
        }

        auto main = doc.mut_arr();
        const int32_t frameCount = bs.readInt32();
        for (int32_t i = 0; i < frameCount && !bs.hasErrorOccurred(); ++i) main.arr_append(parseFrame(bs, doc));

        root.obj_add(doc.mut_str("shared_animations"), shared);
        root.obj_add(doc.mut_str("frames"), main);
        return root;
    }

    [[nodiscard]] static json::MutValue parseLabels(std::span<const uint8_t> data, json::MutDocument& doc) {
        Stream bs(data, Stream::Endian::Big);
        auto labels = doc.mut_obj();
        const uint32_t n = bs.readUInt32();
        for (uint32_t i = 0; i < n && !bs.hasErrorOccurred(); ++i) {
            const uint16_t len = bs.readUInt16();
            auto bytes = bs.readSpan(len);
            auto name = Detail::latin1ToUtf8(bytes);
            labels.obj_add(doc.mut_strdup(name), doc.mut_uint(bs.readUInt32()));
        }
        return labels;
    }

    [[nodiscard]] static std::vector<std::unique_ptr<ImageBitmap>> parseAtlas(std::span<const uint8_t> data) {
        Stream bs(data, Stream::Endian::Big);
        if (bs.readUInt8() != 0xFF) throw std::runtime_error("invalid bbone atlas");

        const uint16_t bitmapCount = bs.readUInt16();
        std::vector<std::unique_ptr<ImageBitmap>> bitmaps;
        bitmaps.reserve(bitmapCount);

        for (uint16_t i = 0; i < bitmapCount && !bs.hasErrorOccurred(); ++i) {
            const int width = bs.readUInt16();
            const int height = bs.readUInt16();
            const uint16_t marker = bs.readUInt16();
            if (marker == 65495) throw std::runtime_error("jpeg atlas is unsupported");

            bs.setPosition(bs.getPosition() - 2);
            auto bmp = std::unique_ptr<ImageBitmap>(ImageBitmap::create(width, height));
            auto* px = bmp->getPixels();
            const int count = width * height;
            for (int j = 0; j < count && !bs.hasErrorOccurred(); ++j) {
                const uint8_t alpha = bs.readUInt8();
                const uint8_t red = bs.readUInt8();
                const uint8_t green = bs.readUInt8();
                const uint8_t blue = bs.readUInt8();
                px[j] = ImageColor(red, green, blue, alpha);
            }
            bitmaps.push_back(std::move(bmp));
        }
        return bitmaps;
    }

    [[nodiscard]] static json::MutValue parseSplit(std::span<const uint8_t> data, json::MutDocument& doc) {
        Stream bs(data, Stream::Endian::Big);
        if (bs.readUInt8() != 0xFF) throw std::runtime_error("invalid bbone atlas");

        const uint16_t bitmapCount = bs.readUInt16();
        for (uint16_t i = 0; i < bitmapCount && !bs.hasErrorOccurred(); ++i) {
            const uint16_t w = bs.readUInt16();
            const uint16_t h = bs.readUInt16();
            const uint16_t marker = bs.readUInt16();
            if (marker == 65495) {
                const uint32_t size = bs.readUInt32();
                bs.setPosition(bs.getPosition() + size);
            } else {
                bs.setPosition(bs.getPosition() - 2 + static_cast<size_t>(w) * h * 4);
            }
        }

        auto records = doc.mut_arr();
        const uint16_t frameCount = bs.readUInt16();
        for (uint16_t i = 0; i < frameCount && !bs.hasErrorOccurred(); ++i) {
            auto name = Detail::readUtf(bs);
            const uint32_t total = bs.readUInt32();
            for (uint32_t j = 0; j < total && !bs.hasErrorOccurred(); ++j) {
                if (bs.readUInt8() != 0xFF) break;
                auto record = doc.mut_obj();
                doc.obj_add_str(record, "name", name);
                doc.obj_add_int(record, "bitmap_id", bs.readUInt16());
                doc.obj_add_int(record, "rect_x", bs.readInt16());
                doc.obj_add_int(record, "rect_y", bs.readInt16());
                doc.obj_add_int(record, "rect_w", bs.readUInt16());
                doc.obj_add_int(record, "rect_h", bs.readUInt16());
                doc.obj_add_real(record, "origin_x", bs.readFloat32());
                doc.obj_add_real(record, "origin_y", bs.readFloat32());
                doc.obj_add_real(record, "scale_x", bs.readFloat32());
                doc.obj_add_real(record, "scale_y", bs.readFloat32());
                doc.obj_add_real(record, "rotation", bs.readFloat32());
                records.arr_append(record);
            }
        }
        return records;
    }

    static void cropAndSave(const ImageBitmap& atlas, int x, int y, int w, int h, const std::string& path) {
        x = std::max(x, 0);
        y = std::max(y, 0);
        w = std::min(w, atlas.getWidth() - x);
        h = std::min(h, atlas.getHeight() - y);
        if (w <= 0 || h <= 0) return;

        auto out = std::unique_ptr<ImageBitmap>(ImageBitmap::create(w, h));
        const auto* src = atlas.getPixels();
        auto* dst = out->getPixels();
        const int aw = atlas.getWidth();
        for (int cy = 0; cy < h; ++cy)
            std::copy_n(src + static_cast<size_t>(y + cy) * aw + x, w, dst + static_cast<size_t>(cy) * w);
        out->save(path);
    }

public:
    [[nodiscard]] static bool decode(const std::string& filePath, const std::string& outputDir) {
        try {
            auto file = FileUtils::readFileBytes(filePath);
            Stream bs(file, Stream::Endian::Big);
            if (bs.readUInt16() != MAGIC) return false;

            const uint16_t headerLen = bs.readUInt16();
            if (headerLen > bs.getLength()) return false;
            bs.setPosition(headerLen);
            auto compressed = bs.readSpan(bs.getLength() - headerLen);
            auto uncompressedOpt = zlib_ns::Decompressor::decompress(compressed);
            if (!uncompressedOpt) return false;
            auto& uncompressed = *uncompressedOpt;

            Stream payload(uncompressed, Stream::Endian::Big);
            std::vector<PluginMapEntry> entries;
            while (payload.getPosition() < payload.getLength()) {
                const uint8_t id = payload.readUInt8();
                if (id == 0 || payload.hasErrorOccurred()) break;
                entries.push_back({id, payload.readUInt32(), payload.readUInt32()});
            }

            const size_t blobStart = payload.getPosition();
            std::map<uint8_t, std::span<const uint8_t>> plugins;
            for (const auto& e : entries) {
                const size_t begin = blobStart + e.offset;
                const size_t end = begin + e.length;
                if (begin <= end && end <= uncompressed.size()) plugins[e.id] = {uncompressed.data() + begin, e.length};
            }

            FileUtils::createDirectory(outputDir);
            json::MutDocument doc;
            auto root = doc.mut_obj();
            doc.set_root(root);

            if (auto it = plugins.find(1); it != plugins.end()) {
                auto atlas = parseAtlas(it->second);
                auto plist = parseSplit(it->second, doc);
                if (!atlas.empty() && plist.is_arr()) {
                    for (auto entry : plist.mut_array()) {
                        auto name = std::string(entry.mut_obj_get("name").get_str());
                        cropAndSave(*atlas.front(),
                                    static_cast<int>(entry.mut_obj_get("rect_x").get_sint()),
                                    static_cast<int>(entry.mut_obj_get("rect_y").get_sint()),
                                    static_cast<int>(entry.mut_obj_get("rect_w").get_sint()),
                                    static_cast<int>(entry.mut_obj_get("rect_h").get_sint()),
                                    FileUtils::joinPath(outputDir, name + ".png"));
                    }
                }
                root.obj_add(doc.mut_str("plist"), plist);
            }

            if (auto it = plugins.find(3); it != plugins.end()) root.obj_add(doc.mut_str("labels"), parseLabels(it->second, doc));
            if (auto it = plugins.find(2); it != plugins.end()) root.obj_add(doc.mut_str("animation"), decodeAnimation(it->second, doc));

            return FileUtils::writeTextFile(FileUtils::joinPath(outputDir, "animation.json"), doc.write(json::WriteFlag::Pretty));
        } catch (...) {
            return false;
        }
    }
};

namespace XFL {

class Decoder {
public:
    [[nodiscard]] static bool decode(const std::string& inputDir, const std::string& outputFile) {
        try {
            const auto domPath = FileUtils::joinPath(inputDir, "DOMDocument.xml");
            if (!FileUtils::fileExists(domPath)) return false;

            auto domResult = xml::Document::load_file(domPath);
            if (!domResult) return false;
            auto& dom = *domResult;

            constexpr double dpik = 0.78125;
            json::MutDocument doc;
            auto root = doc.mut_obj();
            doc.set_root(root);

            auto plist = doc.mut_arr();
            if (auto desc = dom.child("DOMDocument").child("Description")) {
                auto shadow = json::Document::parse(std::string(desc.text()));
                if (shadow) {
                    auto p = shadow.root().obj_get("plist");
                    if (p && p.is_arr()) plist = doc.mut_copy(p);
                }
            }
            root.obj_add(doc.mut_str("plist"), plist);

            auto labels = doc.mut_obj();
            auto layers = dom.child("DOMDocument").child("timelines").child("DOMTimeline").child("layers");
            for (auto layer : layers.children("DOMLayer")) {
                if (layer.attribute("name").value() != std::string_view("Labels")) continue;
                for (auto frame : layer.child("frames").children("DOMFrame")) {
                    auto label = frame.attribute("name").value();
                    if (!label.empty()) labels.obj_add(doc.mut_str(label), doc.mut_int(frame.attribute("index").as_int(0)));
                }
            }
            root.obj_add(doc.mut_str("labels"), labels);

            std::vector<xml::Node> visualLayers;
            for (auto layer : layers.children("DOMLayer"))
                if (layer.attribute("name").value() != std::string_view("Labels")) visualLayers.push_back(layer);

            int maxFrame = -1;
            for (auto layer : visualLayers) {
                for (auto frame : layer.child("frames").children("DOMFrame")) {
                    const int index = static_cast<int>(frame.attribute("index").as_int(0));
                    const int duration = static_cast<int>(frame.attribute("duration").as_int(1));
                    maxFrame = std::max(maxFrame, index + duration - 1);
                }
            }

            auto animation = doc.mut_obj();
            animation.obj_add(doc.mut_str("shared_animations"), doc.mut_obj());
            auto frames = doc.mut_arr();

            for (int i = 0; i <= maxFrame; ++i) {
                auto frameObj = doc.mut_obj();
                auto children = doc.mut_arr();
                for (auto it = visualLayers.rbegin(); it != visualLayers.rend(); ++it) {
                    xml::Node current;
                    for (auto frame : it->child("frames").children("DOMFrame")) {
                        const int index = static_cast<int>(frame.attribute("index").as_int(0));
                        const int duration = static_cast<int>(frame.attribute("duration").as_int(1));
                        if (i >= index && i < index + duration) { current = frame; break; }
                    }
                    auto inst = current.child("elements").child("DOMSymbolInstance");
                    if (!inst) continue;

                    std::string name(inst.attribute("libraryItemName").value());
                    if (name.starts_with("sprite/")) name.erase(0, 7);

                    auto bone = doc.mut_obj();
                    doc.obj_add_str(bone, "name", name);

                    auto mat = inst.child("matrix").child("Matrix");
                    auto matrix = doc.mut_obj();
                    doc.obj_add_real(matrix, "a", mat.attribute("a").as_double(1.0) / dpik);
                    doc.obj_add_real(matrix, "b", mat.attribute("b").as_double(0.0) / dpik);
                    doc.obj_add_real(matrix, "c", mat.attribute("c").as_double(0.0) / dpik);
                    doc.obj_add_real(matrix, "d", mat.attribute("d").as_double(1.0) / dpik);
                    doc.obj_add_real(matrix, "tx", mat.attribute("tx").as_double(0.0) / dpik);
                    doc.obj_add_real(matrix, "ty", mat.attribute("ty").as_double(0.0) / dpik);
                    bone.obj_add(doc.mut_str("matrix"), matrix);

                    auto color = doc.mut_obj();
                    auto col = inst.child("color").child("Color");
                    doc.obj_add_real(color, "alphaMultiplier", col ? col.attribute("alphaMultiplier").as_double(1.0) : 1.0);
                    bone.obj_add(doc.mut_str("color"), color);
                    bone.obj_add(doc.mut_str("children"), doc.mut_arr());
                    children.arr_append(bone);
                }
                frameObj.obj_add(doc.mut_str("children"), children);
                frames.arr_append(frameObj);
            }

            animation.obj_add(doc.mut_str("frames"), frames);
            root.obj_add(doc.mut_str("animation"), animation);

            FileUtils::createDirectory(FileUtils::getParentDirectory(outputFile));
            if (!FileUtils::writeTextFile(outputFile, doc.write(json::WriteFlag::Pretty))) return false;

            const auto mediaDir = FileUtils::joinPath(inputDir, "library/media");
            const auto outDir = FileUtils::getParentDirectory(outputFile);
            if (FileUtils::isDirectory(mediaDir)) {
                for (const auto& f : FileUtils::collectFilesByExtension(mediaDir, {".png"}))
                    FileUtils::copyFile(f, FileUtils::joinPath(outDir, FileUtils::getFileName(f)));
            }
            return true;
        } catch (...) {
            return false;
        }
    }
};

}

}
