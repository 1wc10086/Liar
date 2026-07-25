module;
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <map>
#include <span>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.popanim.encode;
import utility.binary.unified_binary_stream;
import utility.io;
import utility.json;
import utility.xml.xml;
import tool.popcap.popanim.core;
import tool.popcap.popanim.utils;

export namespace PopAnim {

class Encoder {
public:
    [[nodiscard]] static std::vector<uint8_t> encode(std::string_view source) {
        std::string input(source);
        auto doc = json::Document::parse(input, json::ReadFlag::Insitu);
        if (!doc || !doc.root().is_obj()) return {};
        try {
            auto root = doc.root(); UnifiedBinaryStream stream;
            stream.writeUInt32(Magic);
            const int version = Utils::integer(Utils::member(root, "version"), 6);
            stream.writeInt32(version); if (version > MaxVersion) return {};
            stream.writeUInt8(static_cast<uint8_t>(Utils::integer(Utils::member(root, "frame_rate"), 30)));
            writePair(stream, Utils::member(root, "position"), 0); writePair(stream, Utils::member(root, "size"), -1);
            auto images = Utils::member(root, "image"); writeCount(stream, images); if (images && images.is_arr()) for (auto image : images.array()) writeImage(stream, image, version);
            auto sprites = Utils::member(root, "sprite"); writeCount(stream, sprites); if (sprites && sprites.is_arr()) for (auto sprite : sprites.array()) writeSprite(stream, sprite, version);
            auto main = Utils::member(root, "main_sprite");
            if (version <= 3) writeSprite(stream, main, version);
            else { stream.writeBool(main && main.is_obj()); if (main && main.is_obj()) writeSprite(stream, main, version); }
            return stream.hasErrorOccurred() ? std::vector<uint8_t>{} : stream.toByteArray();
        } catch (...) { return {}; }
    }

private:
    static void writeCount(UnifiedBinaryStream& stream, json::Value values) { stream.writeInt16(static_cast<int16_t>(values && values.is_arr() ? values.arr_size() : 0)); }
    static void writePair(UnifiedBinaryStream& stream, json::Value values, int fallback) {
        stream.writeInt16(static_cast<int16_t>(Utils::numberAt(values, 0, fallback) * 20.0)); stream.writeInt16(static_cast<int16_t>(Utils::numberAt(values, 1, fallback) * 20.0));
    }
    static void writeText(UnifiedBinaryStream& stream, json::Value object, std::string_view key) { stream.writeStringByInt16Head(Utils::string(Utils::member(object, key))); }

