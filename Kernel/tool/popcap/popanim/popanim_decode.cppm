module;
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <cstdlib>
#include <map>
#include <sstream>
#include <span>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.popanim.decode;
import utility.binary.unified_binary_stream;
import utility.io;
import utility.json;
import utility.xml.xml;
import tool.popcap.popanim.core;
import tool.popcap.popanim.utils;

export namespace PopAnim {

class Decoder {
public:
    [[nodiscard]] static std::string decode(std::span<const uint8_t> bytes) {
        UnifiedBinaryStream stream(bytes);
        if (stream.readUInt32() != Magic) return {};
        const int version = stream.readInt32();
        if (version > MaxVersion) return {};
        json::MutDocument doc;
        auto root = doc.mut_obj();
        doc.set_root(root);
        try {
            Utils::add(doc, root, "version", doc.mut_int(version));
            const int frameRate = stream.readUInt8();
            Utils::add(doc, root, "frame_rate", doc.mut_int(frameRate));
            Utils::add(doc, root, "position", pair(doc, stream.readInt16() / 20.0, stream.readInt16() / 20.0));
            Utils::add(doc, root, "size", pair(doc, stream.readInt16() / 20.0, stream.readInt16() / 20.0));
            auto images = doc.mut_arr();
            Utils::add(doc, root, "image", images);
            for (int n = std::max(0, static_cast<int>(stream.readInt16())); n-- > 0;) images.arr_append(image(stream, version, doc));
            auto sprites = doc.mut_arr();
            Utils::add(doc, root, "sprite", sprites);
            for (int n = std::max(0, static_cast<int>(stream.readInt16())); n-- > 0;) sprites.arr_append(sprite(stream, version, frameRate, doc));
            Utils::add(doc, root, "main_sprite", version <= 3 || stream.readBool() ? sprite(stream, version, frameRate, doc) : doc.mut_null());
            return stream.hasErrorOccurred() ? std::string{} : doc.write(json::WriteFlag::Pretty);
        } catch (...) { return {}; }
    }

private:
    [[nodiscard]] static json::MutValue pair(json::MutDocument& doc, double x, double y) {
        auto value = doc.mut_arr(); value.arr_append(doc.mut_real(x)); value.arr_append(doc.mut_real(y)); return value;
    }

    [[nodiscard]] static json::MutValue image(UnifiedBinaryStream& stream, int version, json::MutDocument& doc) {
        auto value = doc.mut_obj();
        Utils::addString(doc, value, "name", stream.readStringByInt16Head());
        auto size = doc.mut_arr();
        Utils::add(doc, value, "size", size);
        if (version >= 4) { size.arr_append(doc.mut_int(stream.readInt16())); size.arr_append(doc.mut_int(stream.readInt16())); }
        else { size.arr_append(doc.mut_int(-1)); size.arr_append(doc.mut_int(-1)); }
        auto transform = doc.mut_arr();
        Utils::add(doc, value, "transform", transform);
        if (version == 1) {
            const auto angle = stream.readInt16() / 1000.0;
            const auto sine = std::sin(angle);
            transform.arr_append(doc.mut_real(std::cos(angle))); transform.arr_append(doc.mut_real(sine));
            transform.arr_append(doc.mut_real(-sine)); transform.arr_append(doc.mut_real(std::cos(angle)));
            transform.arr_append(doc.mut_real(stream.readInt16() / 20.0)); transform.arr_append(doc.mut_real(stream.readInt16() / 20.0));
        } else {
            const auto a = stream.readInt32() / 1310720.0, c = stream.readInt32() / 1310720.0;
            const auto b = stream.readInt32() / 1310720.0, d = stream.readInt32() / 1310720.0;
            for (const auto n : {a, b, c, d, stream.readInt16() / 20.0, stream.readInt16() / 20.0}) transform.arr_append(doc.mut_real(n));
        }
        return value;
    }

