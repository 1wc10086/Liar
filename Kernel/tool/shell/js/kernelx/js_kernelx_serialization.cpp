module;
#include <cstdint>
#include <charconv>
#include <initializer_list>
#include <string>
#include <string_view>
#include <tuple>
#include <utility>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.io;
import utility.json;
import utility.xml.xml;
import utility.serialization.tomlplusplus.tomlplusplus_serialization;
import utility.serialization.rapidyaml.rapidyaml_serialization;
import utility.serialization.tomlplusplus.tomlplusplus_core;
import utility.serialization.rapidyaml.rapidyaml_core;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;

bool boolArg(JSContext* ctx, JSValueConst value, bool fallback) noexcept;
int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;
JSValue object(JSContext* ctx, FnList funcs);

namespace {

JSClassID tomlDocumentClass{};
JSClassID yamlDocumentClass{};

struct TomlDocumentObject { tomlplusplus::Document value; };
struct YamlDocumentObject { rapidyaml::Document value; };

void tomlDocumentFinalizer(JSRuntime*, JSValueConst value) { delete static_cast<TomlDocumentObject*>(JS_GetOpaque(value, tomlDocumentClass)); }
void yamlDocumentFinalizer(JSRuntime*, JSValueConst value) { delete static_cast<YamlDocumentObject*>(JS_GetOpaque(value, yamlDocumentClass)); }
const JSClassDef tomlDocumentClassDef{"TomlDocument", tomlDocumentFinalizer, nullptr, nullptr, nullptr};
const JSClassDef yamlDocumentClassDef{"YamlDocument", yamlDocumentFinalizer, nullptr, nullptr, nullptr};

TomlDocumentObject* tomlDocumentData(JSContext* ctx, JSValueConst value) { return static_cast<TomlDocumentObject*>(JS_GetOpaque2(ctx, value, tomlDocumentClass)); }
YamlDocumentObject* yamlDocumentData(JSContext* ctx, JSValueConst value) { return static_cast<YamlDocumentObject*>(JS_GetOpaque2(ctx, value, yamlDocumentClass)); }

template <class Writer>
JSValue documentText(JSContext* ctx, Writer writer) {
    std::vector<char> output(256);
    for (;;) {
        const auto size = writer(output);
        if (size <= output.size()) { output.resize(size); return js::string(ctx, {output.data(), output.size()}); }
        output.resize(size);
    }
}

JSValue queryValue(JSContext* ctx, JSValue current, std::string_view path) {
    size_t begin{};
    while (begin < path.size()) {
        const auto dot = path.find('.', begin);
        const auto part = path.substr(begin, dot == path.npos ? path.size() - begin : dot - begin);
        JSValue next{};
        uint32_t index{};
        const auto [ptr, ec] = std::from_chars(part.data(), part.data() + part.size(), index);
        if (ec == std::errc{} && ptr == part.data() + part.size() && JS_IsArray(current)) next = JS_GetPropertyUint32(ctx, current, index);
        else next = JS_GetPropertyStr(ctx, current, std::string{part}.c_str());
        JS_FreeValue(ctx, current);
        current = next;
        if (JS_IsUndefined(current) || JS_IsException(current)) return current;
        if (dot == path.npos) break;
        begin = dot + 1;
    }
    return current;
}

template <class Document>
JSValue documentJson(JSContext* ctx, const Document& document) {
    JSValue text = documentText(ctx, [&document](auto output) { return document.json_to(output); });
    const auto json = js::toString(ctx, text);
    JS_FreeValue(ctx, text);
    return js::parseJson(ctx, json);
}

template <class Document>
JSValue documentQuery(JSContext* ctx, const Document& document, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_UNDEFINED;
    return queryValue(ctx, documentJson(ctx, document), js::toString(ctx, argv[0]));
}

JSValue tomlDocumentGet(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { const auto* document = tomlDocumentData(ctx, self); return document ? documentQuery(ctx, document->value, argc, argv) : JS_NULL; }
JSValue yamlDocumentGet(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { const auto* document = yamlDocumentData(ctx, self); return document ? documentQuery(ctx, document->value, argc, argv) : JS_NULL; }
JSValue tomlDocumentHas(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { JSValue value = tomlDocumentGet(ctx, self, argc, argv); const auto result = !JS_IsUndefined(value) && !JS_IsException(value); JS_FreeValue(ctx, value); return JS_NewBool(ctx, result); }
JSValue yamlDocumentHas(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { JSValue value = yamlDocumentGet(ctx, self, argc, argv); const auto result = !JS_IsUndefined(value) && !JS_IsException(value); JS_FreeValue(ctx, value); return JS_NewBool(ctx, result); }
JSValue tomlDocumentJson(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* document = tomlDocumentData(ctx, self); return document ? documentJson(ctx, document->value) : JS_NULL; }
JSValue yamlDocumentJson(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* document = yamlDocumentData(ctx, self); return document ? documentJson(ctx, document->value) : JS_NULL; }
JSValue tomlDocumentStringify(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* document = tomlDocumentData(ctx, self); return document ? documentText(ctx, [&document](auto output) { return document->value.format_to(output); }) : JS_NULL; }
JSValue yamlDocumentStringify(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* document = yamlDocumentData(ctx, self); return document ? documentText(ctx, [&document](auto output) { return document->value.format_to(output); }) : JS_NULL; }
JSValue tomlDocumentDispose(JSContext* ctx, JSValueConst self, int, JSValueConst*) { auto* document = tomlDocumentData(ctx, self); if (!document) return JS_FALSE; delete document; JS_SetOpaque(self, nullptr); return JS_TRUE; }
JSValue yamlDocumentDispose(JSContext* ctx, JSValueConst self, int, JSValueConst*) { auto* document = yamlDocumentData(ctx, self); if (!document) return JS_FALSE; delete document; JS_SetOpaque(self, nullptr); return JS_TRUE; }
JSValue tomlDocumentSet(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* document = tomlDocumentData(ctx, self); if (!document || argc < 2) return JS_FALSE; const auto path = js::toString(ctx, argv[0]); const auto value = js::toString(ctx, argv[1]); return JS_NewBool(ctx, document->value.find({path.data(), path.size()}).set_text({value.data(), value.size()})); }
JSValue yamlDocumentSet(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* document = yamlDocumentData(ctx, self); if (!document || argc < 2) return JS_FALSE;
    const auto path = js::toString(ctx, argv[0]); const auto value = js::toString(ctx, argv[1]); auto node = document->value.root(); size_t begin{};
    while (node && begin < path.size()) { const auto dot = path.find('.', begin); const auto part = std::string_view{path}.substr(begin, dot == path.npos ? path.size() - begin : dot - begin); uint32_t index{}; const auto [ptr, ec] = std::from_chars(part.data(), part.data() + part.size(), index); node = ec == std::errc{} && ptr == part.data() + part.size() ? node.at(index) : node.get({part.data(), part.size()}); if (dot == path.npos) break; begin = dot + 1; }
    return JS_NewBool(ctx, node && node.set_value({value.data(), value.size()}));
}
JSValue yamlDocumentAppend(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* document = yamlDocumentData(ctx, self); if (!document || argc < 3) return JS_FALSE;
    const auto path = js::toString(ctx, argv[0]); const auto key = js::toString(ctx, argv[1]); const auto value = js::toString(ctx, argv[2]); auto node = document->value.root();
    for (size_t begin{}; node && begin < path.size();) { const auto dot = path.find('.', begin); const auto part = std::string_view{path}.substr(begin, dot == path.npos ? path.size() - begin : dot - begin); node = node.get({part.data(), part.size()}); if (dot == path.npos) break; begin = dot + 1; }
    return JS_NewBool(ctx, node && node.append({key.data(), key.size()}, {value.data(), value.size()}));
}
JSValue createTomlDocument(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL; const auto input = js::toString(ctx, argv[0]); auto value = tomlplusplus::Document{{input.data(), input.size()}}; if (!value) return JS_NULL;
    JSValue object = JS_NewObjectClass(ctx, tomlDocumentClass); if (JS_IsException(object)) return object; JS_SetOpaque(object, new TomlDocumentObject{std::move(value)});
    js::setFunction(ctx, object, "get", tomlDocumentGet, 1); js::setFunction(ctx, object, "query", tomlDocumentGet, 1); js::setFunction(ctx, object, "has", tomlDocumentHas, 1); js::setFunction(ctx, object, "set", tomlDocumentSet, 2); js::setFunction(ctx, object, "toJson", tomlDocumentJson, 0); js::setFunction(ctx, object, "stringify", tomlDocumentStringify, 0); js::setFunction(ctx, object, "dispose", tomlDocumentDispose, 0); return object;
}

JSValue createYamlDocument(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL; const auto input = js::toString(ctx, argv[0]); auto value = rapidyaml::Document{{input.data(), input.size()}}; if (!value) return JS_NULL;
    JSValue object = JS_NewObjectClass(ctx, yamlDocumentClass); if (JS_IsException(object)) return object; JS_SetOpaque(object, new YamlDocumentObject{std::move(value)});
    js::setFunction(ctx, object, "get", yamlDocumentGet, 1); js::setFunction(ctx, object, "query", yamlDocumentGet, 1); js::setFunction(ctx, object, "has", yamlDocumentHas, 1); js::setFunction(ctx, object, "set", yamlDocumentSet, 2); js::setFunction(ctx, object, "append", yamlDocumentAppend, 3); js::setFunction(ctx, object, "toJson", yamlDocumentJson, 0); js::setFunction(ctx, object, "stringify", yamlDocumentStringify, 0); js::setFunction(ctx, object, "dispose", yamlDocumentDispose, 0); return object;
}

void initializeDocuments(JSContext* ctx) { auto* runtime = JS_GetRuntime(ctx); if (!tomlDocumentClass) { JS_NewClassID(runtime, &tomlDocumentClass); JS_NewClass(runtime, tomlDocumentClass, &tomlDocumentClassDef); } if (!yamlDocumentClass) { JS_NewClassID(runtime, &yamlDocumentClass); JS_NewClass(runtime, yamlDocumentClass, &yamlDocumentClassDef); } }

JSValue jsonValue(JSContext* ctx, json::Value value) {
    if (!value) return JS_UNDEFINED;
    if (value.is_null()) return JS_NULL;
    if (value.is_bool()) return JS_NewBool(ctx, value.get_bool());
    if (value.is_uint()) return JS_NewInt64(ctx, static_cast<int64_t>(value.get_uint()));
    if (value.is_int()) return JS_NewInt64(ctx, value.get_sint());
    if (value.is_real()) return JS_NewFloat64(ctx, value.get_real());
    if (value.is_str()) return js::string(ctx, value.get_str_view());
    if (value.is_arr()) {
        JSValue arr = JS_NewArray(ctx);
        uint32_t i = 0;
        for (auto item : value.array()) JS_SetPropertyUint32(ctx, arr, i++, jsonValue(ctx, item));
        return arr;
    }
    if (value.is_obj()) {
        JSValue obj = JS_NewObject(ctx);
        for (auto [key, val] : value.object()) {
            const auto name = std::string(key.get_str_view());
            JS_SetPropertyStr(ctx, obj, name.c_str(), jsonValue(ctx, val));
        }
        return obj;
    }
    return JS_UNDEFINED;
}

const char* jsonType(json::Value v) {
    if (!v) return "undefined";
    if (v.is_null()) return "null";
    if (v.is_bool()) return "bool";
    if (v.is_str()) return "string";
    if (v.is_arr()) return "array";
    if (v.is_obj()) return "object";
    if (v.is_int()) return "integer";
    if (v.is_real()) return "real";
    if (v.is_num()) return "number";
    return "unknown";
}

json::Value query(json::Value root, std::string_view path) {
    auto cur = root;
    size_t begin = 0;
    while (cur && begin < path.size()) {
        const auto dot = path.find('.', begin);
        const auto part = path.substr(begin, dot == std::string_view::npos ? path.size() - begin : dot - begin);
        if (cur.is_obj()) cur = cur.obj_get(part);
        else if (cur.is_arr()) {
            size_t idx = 0;
            for (char c : part) {
                if (c < '0' || c > '9') return {};
                idx = idx * 10 + static_cast<size_t>(c - '0');
            }
            cur = cur.arr_get(idx);
        } else return {};
        if (dot == std::string_view::npos) break;
        begin = dot + 1;
    }
    return cur;
}

JSValue parseJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    auto flags = json::ReadFlag::None;
    if (argc > 1 && JS_IsObject(argv[1])) {
        JSValue comments = JS_GetPropertyStr(ctx, argv[1], "comments");
        JSValue trailing = JS_GetPropertyStr(ctx, argv[1], "trailingCommas");
        if (boolArg(ctx, comments, false)) flags = flags | json::ReadFlag::AllowComments;
        if (boolArg(ctx, trailing, false)) flags = flags | json::ReadFlag::AllowTrailingCommas;
        JS_FreeValue(ctx, comments);
        JS_FreeValue(ctx, trailing);
    }
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text, flags);
    return doc ? jsonValue(ctx, doc.root()) : JS_NULL;
}

JSValue stringifyJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    return argc > 0 ? js::string(ctx, js::stringify(ctx, argv[0], "null")) : JS_NewString(ctx, "null");
}

JSValue validJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_FALSE;
    const auto text = js::toString(ctx, argv[0]);
    return JS_NewBool(ctx, static_cast<bool>(json::Document::parse(text)));
}

JSValue typeJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewString(ctx, "undefined");
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    return doc ? JS_NewString(ctx, jsonType(doc.root())) : JS_NewString(ctx, "invalid");
}