    static void writeImage(UnifiedBinaryStream& stream, json::Value value, int version) {
        writeText(stream, value, "name"); auto size = Utils::member(value, "size");
        if (version >= 4) { stream.writeInt16(static_cast<int16_t>(Utils::integerAt(size, 0, -1))); stream.writeInt16(static_cast<int16_t>(Utils::integerAt(size, 1, -1))); }
        auto transform = Utils::member(value, "transform"); const auto count = transform && transform.is_arr() ? transform.arr_size() : 0;
        if (version == 1) {
            if (count < 2) { stream.writeInt16(0); stream.writeInt16(0); stream.writeInt16(0); }
            else if (count >= 4) { const auto c = std::clamp(Utils::numberAt(transform, 0), -1.0, 1.0); auto angle = std::acos(c); if (Utils::numberAt(transform, 1) < 0) angle = -angle; stream.writeInt16(static_cast<int16_t>(angle)); stream.writeInt16(static_cast<int16_t>(count >= 6 ? Utils::numberAt(transform, 4) * 20.0 : 0)); stream.writeInt16(static_cast<int16_t>(count >= 6 ? Utils::numberAt(transform, 5) * 20.0 : 0)); }
            else { stream.writeInt16(0); stream.writeInt16(static_cast<int16_t>(Utils::numberAt(transform, 0) * 20.0)); stream.writeInt16(static_cast<int16_t>(Utils::numberAt(transform, 1) * 20.0)); }
        } else if (count >= 4) {
            stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 0) * 1310720.0)); stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 2) * 1310720.0)); stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 1) * 1310720.0)); stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 3) * 1310720.0));
            stream.writeInt16(static_cast<int16_t>(count >= 6 ? Utils::numberAt(transform, 4) * 20.0 : 0)); stream.writeInt16(static_cast<int16_t>(count >= 6 ? Utils::numberAt(transform, 5) * 20.0 : 0));
        } else { stream.writeInt32(1310720); stream.writeInt32(0); stream.writeInt32(0); stream.writeInt32(1310720); stream.writeInt16(static_cast<int16_t>(count ? Utils::numberAt(transform, 0) * 20.0 : 0)); stream.writeInt16(static_cast<int16_t>(count > 1 ? Utils::numberAt(transform, 1) * 20.0 : 0)); }
    }

    static void writeSprite(UnifiedBinaryStream& stream, json::Value value, int version) {
        if (version >= 4) { writeText(stream, value, "name"); if (version >= 6) writeText(stream, value, "description"); stream.writeInt32(static_cast<int32_t>(Utils::number(Utils::member(value, "frame_rate"), -1.0) * 65536.0)); }
        auto frames = Utils::member(value, "frame"); const auto count = frames && frames.is_arr() ? frames.arr_size() : 0; auto area = Utils::member(value, "work_area"); const auto areaCount = area && area.is_arr() ? area.arr_size() : 0;
        const int start = Utils::integerAt(area, 0), length = areaCount > 1 ? Utils::integerAt(area, 1) : static_cast<int>(count);
        if (version >= 5) { if (areaCount < 2) { stream.writeInt16(1); stream.writeInt16(0); stream.writeInt16(0); } else { stream.writeInt16(static_cast<int16_t>(length)); stream.writeInt16(static_cast<int16_t>(start)); stream.writeInt16(static_cast<int16_t>(start + length - 1)); } }
        else stream.writeInt16(static_cast<int16_t>(areaCount < 2 ? 1 : length));
        if (frames && frames.is_arr()) for (auto frame : frames.array()) writeFrame(stream, frame, version);
    }

    static void writeEntryCount(UnifiedBinaryStream& stream, size_t count) { if (count < 255) stream.writeUInt8(static_cast<uint8_t>(count)); else { stream.writeUInt8(255); stream.writeInt16(static_cast<int16_t>(count)); } }
    static void writeIndex(UnifiedBinaryStream& stream, int index, int limit) { if (index >= limit || index < 0) { stream.writeInt16(static_cast<int16_t>(limit)); stream.writeInt32(index); } else stream.writeInt16(static_cast<int16_t>(index)); }

    static void writeFrame(UnifiedBinaryStream& stream, json::Value value, int version) {
        const auto remove = Utils::member(value, "remove"), append = Utils::member(value, "append"), change = Utils::member(value, "change"), commands = Utils::member(value, "command");
        const auto label = Utils::string(Utils::member(value, "label"));
        const int flags = (remove && remove.is_arr() && remove.arr_size() ? 1 : 0) | (append && append.is_arr() && append.arr_size() ? 2 : 0) | (change && change.is_arr() && change.arr_size() ? 4 : 0) | (!label.empty() ? 8 : 0) | (Utils::boolean(Utils::member(value, "stop")) ? 16 : 0) | (commands && commands.is_arr() && commands.arr_size() ? 32 : 0);
        stream.writeUInt8(static_cast<uint8_t>(flags));
        if (flags & 1) { writeEntryCount(stream, remove.arr_size()); for (auto item : remove.array()) writeIndex(stream, Utils::integer(Utils::member(item, "index")), 2047); }
        if (flags & 2) { writeEntryCount(stream, append.arr_size()); for (auto item : append.array()) writeAppend(stream, item, version); }
        if (flags & 4) { writeEntryCount(stream, change.arr_size()); for (auto item : change.array()) writeChange(stream, item, version); }
        if (flags & 8) stream.writeStringByInt16Head(label);
        if (flags & 32) { const auto n = std::min<size_t>(commands.arr_size(), 255); stream.writeUInt8(static_cast<uint8_t>(n)); size_t i = 0; for (auto item : commands.array()) { if (i++ == n) break; writeText(stream, item, "command"); writeText(stream, item, "parameter"); } }
    }

    static void writeAppend(UnifiedBinaryStream& stream, json::Value value, int version) {
        const int index = Utils::integer(Utils::member(value, "index")); const int preload = Utils::integer(Utils::member(value, "preload_frames")); const auto name = Utils::string(Utils::member(value, "name")); const auto timeScale = Utils::number(Utils::member(value, "timescale"), 1.0);
        int bits = index >= 2047 || index < 0 ? 2047 : index; bits |= Utils::boolean(Utils::member(value, "sprite")) ? 32768 : 0; bits |= Utils::boolean(Utils::member(value, "additive")) ? 16384 : 0; bits |= preload ? 8192 : 0; bits |= !name.empty() ? 4096 : 0; bits |= timeScale != 1.0 ? 2048 : 0;
        stream.writeUInt16(static_cast<uint16_t>(bits)); if (index >= 2047 || index < 0) stream.writeInt32(index);
        const int resource = Utils::integer(Utils::member(value, "resource")); if (version >= 6 && (resource >= 255 || resource < 0)) { stream.writeUInt8(255); stream.writeInt16(static_cast<int16_t>(resource)); } else stream.writeUInt8(static_cast<uint8_t>(resource));
        if (preload) stream.writeInt16(static_cast<int16_t>(preload)); if (!name.empty()) stream.writeStringByInt16Head(name); if (timeScale != 1.0) stream.writeInt32(static_cast<int32_t>(timeScale * 65536.0));
    }

    static void writeChange(UnifiedBinaryStream& stream, json::Value value, int version) {
        const int index = Utils::integer(Utils::member(value, "index")); auto transform = Utils::member(value, "transform"); const auto count = transform && transform.is_arr() ? transform.arr_size() : 0; auto rect = Utils::member(value, "src_rect"); auto color = Utils::member(value, "color"); const int anim = Utils::integer(Utils::member(value, "anim_frame_num"));
        int bits = index >= 1023 || index < 0 ? 1023 : index; bits |= count == 6 ? 4096 : count == 3 ? 16384 : 0; bits |= version >= 5 ? 2048 : 0; bits |= rect && rect.is_arr() && rect.arr_size() >= 4 ? 32768 : 0; bits |= color && color.is_arr() && color.arr_size() >= 4 ? 8192 : 0; bits |= anim ? 1024 : 0;
        stream.writeUInt16(static_cast<uint16_t>(bits)); if (index >= 1023 || index < 0) stream.writeInt32(index);
        if (count == 6) { stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 0) * 65536.0)); stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 2) * 65536.0)); stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 1) * 65536.0)); stream.writeInt32(static_cast<int32_t>(Utils::numberAt(transform, 3) * 65536.0)); } else if (count == 3) stream.writeInt16(static_cast<int16_t>(Utils::numberAt(transform, 0) * 1000.0));
        const auto x = count >= 2 ? Utils::numberAt(transform, count - 2) : 0.0, y = count ? Utils::numberAt(transform, count - 1) : 0.0;
        if (version >= 5) { stream.writeInt32(static_cast<int32_t>(x * 20.0)); stream.writeInt32(static_cast<int32_t>(y * 20.0)); } else { stream.writeInt16(static_cast<int16_t>(x * 20.0)); stream.writeInt16(static_cast<int16_t>(y * 20.0)); }
        if (bits & 32768) for (size_t i = 0; i < 4; ++i) stream.writeInt16(static_cast<int16_t>(Utils::integerAt(rect, i) * 20));
        if (bits & 8192) for (size_t i = 0; i < 4; ++i) stream.writeUInt8(static_cast<uint8_t>(Utils::numberAt(color, i) * 255.0));
        if (anim) stream.writeInt16(static_cast<int16_t>(anim));
    }
};

