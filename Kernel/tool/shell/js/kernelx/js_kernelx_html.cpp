module;
#include <cstdint>
#include <optional>
#include <string>
#include <utility>
#include <vector>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.serialization.lexbor.lexbor_core;
import utility.serialization.lexbor.lexbor_html;

namespace kernelx {

bool boolArg(JSContext* ctx, JSValueConst value, bool fallback) noexcept;
int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;

namespace {

JSClassID documentClass{};
JSClassID nodeClass{};

struct Document { lexbor::Document value; };
struct Node { void* value{}; JSValue document{JS_UNDEFINED}; };

void documentFinalizer(JSRuntime*, JSValueConst value) { delete static_cast<Document*>(JS_GetOpaque(value, documentClass)); }
void nodeFinalizer(JSRuntime* runtime, JSValueConst value) { if (auto* node = static_cast<Node*>(JS_GetOpaque(value, nodeClass))) { JS_FreeValueRT(runtime, node->document); delete node; } }
const JSClassDef documentClassDef{"LexborDocument", documentFinalizer, nullptr, nullptr, nullptr};
const JSClassDef nodeClassDef{"LexborNode", nodeFinalizer, nullptr, nullptr, nullptr};

Node* nodeData(JSContext* ctx, JSValueConst value) { return static_cast<Node*>(JS_GetOpaque2(ctx, value, nodeClass)); }
Document* documentData(JSContext* ctx, JSValueConst value) { return static_cast<Document*>(JS_GetOpaque2(ctx, value, documentClass)); }
JSValue nodeObject(JSContext* ctx, JSValueConst document, void* value);
JSValue nodeQuery(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv);

JSValue nodeName(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* node = nodeData(ctx, self); const auto value = node ? lexbor::name(node->value) : lexbor::view_type{}; return JS_NewStringLen(ctx, reinterpret_cast<const char*>(value.data()), value.size()); }
JSValue nodeText(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* node = nodeData(ctx, self); const auto value = node ? lexbor::text(node->value) : lexbor::view_type{}; return JS_NewStringLen(ctx, reinterpret_cast<const char*>(value.data()), value.size()); }
JSValue nodeType(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* node = nodeData(ctx, self); return JS_NewInt32(ctx, node ? lexbor::type(node->value) : 0); }
JSValue related(JSContext* ctx, JSValueConst self, void* (*get)(void*) noexcept) { const auto* node = nodeData(ctx, self); const auto value = node ? get(node->value) : nullptr; return value ? nodeObject(ctx, node->document, value) : JS_NULL; }
JSValue nodeParent(JSContext* ctx, JSValueConst self, int, JSValueConst*) { return related(ctx, self, lexbor::parent); }
JSValue nodeFirstChild(JSContext* ctx, JSValueConst self, int, JSValueConst*) { return related(ctx, self, lexbor::first_child); }
JSValue nodeLastChild(JSContext* ctx, JSValueConst self, int, JSValueConst*) { return related(ctx, self, lexbor::last_child); }
JSValue nodeNext(JSContext* ctx, JSValueConst self, int, JSValueConst*) { return related(ctx, self, lexbor::next); }
JSValue nodePrevious(JSContext* ctx, JSValueConst self, int, JSValueConst*) { return related(ctx, self, lexbor::previous); }
JSValue nodeSetText(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* node = nodeData(ctx, self); auto input = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; return JS_NewBool(ctx, node && input && lexbor::set_text(node->value, input.span()) == lexbor::status_ok); }
JSValue nodeAttribute(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* node = nodeData(ctx, self); auto key = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; const auto value = node && key ? lexbor::attribute(node->value, key.span()) : lexbor::view_type{}; return value.data() ? JS_NewStringLen(ctx, reinterpret_cast<const char*>(value.data()), value.size()) : JS_NULL; }
JSValue nodeSetAttribute(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* node = nodeData(ctx, self); auto key = argc > 0 ? js::bytesView(ctx, argv[0]) : js::BytesView{}; auto value = argc > 1 ? js::bytesView(ctx, argv[1]) : js::BytesView{}; return JS_NewBool(ctx, node && key && value && lexbor::set_attribute(node->value, key.span(), value.span()) == lexbor::status_ok); }
JSValue nodeRemoveAttribute(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* node = nodeData(ctx, self); auto key = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; return JS_NewBool(ctx, node && key && lexbor::remove_attribute(node->value, key.span()) == lexbor::status_ok); }
JSValue nodeInnerHtml(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* node = nodeData(ctx, self); if (!node) return JS_NULL; if (argc) { auto input = js::bytesView(ctx, argv[0]); return JS_NewBool(ctx, input && lexbor::set_inner_html(node->value, input.span()) == lexbor::status_ok); } auto output = lexbor::serialize(node->value, false); return output ? js::bytes(ctx, std::move(*output)) : JS_NULL; }
JSValue nodeClone(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { const auto* node = nodeData(ctx, self); const auto value = node ? lexbor::clone(node->value, argc == 0 || boolArg(ctx, argv[0], true)) : nullptr; return value ? nodeObject(ctx, node->document, value) : JS_NULL; }
JSValue nodeAppend(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* parent = nodeData(ctx, self); auto* child = argc ? nodeData(ctx, argv[0]) : nullptr; return JS_NewBool(ctx, parent && child && lexbor::append(parent->value, child->value) == lexbor::status_ok); }
JSValue nodeReplace(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* old = nodeData(ctx, self); auto* replacement = argc ? nodeData(ctx, argv[0]) : nullptr; return JS_NewBool(ctx, old && replacement && lexbor::replace(lexbor::parent(old->value), replacement->value, old->value) == lexbor::status_ok); }
JSValue nodeRemove(JSContext* ctx, JSValueConst self, int, JSValueConst*) { auto* node = nodeData(ctx, self); return JS_NewBool(ctx, node && lexbor::remove(lexbor::parent(node->value), node->value) == lexbor::status_ok); }
JSValue nodeHtml(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { const auto* node = nodeData(ctx, self); auto output = node ? lexbor::serialize(node->value, argc == 0 || boolArg(ctx, argv[0], true), argc > 1 && boolArg(ctx, argv[1], false), argc > 2 ? static_cast<size_t>(intArg(ctx, argv[2], 2)) : 2) : std::nullopt; return output ? js::bytes(ctx, std::move(*output)) : JS_NULL; }

JSValue nodeObject(JSContext* ctx, JSValueConst document, void* value) {
    if (!value) return JS_NULL;
    JSValue object = JS_NewObjectClass(ctx, nodeClass);
    if (JS_IsException(object)) return object;
    JS_SetOpaque(object, new Node{value, JS_DupValue(ctx, document)});
    js::setFunction(ctx, object, "name", nodeName, 0); js::setFunction(ctx, object, "text", nodeText, 0); js::setFunction(ctx, object, "type", nodeType, 0);
    js::setFunction(ctx, object, "parent", nodeParent, 0); js::setFunction(ctx, object, "firstChild", nodeFirstChild, 0); js::setFunction(ctx, object, "lastChild", nodeLastChild, 0); js::setFunction(ctx, object, "next", nodeNext, 0); js::setFunction(ctx, object, "previous", nodePrevious, 0);
    js::setFunction(ctx, object, "setText", nodeSetText, 1); js::setFunction(ctx, object, "attribute", nodeAttribute, 1); js::setFunction(ctx, object, "setAttribute", nodeSetAttribute, 2); js::setFunction(ctx, object, "removeAttribute", nodeRemoveAttribute, 1); js::setFunction(ctx, object, "innerHtml", nodeInnerHtml, 1); js::setFunction(ctx, object, "clone", nodeClone, 1); js::setFunction(ctx, object, "append", nodeAppend, 1); js::setFunction(ctx, object, "replace", nodeReplace, 1); js::setFunction(ctx, object, "remove", nodeRemove, 0); js::setFunction(ctx, object, "query", nodeQuery, 1); js::setFunction(ctx, object, "html", nodeHtml, 3);
    return object;
}

struct Query { JSContext* context; JSValueConst document; JSValue result; uint32_t index{}; };
int queryCallback(void* value, void* context) noexcept { auto& query = *static_cast<Query*>(context); JS_SetPropertyUint32(query.context, query.result, query.index++, nodeObject(query.context, query.document, value)); return 0; }
JSValue query(JSContext* ctx, JSValueConst document, void* root, JSValueConst selector) { const auto text = js::toString(ctx, selector); JSValue result = JS_NewArray(ctx); Query state{ctx, document, result}; lexbor::query(root, {reinterpret_cast<const uint8_t*>(text.data()), text.size()}, queryCallback, &state); return result; }
JSValue nodeQuery(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { const auto* node = nodeData(ctx, self); return node && argc ? query(ctx, node->document, node->value, argv[0]) : JS_NewArray(ctx); }

JSValue documentRoot(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* document = documentData(ctx, self); return document ? nodeObject(ctx, self, document->value.node()) : JS_NULL; }
JSValue documentElement(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* document = documentData(ctx, self); auto name = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; return document && name ? nodeObject(ctx, self, document->value.create_element(name.span())) : JS_NULL; }
JSValue documentText(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* document = documentData(ctx, self); auto text = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; return document && text ? nodeObject(ctx, self, document->value.create_text(text.span())) : JS_NULL; }
JSValue documentQuery(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* document = documentData(ctx, self); return document && argc ? query(ctx, self, document->value.node(), argv[0]) : JS_NewArray(ctx); }
JSValue documentHtml(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { const auto* document = documentData(ctx, self); auto output = document ? lexbor::serialize(document->value.node(), true, argc && boolArg(ctx, argv[0], false), argc > 1 ? static_cast<size_t>(intArg(ctx, argv[1], 2)) : 2) : std::nullopt; return output ? js::bytes(ctx, std::move(*output)) : JS_NULL; }
JSValue documentBegin(JSContext* ctx, JSValueConst self, int, JSValueConst*) { auto* document = documentData(ctx, self); return JS_NewBool(ctx, document && document->value.begin() == lexbor::status_ok); }
JSValue documentWrite(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* document = documentData(ctx, self); auto input = argc ? js::bytesView(ctx, argv[0]) : js::BytesView{}; return JS_NewBool(ctx, document && input && document->value.append(input.span()) == lexbor::status_ok); }
JSValue documentEnd(JSContext* ctx, JSValueConst self, int, JSValueConst*) { auto* document = documentData(ctx, self); return JS_NewBool(ctx, document && document->value.end() == lexbor::status_ok); }

JSValue createDocument(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    JSValue object = JS_NewObjectClass(ctx, documentClass);
    if (JS_IsException(object)) return object;
    JS_SetOpaque(object, new Document{});
    js::setFunction(ctx, object, "root", documentRoot, 0); js::setFunction(ctx, object, "createElement", documentElement, 1); js::setFunction(ctx, object, "createText", documentText, 1); js::setFunction(ctx, object, "query", documentQuery, 1); js::setFunction(ctx, object, "html", documentHtml, 2); js::setFunction(ctx, object, "begin", documentBegin, 0); js::setFunction(ctx, object, "write", documentWrite, 1); js::setFunction(ctx, object, "end", documentEnd, 0);
    if (!argc) return object;
    auto input = js::bytesView(ctx, argv[0]);
    auto* document = documentData(ctx, object);
    if (!input || !document || document->value.parse(input.span()) != lexbor::status_ok) { JS_FreeValue(ctx, object); return JS_NULL; }
    return object;
}

JSValue parse(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { if (!argc) return JS_NULL; auto document = createDocument(ctx, JS_UNDEFINED, argc, argv); if (JS_IsNull(document)) return JS_NULL; auto* data = documentData(ctx, document); auto output = data ? lexbor::serialize(data->value.node()) : std::nullopt; JS_FreeValue(ctx, document); return output ? js::bytes(ctx, std::move(*output)) : JS_NULL; }
JSValue valid(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { auto document = argc ? createDocument(ctx, JS_UNDEFINED, argc, argv) : JS_NULL; const auto result = !JS_IsNull(document); if (result) JS_FreeValue(ctx, document); return JS_NewBool(ctx, result); }
JSValue loaded(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewBool(ctx, lexbor::loaded()); }
JSValue version(JSContext* ctx, JSValueConst, int, JSValueConst*) { return JS_NewUint32(ctx, lexbor::abi_version()); }
JSValue error(JSContext* ctx, JSValueConst, int, JSValueConst*) { return js::string(ctx, lexbor::load_error()); }

void initialize(JSContext* ctx) { auto* runtime = JS_GetRuntime(ctx); if (!documentClass) { JS_NewClassID(runtime, &documentClass); JS_NewClass(runtime, documentClass, &documentClassDef); } if (!nodeClass) { JS_NewClassID(runtime, &nodeClass); JS_NewClass(runtime, nodeClass, &nodeClassDef); } }

}

JSValue createLexbor(JSContext* ctx) {
    initialize(ctx);
    JSValue api = JS_NewObject(ctx);
    js::setFunction(ctx, api, "document", createDocument, 1); js::setFunction(ctx, api, "parseDocument", createDocument, 1); js::setFunction(ctx, api, "parse", parse, 1); js::setFunction(ctx, api, "serialize", parse, 1); js::setFunction(ctx, api, "valid", valid, 1); js::setFunction(ctx, api, "loaded", loaded, 0); js::setFunction(ctx, api, "abiVersion", version, 0); js::setFunction(ctx, api, "error", error, 0);
    return api;
}

}
