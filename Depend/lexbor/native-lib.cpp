#include <cstddef>
#include <cstdint>
#include "lexbor/html/html.h"
#include "lexbor/html/interfaces/element.h"
#include "lexbor/selectors/selectors.h"
#include "lexbor/css/parser.h"
#include "lexbor/css/selectors/selectors.h"

#if defined(__GNUC__)
#define KLEXBOR_API extern "C" __attribute__((visibility("default")))
#else
#define KLEXBOR_API extern "C"
#endif

using serialize_callback = int (*)(const uint8_t*, size_t, void*);
using node_callback = int (*)(void*, void*);

namespace {

struct serialize_context {
    serialize_callback callback;
    void* user;
};

lxb_status_t serialize_with_context(const lxb_char_t* data, size_t size, void* context) {
    const auto state = static_cast<serialize_context*>(context);
    return state->callback(reinterpret_cast<const uint8_t*>(data), size, state->user) == 0 ? LXB_STATUS_OK : LXB_STATUS_ERROR;
}

struct query_context { node_callback callback; void* user; };
lxb_status_t query_with_context(lxb_dom_node_t* node, lxb_css_selector_specificity_t, void* context) {
    const auto state = static_cast<query_context*>(context);
    return state->callback(node, state->user) == 0 ? LXB_STATUS_OK : LXB_STATUS_ERROR;
}

}