class XflEncoder {
public:
    [[nodiscard]] static bool encode(std::string_view source, std::string_view outputFolder, int resolution) {
        std::string input(source);
        auto document = json::Document::parse(input, json::ReadFlag::Insitu);
        if (!document || !document.root().is_obj() || resolution <= 0) return false;
        try {
            const auto root = document.root(); const std::string output(outputFolder);
            for (const auto path : {"LIBRARY/image", "LIBRARY/sprite", "LIBRARY/source", "LIBRARY/media"}) FileUtils::createDirectory(FileUtils::joinPath(output, path));
            json::MutDocument extra; auto meta = extra.mut_obj(); extra.set_root(meta);
            Utils::add(extra, meta, "version", extra.mut_int(Utils::integer(Utils::member(root, "version"), 6)));
            Utils::add(extra, meta, "xfl_resolution", extra.mut_int(resolution));
            for (const auto key : {"position"}) {
                auto value = Utils::member(root, key); if (value) Utils::add(extra, meta, key, Utils::copy(extra, value));
            }
            auto imageMeta = extra.mut_arr();
            auto images = Utils::member(root, "image");
            if (images && images.is_arr()) for (auto image : images.array()) {
                auto item = extra.mut_obj();
                for (const auto key : {"name", "size"}) { auto value = Utils::member(image, key); if (value) Utils::add(extra, item, key, Utils::copy(extra, value)); }
                imageMeta.arr_append(item);
            }
            Utils::add(extra, meta, "image", imageMeta);
            auto spriteMeta = extra.mut_arr();
            auto sprites = Utils::member(root, "sprite");
            if (sprites && sprites.is_arr()) for (auto sprite : sprites.array()) spriteMeta.arr_append(metadata(extra, sprite));
            Utils::add(extra, meta, "sprite", spriteMeta);
            auto main = Utils::member(root, "main_sprite");
            Utils::add(extra, meta, "main_sprite", main && main.is_obj() ? metadata(extra, main) : extra.mut_null());
            if (!FileUtils::writeTextFile(FileUtils::joinPath(output, "extra.json"), extra.write(json::WriteFlag::Pretty))) return false;
            const auto frameRate = Utils::integer(Utils::member(root, "frame_rate"), 30); const auto size = Utils::member(root, "size");
            std::string media, symbols;
            if (images && images.is_arr()) {
                size_t index = 0;
                for (auto image : images.array()) {
                    const auto id = std::to_string(++index);
                    const auto png = "image_" + id + ".png";
                    media += "<DOMBitmapItem name=\"media/" + png + "\" href=\"media/" + png + "\" width=\"" + std::to_string(Utils::integerAt(Utils::member(image, "size"), 0, -1)) + "\" height=\"" + std::to_string(Utils::integerAt(Utils::member(image, "size"), 1, -1)) + "\"/>";
                    symbols += "<Include href=\"image/image_" + id + ".xml\"/>";
                    if (!FileUtils::writeTextFile(FileUtils::joinPath(output, "LIBRARY/media/" + png), defaultPng())) return false;
                    if (!FileUtils::writeTextFile(FileUtils::joinPath(output, "LIBRARY/image/image_" + id + ".xml"), imageXml(id, image))) return false;
                }
            }
            if (sprites && sprites.is_arr()) {
                size_t index = 0;
                for (auto sprite : sprites.array()) {
                    const auto id = std::to_string(++index);
                    symbols += "<Include href=\"sprite/sprite_" + id + ".xml\"/>";
                    if (!FileUtils::writeTextFile(FileUtils::joinPath(output, "LIBRARY/sprite/sprite_" + id + ".xml"), spriteXml("sprite_" + id, sprite, root, resolution))) return false;
                }
            }
            if (main && main.is_obj()) {
                symbols += "<Include href=\"main_sprite.xml\"/>";
                if (!FileUtils::writeTextFile(FileUtils::joinPath(output, "LIBRARY/main_sprite.xml"), spriteXml("main_sprite", main, root, resolution))) return false;
            }
            std::string dom = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<DOMDocument xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://ns.adobe.com/xfl/2008/\" frameRate=\"" + std::to_string(frameRate) + "\" width=\"" + std::to_string(Utils::integerAt(size, 0)) + "\" height=\"" + std::to_string(Utils::integerAt(size, 1)) + "\" xflVersion=\"2.971\"><folders><DOMFolderItem name=\"media\"/><DOMFolderItem name=\"source\"/><DOMFolderItem name=\"image\"/><DOMFolderItem name=\"sprite\"/></folders><media>" + media + "</media><symbols>" + symbols + "</symbols></DOMDocument>";
            if (!FileUtils::writeTextFile(FileUtils::joinPath(output, "DOMDocument.xml"), dom)) return false;
            return FileUtils::writeTextFile(FileUtils::joinPath(output, "main.xfl"), Utils::XflContent);
        } catch (...) { return false; }
    }

private:
    static std::string escape(std::string_view value) {
        std::string result;
        for (const auto c : value) {
            if (c == '&') result += "&amp;";
            else if (c == '<') result += "&lt;";
            else if (c == '"') result += "&quot;";
            else result += c;
        }
        return result;
    }