    [[nodiscard]] static json::MutValue sprite(UnifiedBinaryStream& stream, int version, int globalRate, json::MutDocument& doc) {
        auto value = doc.mut_obj();
        if (version >= 4) {
            Utils::addString(doc, value, "name", stream.readStringByInt16Head());
            if (version >= 6) Utils::addString(doc, value, "description", stream.readStringByInt16Head());
            else Utils::add(doc, value, "description", doc.mut_null());
            Utils::add(doc, value, "frame_rate", doc.mut_real(stream.readInt32() / 65536.0));
        } else {
            Utils::add(doc, value, "name", doc.mut_null()); Utils::add(doc, value, "description", doc.mut_null()); Utils::add(doc, value, "frame_rate", doc.mut_real(globalRate));
        }
        const auto count = std::max(0, static_cast<int>(stream.readInt16()));
        auto area = doc.mut_arr(); Utils::add(doc, value, "work_area", area);
        area.arr_append(doc.mut_int(version >= 5 ? stream.readInt16() : 0)); if (version >= 5) (void)stream.readInt16(); area.arr_append(doc.mut_int(count));
        auto frames = doc.mut_arr(); Utils::add(doc, value, "frame", frames);
        for (int n = count; n-- > 0;) frames.arr_append(frame(stream, version, doc));
        return value;
    }

    [[nodiscard]] static int count(UnifiedBinaryStream& stream) { const auto n = stream.readUInt8(); return n == 255 ? std::max(0, static_cast<int>(stream.readInt16())) : n; }