JSValue getJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_UNDEFINED;
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    if (!doc) return JS_UNDEFINED;
    return jsonValue(ctx, query(doc.root(), js::toString(ctx, argv[1])));
}

JSValue keysJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewArray(ctx);
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    if (!doc) return JS_NewArray(ctx);
    auto root = argc > 1 ? query(doc.root(), js::toString(ctx, argv[1])) : doc.root();
    JSValue arr = JS_NewArray(ctx);
    if (!root.is_obj()) return arr;
    uint32_t i = 0;
    for (auto [key, val] : root.object()) JS_SetPropertyUint32(ctx, arr, i++, js::string(ctx, key.get_str_view()));
    return arr;
}

JSValue valuesJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewArray(ctx);
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    if (!doc) return JS_NewArray(ctx);
    auto root = argc > 1 ? query(doc.root(), js::toString(ctx, argv[1])) : doc.root();
    JSValue arr = JS_NewArray(ctx);
    uint32_t i = 0;
    if (root.is_obj()) for (auto [key, val] : root.object()) JS_SetPropertyUint32(ctx, arr, i++, jsonValue(ctx, val));
    else if (root.is_arr()) for (auto val : root.array()) JS_SetPropertyUint32(ctx, arr, i++, jsonValue(ctx, val));
    return arr;
}