    static json::MutValue metadata(json::MutDocument& doc, json::Value sprite) {
        auto result = doc.mut_obj();
        for (const auto key : {"name", "description", "frame_rate", "work_area"}) {
            auto value = Utils::member(sprite, key);
            if (value) Utils::add(doc, result, key, Utils::copy(doc, value));
        }
        return result;
    }

    static std::string defaultPng() {
        constexpr unsigned char bytes[] = {
            0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1f, 0x15, 0xc4,
            0x89, 0x00, 0x00, 0x00, 0x0b, 0x49, 0x44, 0x41, 0x54, 0x08, 0x99, 0x63, 0x60, 0x00, 0x02, 0x00,
            0x00, 0x05, 0x00, 0x01, 0xe9, 0xfa, 0xdc, 0x68, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4e, 0x44,
            0xae, 0x42, 0x60, 0x82
        };
        return {reinterpret_cast<const char*>(bytes), sizeof(bytes)};
    }

    static std::string matrix(json::Value transform, int resolution) {
        const auto value = Utils::standardTransform(transform);
        return "<Matrix a=\"" + Utils::decimal(value[0]) + "\" b=\"" + Utils::decimal(value[1]) + "\" c=\"" + Utils::decimal(value[2]) + "\" d=\"" + Utils::decimal(value[3]) + "\" tx=\"" + Utils::decimal(value[4] * resolution / Utils::StandardResolution) + "\" ty=\"" + Utils::decimal(value[5] * resolution / Utils::StandardResolution) + "\"/>";
    }