    [[nodiscard]] static json::MutValue frame(UnifiedBinaryStream& stream, int version, json::MutDocument& doc) {
        auto value = doc.mut_obj(); const auto flags = stream.readUInt8();
        auto remove = doc.mut_arr(); Utils::add(doc, value, "remove", remove);
        if (flags & 1) for (int n = count(stream); n-- > 0;) { auto item = doc.mut_obj(); auto index = stream.readInt16(); Utils::add(doc, item, "index", doc.mut_int(index >= 2047 ? stream.readInt32() : index)); remove.arr_append(item); }
        auto append = doc.mut_arr(); Utils::add(doc, value, "append", append);
        if (flags & 2) for (int n = count(stream); n-- > 0;) {
            auto item = doc.mut_obj(); const auto bits = stream.readUInt16(); const auto small = bits & 2047;
            Utils::add(doc, item, "index", doc.mut_int(small == 2047 ? stream.readInt32() : small));
            Utils::add(doc, item, "sprite", doc.mut_bool(bits & 32768)); Utils::add(doc, item, "additive", doc.mut_bool(bits & 16384));
            int resource = stream.readUInt8(); if (version >= 6 && resource == 255) resource = stream.readInt16(); Utils::add(doc, item, "resource", doc.mut_int(resource));
            Utils::add(doc, item, "preload_frames", doc.mut_int(bits & 8192 ? stream.readInt16() : 0));
            if (bits & 4096) Utils::addString(doc, item, "name", stream.readStringByInt16Head()); else Utils::add(doc, item, "name", doc.mut_null());
            Utils::add(doc, item, "timescale", doc.mut_real(bits & 2048 ? stream.readInt32() / 65536.0 : 1.0)); append.arr_append(item);
        }
        auto change = doc.mut_arr(); Utils::add(doc, value, "change", change);
        if (flags & 4) for (int n = count(stream); n-- > 0;) {
            auto item = doc.mut_obj(); const auto bits = stream.readUInt16(); const auto small = bits & 1023;
            Utils::add(doc, item, "index", doc.mut_int(small == 1023 ? stream.readInt32() : small)); auto transform = doc.mut_arr(); Utils::add(doc, item, "transform", transform);
            if (bits & 4096) { const auto a = stream.readInt32() / 65536.0, c = stream.readInt32() / 65536.0, b = stream.readInt32() / 65536.0, d = stream.readInt32() / 65536.0; for (auto x : {a, b, c, d}) transform.arr_append(doc.mut_real(x)); }
            else if (bits & 16384) transform.arr_append(doc.mut_real(stream.readInt16() / 1000.0));
            transform.arr_append(doc.mut_real((bits & 2048 ? stream.readInt32() : stream.readInt16()) / 20.0)); transform.arr_append(doc.mut_real((bits & 2048 ? stream.readInt32() : stream.readInt16()) / 20.0));
            if (bits & 32768) { auto rect = doc.mut_arr(); for (int i = 0; i != 4; ++i) rect.arr_append(doc.mut_int(stream.readInt16() / 20)); Utils::add(doc, item, "src_rect", rect); } else Utils::add(doc, item, "src_rect", doc.mut_null());
            if (bits & 8192) { auto color = doc.mut_arr(); for (int i = 0; i != 4; ++i) color.arr_append(doc.mut_real(stream.readUInt8() / 255.0)); Utils::add(doc, item, "color", color); } else Utils::add(doc, item, "color", doc.mut_null());
            Utils::add(doc, item, "anim_frame_num", doc.mut_int(bits & 1024 ? stream.readInt16() : 0)); change.arr_append(item);
        }
        if (flags & 8) Utils::addString(doc, value, "label", stream.readStringByInt16Head()); else Utils::add(doc, value, "label", doc.mut_null());
        Utils::add(doc, value, "stop", doc.mut_bool(flags & 16)); auto commands = doc.mut_arr(); Utils::add(doc, value, "command", commands);
        if (flags & 32) for (int n = stream.readUInt8(); n-- > 0;) { auto command = doc.mut_obj(); Utils::addString(doc, command, "command", stream.readStringByInt16Head()); Utils::addString(doc, command, "parameter", stream.readStringByInt16Head()); commands.arr_append(command); }
        return value;
    }
};

class XflDecoder {
public:
    [[nodiscard]] static std::string decode(std::string_view folder) {
        try {
            const auto base = std::string(folder);
            auto extraText = FileUtils::readTextFile(FileUtils::joinPath(base, "extra.json"));
            auto extra = json::Document::parse(extraText);
            auto dom = xml::Document::load_file(FileUtils::joinPath(base, "DOMDocument.xml"));
            if (!dom) return {};
            auto root = dom->child("DOMDocument");
            if (!root) return {};
            json::MutDocument output; auto result = output.mut_obj(); output.set_root(result);
            auto input = extra && extra.root().is_obj() ? extra.root() : json::Value{};
            Utils::add(output, result, "version", output.mut_int(Utils::integer(Utils::member(input, "version"), 6)));
            Utils::add(output, result, "frame_rate", output.mut_int(root.attribute("frameRate").as_int(30)));
            auto position = Utils::member(input, "position");
            if (position) Utils::add(output, result, "position", Utils::copy(output, position));
            else { auto value = output.mut_arr(); value.arr_append(output.mut_int(0)); value.arr_append(output.mut_int(0)); Utils::add(output, result, "position", value); }
            auto size = output.mut_arr(); size.arr_append(output.mut_int(root.attribute("width").as_int())); size.arr_append(output.mut_int(root.attribute("height").as_int())); Utils::add(output, result, "size", size);
            auto images = output.mut_arr();
            auto imageMeta = Utils::member(input, "image");
            size_t imageIndex = 0;
            if (imageMeta && imageMeta.is_arr()) for (auto metadata : imageMeta.array()) {
                const auto shortName = std::string(Utils::string(Utils::member(metadata, "name")));
                if (shortName.empty()) continue;
                auto image = output.mut_obj(); Utils::add(output, image, "name", Utils::copy(output, Utils::member(metadata, "name")));
                Utils::add(output, image, "size", Utils::copy(output, Utils::member(metadata, "size")));
                auto imageDocument = xml::Document::load_file(FileUtils::joinPath(base, "LIBRARY/image/" + shortName + ".xml"));
                if (!imageDocument) imageDocument = xml::Document::load_file(FileUtils::joinPath(base, "LIBRARY/image/image_" + std::to_string(imageIndex + 1) + ".xml"));
                auto transform = output.mut_arr();
                if (imageDocument) {
                    auto matrix = imageDocument->child("DOMSymbolItem").child("timeline").child("DOMTimeline").child("layers").child("DOMLayer").child("frames").child("DOMFrame").child("elements").child("DOMBitmapInstance").child("matrix").child("Matrix");
                    appendMatrix(output, transform, matrix, Utils::StandardResolution);
                } else appendIdentity(output, transform);
                Utils::add(output, image, "transform", transform); images.arr_append(image); ++imageIndex;
            }
            Utils::add(output, result, "image", images);
            auto sprites = output.mut_arr();
            auto spriteMeta = Utils::member(input, "sprite");
            size_t spriteIndex = 0;
            if (spriteMeta && spriteMeta.is_arr()) for (auto metadata : spriteMeta.array()) {
                const auto name = std::string(Utils::string(Utils::member(metadata, "name")));
                if (!name.empty()) sprites.arr_append(sprite(output, base, name, metadata, Utils::integer(Utils::member(input, "xfl_resolution"), Utils::StandardResolution), spriteIndex));
                ++spriteIndex;
            }
            Utils::add(output, result, "sprite", sprites);
            auto mainMetadata = Utils::member(input, "main_sprite");
            Utils::add(output, result, "main_sprite", mainMetadata && mainMetadata.is_obj() ? sprite(output, base, "main_sprite", mainMetadata, Utils::integer(Utils::member(input, "xfl_resolution"), Utils::StandardResolution)) : output.mut_null());
            return output.write(json::WriteFlag::Pretty);
        } catch (...) { return {}; }
    }

private:
    static void appendIdentity(json::MutDocument& doc, json::MutValue value) {
        for (const auto n : {1.0, 0.0, 0.0, 1.0, 0.0, 0.0}) value.arr_append(doc.mut_real(n));
    }