JSValue entriesJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewArray(ctx);
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    if (!doc) return JS_NewArray(ctx);
    auto root = argc > 1 ? query(doc.root(), js::toString(ctx, argv[1])) : doc.root();
    JSValue arr = JS_NewArray(ctx);
    uint32_t i = 0;
    if (root.is_obj()) {
        for (auto [key, val] : root.object()) {
            JSValue entry = JS_NewArray(ctx);
            JS_SetPropertyUint32(ctx, entry, 0, js::string(ctx, key.get_str_view()));
            JS_SetPropertyUint32(ctx, entry, 1, jsonValue(ctx, val));
            JS_SetPropertyUint32(ctx, arr, i++, entry);
        }
    } else if (root.is_arr()) {
        for (auto val : root.array()) {
            JSValue entry = JS_NewArray(ctx);
            JS_SetPropertyUint32(ctx, entry, 0, JS_NewUint32(ctx, i));
            JS_SetPropertyUint32(ctx, entry, 1, jsonValue(ctx, val));
            JS_SetPropertyUint32(ctx, arr, i++, entry);
        }
    }
    return arr;
}

JSValue hasJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_FALSE;
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    return JS_NewBool(ctx, doc && static_cast<bool>(query(doc.root(), js::toString(ctx, argv[1]))));
}