    static std::string imageXml(const std::string& id, json::Value image) {
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?><DOMSymbolItem xmlns=\"http://ns.adobe.com/xfl/2008/\" name=\"image/image_" + id + "\"><timeline><DOMTimeline name=\"image_" + id + "\"><layers><DOMLayer name=\"Layer 1\"><frames><DOMFrame index=\"0\"><elements><DOMBitmapInstance libraryItemName=\"media/image_" + id + ".png\"><matrix>" + matrix(Utils::member(image, "transform"), Utils::StandardResolution) + "</matrix></DOMBitmapInstance></elements></DOMFrame></frames></DOMLayer></layers></DOMTimeline></timeline></DOMSymbolItem>";
    }

    static std::string spriteXml(const std::string& name, json::Value sprite, json::Value root, int resolution) {
        auto frames = Utils::member(sprite, "frame");
        const auto frameCount = frames && frames.is_arr() ? frames.arr_size() : 0;
        struct Instance { json::Value append; json::Value change; };
        std::map<int, Instance> active;
        std::map<int, std::string> layers;
        for (size_t frameIndex = 0; frameIndex < frameCount; ++frameIndex) {
            auto frame = Utils::element(frames, frameIndex);
            auto remove = Utils::member(frame, "remove");
            if (remove && remove.is_arr()) for (auto item : remove.array()) active.erase(Utils::integer(Utils::member(item, "index")));
            auto append = Utils::member(frame, "append");
            if (append && append.is_arr()) for (auto item : append.array()) active[Utils::integer(Utils::member(item, "index"))] = {item, {}};
            auto change = Utils::member(frame, "change");
            if (change && change.is_arr()) for (auto item : change.array()) {
                const auto index = Utils::integer(Utils::member(item, "index"));
                if (auto found = active.find(index); found != active.end()) found->second.change = item;
            }
            for (const auto& [index, item] : active) layers[index] += frameXml(frameIndex, item.append, item.change, root, resolution, Utils::string(Utils::member(frame, "label")), Utils::boolean(Utils::member(frame, "stop")), Utils::member(frame, "command"));
        }
        std::string result = "<?xml version=\"1.0\" encoding=\"utf-8\"?><DOMSymbolItem xmlns=\"http://ns.adobe.com/xfl/2008/\" name=\"" + escape(name) + "\"><timeline><DOMTimeline name=\"" + escape(name) + "\"><layers>";
        for (const auto& [index, content] : layers) result += "<DOMLayer name=\"" + std::to_string(index) + "\"><frames>" + content + "</frames></DOMLayer>";
        return result + "</layers></DOMTimeline></timeline></DOMSymbolItem>";
    }

