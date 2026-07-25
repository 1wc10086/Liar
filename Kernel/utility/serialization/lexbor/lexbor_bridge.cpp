module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <string_view>
#include <utility>

module utility.serialization.lexbor.lexbor_core;

namespace lexbor {
namespace {

struct Api {
    using abi_version_t = uint32_t (*)();
    using document_create_t = void* (*)();
    using document_destroy_t = void (*)(void*);
    using document_parse_t = int32_t (*)(void*, const byte*, size_t);
    using document_stage_t = int32_t (*)(void*);
    using document_node_t = void* (*)(void*);
    using node_t = void* (*)(void*);
    using node_clone_t = void* (*)(void*, uint32_t);
    using node_binary_t = int32_t (*)(void*, void*);
    using node_ternary_t = int32_t (*)(void*, void*, void*);
    using node_type_t = int32_t (*)(void*);
    using node_view_t = const byte* (*)(void*, size_t*);
    using node_set_text_t = int32_t (*)(void*, const byte*, size_t);
    using attribute_t = const byte* (*)(void*, const byte*, size_t, size_t*);
    using set_attribute_t = int32_t (*)(void*, const byte*, size_t, const byte*, size_t);
    using remove_attribute_t = int32_t (*)(void*, const byte*, size_t);
    using serialize_t = int32_t (*)(void*, uint32_t, uint32_t, size_t, int (*)(const byte*, size_t, void*), void*);
    using create_node_t = void* (*)(void*, const byte*, size_t);
    using set_inner_html_t = int32_t (*)(void*, const byte*, size_t);
    using query_t = int32_t (*)(void*, const byte*, size_t, int (*)(void*, void*), void*);

    void* library{};
    const char* path{};
    const char* error{};
    abi_version_t abi_version{};
    document_create_t document_create{};
    document_destroy_t document_destroy{};
    document_parse_t document_parse{};
    document_stage_t document_parse_chunk_begin{};
    document_parse_t document_parse_chunk{};
    document_stage_t document_parse_chunk_end{};
    document_node_t document_node{};
    node_t first_child{};
    node_t next{};
    node_t parent{};
    node_t previous{};
    node_t last_child{};
    node_type_t type{};
    node_view_t name{};
    node_view_t text{};
    node_set_text_t set_text{};
    attribute_t attribute{};
    set_attribute_t set_attribute{};
    remove_attribute_t remove_attribute{};
    serialize_t serialize{};
    node_clone_t clone{};
    node_binary_t append{};
    node_ternary_t replace{};
    node_binary_t remove{};
    create_node_t create_element{};
    create_node_t create_text{};
    set_inner_html_t set_inner_html{};
    query_t query{};