JSValue sizeJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewInt32(ctx, 0);
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    if (!doc) return JS_NewInt32(ctx, 0);
    auto root = argc > 1 ? query(doc.root(), js::toString(ctx, argv[1])) : doc.root();
    return JS_NewInt64(ctx, root.is_arr() ? static_cast<int64_t>(root.arr_size()) : root.is_obj() ? static_cast<int64_t>(root.obj_size()) : 0);
}

JSValue rawJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewString(ctx, "");
    const auto text = js::toString(ctx, argv[0]);
    auto doc = json::Document::parse(text);
    if (!doc) return JS_NewString(ctx, "");
    auto root = argc > 1 ? query(doc.root(), js::toString(ctx, argv[1])) : doc.root();
    JSValue value = jsonValue(ctx, root);
    if (JS_IsUndefined(value)) return JS_NewString(ctx, "");
    auto out = js::stringify(ctx, value, "null");
    JS_FreeValue(ctx, value);
    return js::string(ctx, out);
}

JSValue readJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    try { auto text = FileUtils::readTextFile(js::toString(ctx, argv[0])); JSValue arg = js::string(ctx, text); JSValue out = parseJson(ctx, JS_UNDEFINED, 1, &arg); JS_FreeValue(ctx, arg); return out; }
    catch (...) { return JS_NULL; }
}