    static std::string frameXml(size_t index, json::Value item, json::Value change, json::Value root, int resolution, std::string_view label, bool stop, json::Value commands) {
        const auto resource = Utils::integer(Utils::member(item, "resource"));
        const auto library = std::string(Utils::boolean(Utils::member(item, "sprite")) ? "sprite/sprite_" : "image/image_") + std::to_string(resource + 1);
        std::string result = "<DOMFrame index=\"" + std::to_string(index) + "\" duration=\"1\"";
        if (!label.empty()) result += " name=\"" + escape(label) + "\"";
        if (stop) result += " PAMStop=\"true\"";
        result += "><elements><DOMSymbolInstance libraryItemName=\"" + escape(library) + "\" PAMResource=\"" + std::to_string(resource) + "\" PAMSprite=\"" + (Utils::boolean(Utils::member(item, "sprite")) ? "true" : "false") + "\" PAMAdditive=\"" + (Utils::boolean(Utils::member(item, "additive")) ? "true" : "false") + "\" PAMPreload=\"" + std::to_string(Utils::integer(Utils::member(item, "preload_frames"))) + "\" PAMTimeScale=\"" + Utils::decimal(Utils::number(Utils::member(item, "timescale"), 1.0)) + "\"";
        const auto instanceName = Utils::string(Utils::member(item, "name")); if (!instanceName.empty()) result += " name=\"" + escape(instanceName) + "\"";
        auto rect = Utils::member(change, "src_rect");
        if (rect && rect.is_arr() && rect.arr_size() >= 4) result += " PAMSrcRect=\"" + Utils::decimal(Utils::numberAt(rect, 0)) + "," + Utils::decimal(Utils::numberAt(rect, 1)) + "," + Utils::decimal(Utils::numberAt(rect, 2)) + "," + Utils::decimal(Utils::numberAt(rect, 3)) + "\"";
        auto color = Utils::member(change, "color");
        if (color && color.is_arr() && color.arr_size() >= 4) result += " PAMColor=\"" + Utils::decimal(Utils::numberAt(color, 0)) + "," + Utils::decimal(Utils::numberAt(color, 1)) + "," + Utils::decimal(Utils::numberAt(color, 2)) + "," + Utils::decimal(Utils::numberAt(color, 3)) + "\"";
        const auto anim = Utils::integer(Utils::member(change, "anim_frame_num")); if (anim) result += " PAMAnimFrame=\"" + std::to_string(anim) + "\"";
        result += "><matrix>" + matrix(change ? Utils::member(change, "transform") : Utils::member(item, "transform"), resolution) + "</matrix></DOMSymbolInstance></elements>";
        if (commands && commands.is_arr() && commands.arr_size()) { result += "<Actionscript>"; for (auto command : commands.array()) result += escape(Utils::string(Utils::member(command, "command"))) + "(" + escape(Utils::string(Utils::member(command, "parameter"))) + ");\n"; result += "</Actionscript>"; }
        return result + "</DOMFrame>";
    }
};

}