    template <typename Node>
    static void appendMatrix(json::MutDocument& doc, json::MutValue value, Node matrix, int resolution) {
        if (!matrix) { appendIdentity(doc, value); return; }
        value.arr_append(doc.mut_real(matrix.attribute("a").as_double(1.0)));
        value.arr_append(doc.mut_real(matrix.attribute("b").as_double()));
        value.arr_append(doc.mut_real(matrix.attribute("c").as_double()));
        value.arr_append(doc.mut_real(matrix.attribute("d").as_double(1.0)));
        value.arr_append(doc.mut_real(matrix.attribute("tx").as_double() * Utils::StandardResolution / resolution));
        value.arr_append(doc.mut_real(matrix.attribute("ty").as_double() * Utils::StandardResolution / resolution));
    }

    static json::MutValue vector(json::MutDocument& doc, std::string_view text) {
        if (text.empty()) return doc.mut_null();
        auto result = doc.mut_arr(); std::stringstream stream{std::string(text)}; std::string part;
        while (std::getline(stream, part, ',')) result.arr_append(doc.mut_real(std::strtod(part.c_str(), nullptr)));
        return result.arr_size() == 4 ? result : doc.mut_null();
    }

    template <typename Node>
    static void instance(json::MutDocument& doc, Node xmlFrame, int layer, bool first, json::MutValue append, json::MutValue change, int resolution) {
        auto instance = xmlFrame.child("elements").child("DOMSymbolInstance");
        if (instance) {
            auto item = doc.mut_obj(); Utils::add(doc, item, "index", doc.mut_int(layer));
            const auto library = std::string_view(instance.attribute("libraryItemName").as_string());
            Utils::add(doc, item, "sprite", doc.mut_bool(instance.attribute("PAMSprite").as_bool(library.starts_with("sprite/"))));
            Utils::add(doc, item, "additive", doc.mut_bool(instance.attribute("PAMAdditive").as_bool()));
            Utils::add(doc, item, "resource", doc.mut_int(instance.attribute("PAMResource").as_int()));
            Utils::add(doc, item, "preload_frames", doc.mut_int(instance.attribute("PAMPreload").as_int()));
            Utils::addString(doc, item, "name", instance.attribute("name").as_string());
            Utils::add(doc, item, "timescale", doc.mut_real(instance.attribute("PAMTimeScale").as_double(1.0)));
            if (first) append.arr_append(item);
            auto state = doc.mut_obj(); Utils::add(doc, state, "index", doc.mut_int(layer)); auto transform = doc.mut_arr(); appendMatrix(doc, transform, instance.child("matrix").child("Matrix"), resolution); Utils::add(doc, state, "transform", transform); Utils::add(doc, state, "src_rect", vector(doc, instance.attribute("PAMSrcRect").as_string())); Utils::add(doc, state, "color", vector(doc, instance.attribute("PAMColor").as_string())); Utils::add(doc, state, "anim_frame_num", doc.mut_int(instance.attribute("PAMAnimFrame").as_int())); change.arr_append(state);
        }
    }