JSValue writeJson(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_FALSE;
    return JS_NewBool(ctx, FileUtils::writeTextFile(js::toString(ctx, argv[0]), js::stringify(ctx, argv[1], "null")));
}

JSValue xmlNode(JSContext* ctx, xml::Node node);

JSValue xmlAttr(JSContext* ctx, xml::Attribute attr) {
    JSValue obj = JS_NewObject(ctx);
    JS_SetPropertyStr(ctx, obj, "name", js::string(ctx, attr.name()));
    JS_SetPropertyStr(ctx, obj, "value", js::string(ctx, attr.value()));
    return obj;
}

JSValue xmlNode(JSContext* ctx, xml::Node node) {
    if (!node) return JS_NULL;
    JSValue obj = JS_NewObject(ctx);
    JS_SetPropertyStr(ctx, obj, "name", js::string(ctx, node.name()));
    JS_SetPropertyStr(ctx, obj, "value", js::string(ctx, node.value()));
    JS_SetPropertyStr(ctx, obj, "text", js::string(ctx, node.text()));
    JS_SetPropertyStr(ctx, obj, "type", JS_NewInt32(ctx, static_cast<int>(node.type())));
    JSValue attrs = JS_NewArray(ctx);
    uint32_t ai = 0;
    for (auto attr : node.attributes()) JS_SetPropertyUint32(ctx, attrs, ai++, xmlAttr(ctx, attr));
    JS_SetPropertyStr(ctx, obj, "attributes", attrs);
    JSValue children = JS_NewArray(ctx);
    uint32_t ci = 0;
    for (auto child : node.children()) JS_SetPropertyUint32(ctx, children, ci++, xmlNode(ctx, child));
    JS_SetPropertyStr(ctx, obj, "children", children);
    return obj;
}

xml::Node xmlFind(xml::Node root, std::string_view path) {
    auto cur = root;
    size_t begin = 0;
    while (cur && begin < path.size()) {
        const auto dot = path.find('.', begin);
        const auto part = path.substr(begin, dot == std::string_view::npos ? path.size() - begin : dot - begin);
        cur = cur.child(part);
        if (dot == std::string_view::npos) break;
        begin = dot + 1;
    }
    return cur;
}