KLEXBOR_API uint32_t klexbor_abi_version() noexcept { return 1; }
KLEXBOR_API void* klexbor_document_create() noexcept { return lxb_html_document_create(); }
KLEXBOR_API void klexbor_document_destroy(void* document) noexcept { if (document) lxb_html_document_destroy(static_cast<lxb_html_document_t*>(document)); }
KLEXBOR_API int32_t klexbor_document_parse(void* document, const uint8_t* html, size_t size) noexcept { return document && (html || size == 0) ? static_cast<int32_t>(lxb_html_document_parse(static_cast<lxb_html_document_t*>(document), reinterpret_cast<const lxb_char_t*>(html), size)) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API int32_t klexbor_document_parse_chunk_begin(void* document) noexcept { return document ? static_cast<int32_t>(lxb_html_document_parse_chunk_begin(static_cast<lxb_html_document_t*>(document))) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API int32_t klexbor_document_parse_chunk(void* document, const uint8_t* html, size_t size) noexcept { return document && (html || size == 0) ? static_cast<int32_t>(lxb_html_document_parse_chunk(static_cast<lxb_html_document_t*>(document), reinterpret_cast<const lxb_char_t*>(html), size)) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API int32_t klexbor_document_parse_chunk_end(void* document) noexcept { return document ? static_cast<int32_t>(lxb_html_document_parse_chunk_end(static_cast<lxb_html_document_t*>(document))) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API void* klexbor_document_node(void* document) noexcept { return document ? &static_cast<lxb_html_document_t*>(document)->dom_document.node : nullptr; }
KLEXBOR_API void* klexbor_node_first_child(void* node) noexcept { return node ? lxb_dom_node_first_child(static_cast<lxb_dom_node_t*>(node)) : nullptr; }
KLEXBOR_API void* klexbor_node_next(void* node) noexcept { return node ? lxb_dom_node_next(static_cast<lxb_dom_node_t*>(node)) : nullptr; }
KLEXBOR_API void* klexbor_node_parent(void* node) noexcept { return node ? lxb_dom_node_parent(static_cast<lxb_dom_node_t*>(node)) : nullptr; }
KLEXBOR_API void* klexbor_node_previous(void* node) noexcept { return node ? lxb_dom_node_prev(static_cast<lxb_dom_node_t*>(node)) : nullptr; }
KLEXBOR_API void* klexbor_node_last_child(void* node) noexcept { return node ? lxb_dom_node_last_child(static_cast<lxb_dom_node_t*>(node)) : nullptr; }
KLEXBOR_API const uint8_t* klexbor_node_name(void* node, size_t* size) noexcept { return node ? reinterpret_cast<const uint8_t*>(lxb_dom_node_name(static_cast<lxb_dom_node_t*>(node), size)) : nullptr; }
KLEXBOR_API const uint8_t* klexbor_node_text(void* node, size_t* size) noexcept { return node ? reinterpret_cast<const uint8_t*>(lxb_dom_node_text_content(static_cast<lxb_dom_node_t*>(node), size)) : nullptr; }
KLEXBOR_API int32_t klexbor_node_set_text(void* node, const uint8_t* text, size_t size) noexcept { return node && (text || size == 0) ? static_cast<int32_t>(lxb_dom_node_text_content_set(static_cast<lxb_dom_node_t*>(node), reinterpret_cast<const lxb_char_t*>(text), size)) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API int32_t klexbor_node_type(void* node) noexcept { return node ? static_cast<int32_t>(lxb_dom_node_type(static_cast<lxb_dom_node_t*>(node))) : 0; }
KLEXBOR_API void* klexbor_node_clone(void* node, uint32_t deep) noexcept { return node ? lxb_dom_node_clone(static_cast<lxb_dom_node_t*>(node), deep != 0) : nullptr; }
KLEXBOR_API int32_t klexbor_node_append(void* parent, void* node) noexcept { return parent && node ? static_cast<int32_t>(lxb_dom_node_append_child(static_cast<lxb_dom_node_t*>(parent), static_cast<lxb_dom_node_t*>(node))) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API int32_t klexbor_node_replace(void* parent, void* node, void* child) noexcept { return parent && node && child ? static_cast<int32_t>(lxb_dom_node_replace_child(static_cast<lxb_dom_node_t*>(parent), static_cast<lxb_dom_node_t*>(node), static_cast<lxb_dom_node_t*>(child))) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API int32_t klexbor_node_remove(void* parent, void* child) noexcept { return parent && child ? static_cast<int32_t>(lxb_dom_node_remove_child(static_cast<lxb_dom_node_t*>(parent), static_cast<lxb_dom_node_t*>(child))) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API void* klexbor_document_create_element(void* document, const uint8_t* name, size_t size) noexcept { return document && name ? lxb_html_document_create_element(static_cast<lxb_html_document_t*>(document), reinterpret_cast<const lxb_char_t*>(name), size, nullptr) : nullptr; }
KLEXBOR_API void* klexbor_document_create_text(void* document, const uint8_t* text, size_t size) noexcept { return document && (text || size == 0) ? lxb_dom_document_create_text_node(&static_cast<lxb_html_document_t*>(document)->dom_document, reinterpret_cast<const lxb_char_t*>(text), size) : nullptr; }
KLEXBOR_API int32_t klexbor_element_set_inner_html(void* element, const uint8_t* html, size_t size) noexcept { return element && (html || size == 0) && lxb_html_element_inner_html_set(static_cast<lxb_html_element_t*>(element), reinterpret_cast<const lxb_char_t*>(html), size) ? 0 : static_cast<int32_t>(LXB_STATUS_ERROR); }
KLEXBOR_API const uint8_t* klexbor_element_get_attribute(void* element, const uint8_t* name, size_t name_size, size_t* value_size) noexcept { return element && (name || name_size == 0) ? reinterpret_cast<const uint8_t*>(lxb_dom_element_get_attribute(static_cast<lxb_dom_element_t*>(element), reinterpret_cast<const lxb_char_t*>(name), name_size, value_size)) : nullptr; }
KLEXBOR_API int32_t klexbor_element_set_attribute(void* element, const uint8_t* name, size_t name_size, const uint8_t* value, size_t value_size) noexcept { return element && (name || name_size == 0) && (value || value_size == 0) && lxb_dom_element_set_attribute(static_cast<lxb_dom_element_t*>(element), reinterpret_cast<const lxb_char_t*>(name), name_size, reinterpret_cast<const lxb_char_t*>(value), value_size) ? 0 : static_cast<int32_t>(LXB_STATUS_ERROR); }
KLEXBOR_API int32_t klexbor_element_remove_attribute(void* element, const uint8_t* name, size_t name_size) noexcept { return element && (name || name_size == 0) ? static_cast<int32_t>(lxb_dom_element_remove_attribute(static_cast<lxb_dom_element_t*>(element), reinterpret_cast<const lxb_char_t*>(name), name_size)) : static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS); }
KLEXBOR_API int32_t klexbor_serialize(void* node, uint32_t tree, uint32_t pretty, size_t indent, serialize_callback callback, void* user) noexcept {
    if (!node || !callback) return static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS);
    serialize_context context{callback, user};
    const auto target = static_cast<lxb_dom_node_t*>(node);
    const auto status = pretty ? (tree ? lxb_html_serialize_pretty_tree_cb(target, 0, indent, serialize_with_context, &context) : lxb_html_serialize_pretty_cb(target, 0, indent, serialize_with_context, &context)) : (tree ? lxb_html_serialize_tree_cb(target, serialize_with_context, &context) : lxb_html_serialize_cb(target, serialize_with_context, &context));
    return static_cast<int32_t>(status);
}
KLEXBOR_API int32_t klexbor_query(void* root, const uint8_t* selector, size_t size, node_callback callback, void* user) noexcept {
    if (!root || !selector || !callback) return static_cast<int32_t>(LXB_STATUS_ERROR_WRONG_ARGS);
    auto* parser = lxb_css_parser_create();
    auto* selectors = lxb_selectors_create();
    if (!parser || !selectors || lxb_css_parser_init(parser, nullptr) != LXB_STATUS_OK || lxb_css_parser_selectors_init(parser) != LXB_STATUS_OK || lxb_selectors_init(selectors) != LXB_STATUS_OK) { if (selectors) lxb_selectors_destroy(selectors, true); if (parser) lxb_css_parser_destroy(parser, true); return static_cast<int32_t>(LXB_STATUS_ERROR_MEMORY_ALLOCATION); }
    auto* list = lxb_css_selectors_parse(parser, reinterpret_cast<const lxb_char_t*>(selector), size);
    query_context context{callback, user};
    const auto status = list ? lxb_selectors_find(selectors, static_cast<lxb_dom_node_t*>(root), list, query_with_context, &context) : LXB_STATUS_ERROR;
    lxb_selectors_destroy(selectors, true);
    lxb_css_parser_destroy(parser, true);
    return static_cast<int32_t>(status);
}