    static json::MutValue sprite(json::MutDocument& doc, const std::string& base, const std::string& name, json::Value metadata, int resolution, size_t index = 0) {
        auto result = doc.mut_obj();
        for (const auto key : {"name", "description", "frame_rate", "work_area"}) { auto value = Utils::member(metadata, key); if (value) Utils::add(doc, result, key, Utils::copy(doc, value)); }
        if (!Utils::member(metadata, "name")) Utils::addString(doc, result, "name", name);
        if (!Utils::member(metadata, "description")) Utils::add(doc, result, "description", doc.mut_null());
        if (!Utils::member(metadata, "frame_rate")) Utils::add(doc, result, "frame_rate", doc.mut_real(30.0));
        if (!Utils::member(metadata, "work_area")) { auto area = doc.mut_arr(); area.arr_append(doc.mut_int(0)); area.arr_append(doc.mut_int(0)); Utils::add(doc, result, "work_area", area); }
        auto frames = doc.mut_arr(); Utils::add(doc, result, "frame", frames);
        auto source = xml::Document::load_file(FileUtils::joinPath(base, name == "main_sprite" ? "LIBRARY/main_sprite.xml" : "LIBRARY/sprite/" + name + ".xml"));
        if (!source && name != "main_sprite") source = xml::Document::load_file(FileUtils::joinPath(base, "LIBRARY/sprite/sprite_" + std::to_string(index + 1) + ".xml"));
        if (!source) return result;
        auto timeline = source->child("DOMSymbolItem").child("timeline").child("DOMTimeline");
        std::map<int, bool> active;
        size_t count = 0;
        for (auto layer : timeline.child("layers").children("DOMLayer")) for (auto xmlFrame : layer.child("frames").children("DOMFrame")) count = std::max(count, static_cast<size_t>(xmlFrame.attribute("index").as_int()) + 1);
        for (size_t index = 0; index < count; ++index) {
            auto value = doc.mut_obj(); auto remove = doc.mut_arr(), append = doc.mut_arr(), change = doc.mut_arr(), command = doc.mut_arr();
            Utils::add(doc, value, "remove", remove); Utils::add(doc, value, "append", append); Utils::add(doc, value, "change", change); Utils::add(doc, value, "label", doc.mut_null()); Utils::add(doc, value, "stop", doc.mut_bool(false)); Utils::add(doc, value, "command", command);
            for (auto layer : timeline.child("layers").children("DOMLayer")) {
                const auto layerIndex = layer.attribute("name").as_int(); bool present = false;
                for (auto xmlFrame : layer.child("frames").children("DOMFrame")) if (static_cast<size_t>(xmlFrame.attribute("index").as_int()) == index) {
                    present = true; instance(doc, xmlFrame, layerIndex, !active[layerIndex], append, change, resolution); active[layerIndex] = true;
                    if (const auto label = xmlFrame.attribute("name").as_string(); *label) Utils::addString(doc, value, "label", label);
                    if (xmlFrame.attribute("PAMStop").as_bool()) Utils::add(doc, value, "stop", doc.mut_bool(true));
                    const auto actionNode = xmlFrame.child("Actionscript");
                    const auto scriptNode = actionNode.child("script");
                    const auto actions = scriptNode ? scriptNode.text() : actionNode.text();
                    if (!actions.empty()) {
                        std::stringstream lines{std::string(actions)}; std::string line;
                        while (std::getline(lines, line)) { const auto open = line.find('('), close = line.rfind(')'); if (open == std::string::npos || close < open) continue; auto item = doc.mut_obj(); Utils::addString(doc, item, "command", line.substr(0, open)); Utils::addString(doc, item, "parameter", line.substr(open + 1, close - open - 1)); command.arr_append(item); }
                    }
                }
                if (!present && active[layerIndex]) { auto item = doc.mut_obj(); Utils::add(doc, item, "index", doc.mut_int(layerIndex)); remove.arr_append(item); active[layerIndex] = false; }
            }
            frames.arr_append(value);
        }
        return result;
    }
};

}