JSValue parseXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    const auto text = js::toString(ctx, argv[0]);
    auto parsed = xml::Document::parse(text);
    return parsed ? xmlNode(ctx, parsed->root()) : JS_NULL;
}

JSValue formatXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewString(ctx, "");
    const auto text = js::toString(ctx, argv[0]);
    auto parsed = xml::Document::parse(text);
    return parsed ? js::string(ctx, parsed->write(argc > 1 ? js::toString(ctx, argv[1]) : "  ")) : JS_NewString(ctx, "");
}

JSValue getXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_NULL;
    const auto text = js::toString(ctx, argv[0]);
    auto parsed = xml::Document::parse(text);
    return parsed ? xmlNode(ctx, xmlFind(parsed->root(), js::toString(ctx, argv[1]))) : JS_NULL;
}

JSValue textXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_NewString(ctx, "");
    const auto text = js::toString(ctx, argv[0]);
    auto parsed = xml::Document::parse(text);
    if (!parsed) return JS_NewString(ctx, "");
    auto node = xmlFind(parsed->root(), js::toString(ctx, argv[1]));
    return node ? js::string(ctx, node.text()) : JS_NewString(ctx, "");
}

JSValue attrXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_NewString(ctx, "");
    const auto text = js::toString(ctx, argv[0]);
    auto parsed = xml::Document::parse(text);
    if (!parsed) return JS_NewString(ctx, "");
    auto node = xmlFind(parsed->root(), js::toString(ctx, argv[1]));
    if (!node) return JS_NewString(ctx, "");
    auto attr = node.attribute(js::toString(ctx, argv[2]));
    return attr ? js::string(ctx, attr.value()) : JS_NewString(ctx, "");
}

JSValue validXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_FALSE;
    const auto text = js::toString(ctx, argv[0]);
    return JS_NewBool(ctx, xml::Document::parse(text).has_value());
}

JSValue readXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    const auto path = js::toString(ctx, argv[0]);
    auto parsed = xml::Document::load_file(path.c_str());
    return parsed ? xmlNode(ctx, parsed->root()) : JS_NULL;
}

JSValue writeXml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_FALSE;
    return JS_NewBool(ctx, FileUtils::writeTextFile(js::toString(ctx, argv[0]), js::toString(ctx, argv[1])));
}

template <class Formatter>
JSValue formatText(JSContext* ctx, int argc, JSValueConst* argv, Formatter formatter) {
    if (argc < 1) return JS_NewString(ctx, "");
    const auto input = js::toString(ctx, argv[0]);
    const auto output = formatter({input.data(), input.size()});
    return output.empty() && !input.empty() ? JS_NULL : js::string(ctx, {output.data(), output.size()});
}

JSValue parseToml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return formatText(ctx, argc, argv, tomlplusplus::format); }
JSValue validToml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { if (argc < 1) return JS_FALSE; const auto input = js::toString(ctx, argv[0]); return JS_NewBool(ctx, tomlplusplus::valid({input.data(), input.size()})); }
template <class Converter>
JSValue parseStructuredText(JSContext* ctx, int argc, JSValueConst* argv, Converter converter) {
    if (argc < 1) return JS_NULL;
    const auto input = js::toString(ctx, argv[0]);
    const auto json = converter({input.data(), input.size()});
    return json.empty() && !input.empty() ? JS_NULL : js::parseJson(ctx, {json.data(), json.size()});
}

JSValue parseTomlObject(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return parseStructuredText(ctx, argc, argv, tomlplusplus::json); }
JSValue parseYaml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return parseStructuredText(ctx, argc, argv, rapidyaml::json); }
JSValue validYaml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { if (argc < 1) return JS_FALSE; const auto input = js::toString(ctx, argv[0]); return JS_NewBool(ctx, rapidyaml::valid({input.data(), input.size()})); }
JSValue stringifyYaml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewString(ctx, "");
    const auto json = js::stringify(ctx, argv[0], "null");
    const auto yaml = rapidyaml::from_json({json.data(), json.size()});
    return yaml.empty() && !json.empty() ? JS_NULL : js::string(ctx, {yaml.data(), yaml.size()});
}