    [[nodiscard]] bool ready() const noexcept {
        return library && abi_version && document_create && document_destroy && document_parse && document_parse_chunk_begin && document_parse_chunk && document_parse_chunk_end && document_node && first_child && next && parent && previous && last_child && type && name && text && set_text && attribute && set_attribute && remove_attribute && serialize && clone && append && replace && remove && create_element && create_text && set_inner_html && query;
    }
};

Api api;
std::once_flag api_once;
std::array<char, 4096> fallback_path{};

struct CallbackContext {
    callback_type callback;
    void* user;
};

struct NodeCallbackContext {
    node_callback_type callback;
    void* user;
};

int invoke_callback(const byte* data, size_t size, void* context) noexcept {
    const auto state = static_cast<CallbackContext*>(context);
    return state->callback({data, size}, state->user);
}
int invoke_node_callback(void* node, void* context) noexcept { const auto state = static_cast<NodeCallbackContext*>(context); return state->callback(node, state->user); }

template <class T>
void bind(T& out, const char* name) noexcept { out = reinterpret_cast<T>(dlsym(api.library, name)); }

void init_api() noexcept {
    api.library = dlopen("liblexbor.so", RTLD_NOW | RTLD_LOCAL);
    if (api.library) api.path = "liblexbor.so";
    if (!api.library) {
        Dl_info info{};
        if (dladdr(reinterpret_cast<const void*>(&init_api), &info) && info.dli_fname) {
            const std::string_view path{info.dli_fname};
            if (const auto slash = path.rfind('/'); slash != std::string_view::npos) {
                const auto dir = path.substr(0, slash);
                if (dir.size() + sizeof("/liblexbor.so") <= fallback_path.size()) {
                    std::snprintf(fallback_path.data(), fallback_path.size(), "%.*s/liblexbor.so", static_cast<int>(dir.size()), dir.data());
                    api.library = dlopen(fallback_path.data(), RTLD_NOW | RTLD_LOCAL);
                    if (api.library) api.path = fallback_path.data();
                }
            }
        }
    }
    if (!api.library) { api.error = dlerror(); return; }
    bind(api.abi_version, "klexbor_abi_version");
    bind(api.document_create, "klexbor_document_create"); bind(api.document_destroy, "klexbor_document_destroy"); bind(api.document_parse, "klexbor_document_parse");
    bind(api.document_parse_chunk_begin, "klexbor_document_parse_chunk_begin"); bind(api.document_parse_chunk, "klexbor_document_parse_chunk"); bind(api.document_parse_chunk_end, "klexbor_document_parse_chunk_end"); bind(api.document_node, "klexbor_document_node");
    bind(api.first_child, "klexbor_node_first_child"); bind(api.next, "klexbor_node_next"); bind(api.type, "klexbor_node_type"); bind(api.name, "klexbor_node_name"); bind(api.text, "klexbor_node_text"); bind(api.set_text, "klexbor_node_set_text");
    bind(api.parent, "klexbor_node_parent"); bind(api.previous, "klexbor_node_previous"); bind(api.last_child, "klexbor_node_last_child"); bind(api.clone, "klexbor_node_clone"); bind(api.append, "klexbor_node_append"); bind(api.replace, "klexbor_node_replace"); bind(api.remove, "klexbor_node_remove");
    bind(api.attribute, "klexbor_element_get_attribute"); bind(api.set_attribute, "klexbor_element_set_attribute"); bind(api.remove_attribute, "klexbor_element_remove_attribute"); bind(api.serialize, "klexbor_serialize");
    bind(api.create_element, "klexbor_document_create_element"); bind(api.create_text, "klexbor_document_create_text"); bind(api.set_inner_html, "klexbor_element_set_inner_html"); bind(api.query, "klexbor_query");
    if (!api.ready()) api.error = "missing required klexbor symbol";
}

Api& instance() noexcept { std::call_once(api_once, init_api); return api; }

}

Document::Document() noexcept { if (auto& a = instance(); a.document_create) handle_ = a.document_create(); }
Document::Document(Document&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Document& Document::operator=(Document&& other) noexcept { if (this != &other) { if (handle_) if (auto& a = instance(); a.document_destroy) a.document_destroy(handle_); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Document::~Document() { if (handle_) if (auto& a = instance(); a.document_destroy) a.document_destroy(std::exchange(handle_, nullptr)); }
void* Document::node() const noexcept { auto& a = instance(); return handle_ && a.document_node ? a.document_node(handle_) : nullptr; }
void* Document::create_element(view_type value) noexcept { auto& a = instance(); return handle_ && a.create_element ? a.create_element(handle_, value.data(), value.size()) : nullptr; }
void* Document::create_text(view_type value) noexcept { auto& a = instance(); return handle_ && a.create_text ? a.create_text(handle_, value.data(), value.size()) : nullptr; }
int32_t Document::parse(view_type html) noexcept { auto& a = instance(); return handle_ && a.document_parse ? a.document_parse(handle_, html.data(), html.size()) : -1; }
int32_t Document::begin() noexcept { auto& a = instance(); return handle_ && a.document_parse_chunk_begin ? a.document_parse_chunk_begin(handle_) : -1; }
int32_t Document::append(view_type html) noexcept { auto& a = instance(); return handle_ && a.document_parse_chunk ? a.document_parse_chunk(handle_, html.data(), html.size()) : -1; }
int32_t Document::end() noexcept { auto& a = instance(); return handle_ && a.document_parse_chunk_end ? a.document_parse_chunk_end(handle_) : -1; }

bool loaded() noexcept { return instance().ready(); }
uint32_t abi_version() noexcept { auto& a = instance(); return a.abi_version ? a.abi_version() : 0; }
std::string_view library_path() noexcept { auto& a = instance(); return a.path ? a.path : ""; }
std::string_view load_error() noexcept { auto& a = instance(); return a.error ? a.error : ""; }
void* first_child(void* node) noexcept { auto& a = instance(); return node && a.first_child ? a.first_child(node) : nullptr; }
void* next(void* node) noexcept { auto& a = instance(); return node && a.next ? a.next(node) : nullptr; }
void* parent(void* node) noexcept { auto& a = instance(); return node && a.parent ? a.parent(node) : nullptr; }
void* previous(void* node) noexcept { auto& a = instance(); return node && a.previous ? a.previous(node) : nullptr; }
void* last_child(void* node) noexcept { auto& a = instance(); return node && a.last_child ? a.last_child(node) : nullptr; }
int32_t type(void* node) noexcept { auto& a = instance(); return node && a.type ? a.type(node) : 0; }
view_type name(void* node) noexcept { size_t size{}; auto& a = instance(); const auto* data = node && a.name ? a.name(node, &size) : nullptr; return data ? view_type{data, size} : view_type{}; }
view_type text(void* node) noexcept { size_t size{}; auto& a = instance(); const auto* data = node && a.text ? a.text(node, &size) : nullptr; return data ? view_type{data, size} : view_type{}; }
int32_t set_text(void* node, view_type value) noexcept { auto& a = instance(); return node && a.set_text ? a.set_text(node, value.data(), value.size()) : -1; }
view_type attribute(void* element, view_type key) noexcept { size_t size{}; auto& a = instance(); const auto* data = element && a.attribute ? a.attribute(element, key.data(), key.size(), &size) : nullptr; return data ? view_type{data, size} : view_type{}; }
int32_t set_attribute(void* element, view_type key, view_type value) noexcept { auto& a = instance(); return element && a.set_attribute ? a.set_attribute(element, key.data(), key.size(), value.data(), value.size()) : -1; }
int32_t remove_attribute(void* element, view_type key) noexcept { auto& a = instance(); return element && a.remove_attribute ? a.remove_attribute(element, key.data(), key.size()) : -1; }
void* clone(void* node, bool deep) noexcept { auto& a = instance(); return node && a.clone ? a.clone(node, deep) : nullptr; }
int32_t append(void* parent_node, void* node) noexcept { auto& a = instance(); return parent_node && node && a.append ? a.append(parent_node, node) : -1; }
int32_t replace(void* parent_node, void* node, void* child) noexcept { auto& a = instance(); return parent_node && node && child && a.replace ? a.replace(parent_node, node, child) : -1; }
int32_t remove(void* parent_node, void* child) noexcept { auto& a = instance(); return parent_node && child && a.remove ? a.remove(parent_node, child) : -1; }
int32_t set_inner_html(void* element, view_type html) noexcept { auto& a = instance(); return element && a.set_inner_html ? a.set_inner_html(element, html.data(), html.size()) : -1; }
int32_t query(void* root, view_type selector, node_callback_type callback, void* user) noexcept { auto& a = instance(); NodeCallbackContext context{callback, user}; return root && callback && a.query ? a.query(root, selector.data(), selector.size(), invoke_node_callback, &context) : -1; }
int32_t serialize(void* node, bool tree, bool pretty, size_t indent, callback_type callback, void* user) noexcept { auto& a = instance(); CallbackContext context{callback, user}; return node && callback && a.serialize ? a.serialize(node, tree, pretty, indent, invoke_callback, &context) : -1; }

}