template <class Parser>
JSValue queryStructured(JSContext* ctx, int argc, JSValueConst* argv, Parser parser) {
    if (argc < 2) return JS_UNDEFINED;
    JSValue current = parser(ctx, JS_UNDEFINED, 1, argv);
    if (JS_IsNull(current) || JS_IsException(current)) return current;
    const auto path = js::toString(ctx, argv[1]);
    size_t begin{};
    while (begin < path.size()) {
        const auto dot = path.find('.', begin);
        const auto part = std::string_view{path}.substr(begin, dot == std::string::npos ? path.size() - begin : dot - begin);
        JSValue next{};
        uint32_t index{};
        const auto [ptr, ec] = std::from_chars(part.data(), part.data() + part.size(), index);
        if (ec == std::errc{} && ptr == part.data() + part.size() && JS_IsArray(current)) next = JS_GetPropertyUint32(ctx, current, index);
        else next = JS_GetPropertyStr(ctx, current, std::string{part}.c_str());
        JS_FreeValue(ctx, current);
        current = next;
        if (JS_IsUndefined(current) || JS_IsException(current)) return current;
        if (dot == std::string::npos) break;
        begin = dot + 1;
    }
    return current;
}

JSValue getToml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return queryStructured(ctx, argc, argv, parseTomlObject); }
JSValue getYaml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return queryStructured(ctx, argc, argv, parseYaml); }
JSValue hasToml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { JSValue value = getToml(ctx, JS_UNDEFINED, argc, argv); const auto result = !JS_IsUndefined(value) && !JS_IsException(value); JS_FreeValue(ctx, value); return JS_NewBool(ctx, result); }
JSValue hasYaml(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { JSValue value = getYaml(ctx, JS_UNDEFINED, argc, argv); const auto result = !JS_IsUndefined(value) && !JS_IsException(value); JS_FreeValue(ctx, value); return JS_NewBool(ctx, result); }

}

JSValue createJson(JSContext* ctx) {
    return object(ctx, {
        {"parse", parseJson, 2}, {"stringify", stringifyJson, 1}, {"valid", validJson, 1}, {"type", typeJson, 1},
        {"get", getJson, 2}, {"query", getJson, 2}, {"has", hasJson, 2}, {"keys", keysJson, 2}, {"values", valuesJson, 2}, {"entries", entriesJson, 2}, {"size", sizeJson, 2}, {"raw", rawJson, 2},
        {"read", readJson, 1}, {"write", writeJson, 2},
    });
}

JSValue createXml(JSContext* ctx) {
    return object(ctx, {
        {"parse", parseXml, 1}, {"format", formatXml, 2}, {"stringify", formatXml, 2}, {"valid", validXml, 1},
        {"get", getXml, 2}, {"query", getXml, 2}, {"text", textXml, 2}, {"attr", attrXml, 3}, {"read", readXml, 1}, {"write", writeXml, 2},
    });
}

JSValue createTomlplusplus(JSContext* ctx) { initializeDocuments(ctx); return object(ctx, {{"document", createTomlDocument, 1}, {"parseDocument", createTomlDocument, 1}, {"parse", parseTomlObject, 1}, {"toJson", parseTomlObject, 1}, {"format", parseToml, 1}, {"valid", validToml, 1}, {"get", getToml, 2}, {"query", getToml, 2}, {"has", hasToml, 2}}); }
JSValue createRapidyaml(JSContext* ctx) { initializeDocuments(ctx); return object(ctx, {{"document", createYamlDocument, 1}, {"parseDocument", createYamlDocument, 1}, {"parse", parseYaml, 1}, {"toJson", parseYaml, 1}, {"format", parseYaml, 1}, {"stringify", stringifyYaml, 1}, {"fromJson", stringifyYaml, 1}, {"valid", validYaml, 1}, {"get", getYaml, 2}, {"query", getYaml, 2}, {"has", hasYaml, 2}}); }
}
