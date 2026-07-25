module;
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <dlfcn.h>
#include <mutex>
#include <span>
#include <string_view>
#include <utility>

module utility.serialization.tomlplusplus.tomlplusplus_core;

namespace tomlplusplus {
namespace {
struct Api {
    using abi_t = uint32_t (*)(); using parse_t = void* (*)(const char*, size_t); using destroy_t = void (*)(void*); using format_t = int (*)(const void*, char*, size_t, size_t*); using valid_t = int (*)(const char*, size_t);
    using root_t = void* (*)(void*); using node_path_t = void* (*)(void*, const char*, size_t); using key_t = void* (*)(void*, const char*, size_t); using index_t = void* (*)(void*, size_t); using type_t = uint32_t (*)(const void*); using size_t_ = size_t (*)(const void*); using key_at_t = int (*)(const void*, size_t, char*, size_t, size_t*); using text_t = int (*)(const void*, char*, size_t, size_t*); using set_text_t = int (*)(void*, const char*, size_t); using erase_key_t = int (*)(void*, const char*, size_t); using erase_index_t = int (*)(void*, size_t);
    void* library{}; const char* path{}; const char* error{}; abi_t abi{}; parse_t parse{}; destroy_t destroy{}; format_t format{}; format_t json{}; valid_t valid{}; root_t root{}; node_path_t node_path{}; key_t key{}; index_t index{}; type_t type{}; size_t_ size{}; key_at_t key_at{}; text_t text{}; set_text_t set_text{}; erase_key_t erase_key{}; erase_index_t erase_index{};
    [[nodiscard]] bool ready() const noexcept { return library && abi && parse && destroy && format && json && valid && root && node_path && key && index && type && size && key_at && text && set_text && erase_key && erase_index; }
} api;
std::once_flag once; std::array<char, 4096> fallback{};
template <class T> void bind(T& value, const char* name) noexcept { value = reinterpret_cast<T>(dlsym(api.library, name)); }
void init() noexcept {
    api.library = dlopen("libserdes.so", RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = "libserdes.so";
    if (!api.library) { Dl_info info{}; if (dladdr(reinterpret_cast<const void*>(&init), &info) && info.dli_fname) { std::string_view path{info.dli_fname}; const auto slash = path.rfind('/'); if (slash != path.npos && slash + 13 < fallback.size()) { std::snprintf(fallback.data(), fallback.size(), "%.*s/libserdes.so", static_cast<int>(slash), path.data()); api.library = dlopen(fallback.data(), RTLD_NOW | RTLD_LOCAL); if (api.library) api.path = fallback.data(); } } }
    if (!api.library) { api.error = dlerror(); return; }
    bind(api.abi, "kserdes_abi_version"); bind(api.parse, "ktoml_document_parse"); bind(api.destroy, "ktoml_document_destroy"); bind(api.format, "ktoml_document_format"); bind(api.json, "ktoml_document_json"); bind(api.valid, "ktoml_valid"); bind(api.root, "ktoml_document_root"); bind(api.node_path, "ktoml_node_path"); bind(api.key, "ktoml_node_key"); bind(api.index, "ktoml_node_index"); bind(api.type, "ktoml_node_type"); bind(api.size, "ktoml_node_size"); bind(api.key_at, "ktoml_node_key_at"); bind(api.text, "ktoml_node_text"); bind(api.set_text, "ktoml_node_set_text"); bind(api.erase_key, "ktoml_table_erase"); bind(api.erase_index, "ktoml_array_erase");
    if (!api.ready()) api.error = "missing required serdes TOML symbols";
}
Api& instance() noexcept { std::call_once(once, init); return api; }
}
Document::Document(view_type input) noexcept { auto& a = instance(); if (a.parse) handle_ = a.parse(input.data(), input.size()); }
Document::Document(Document&& other) noexcept : handle_(std::exchange(other.handle_, nullptr)) {}
Document& Document::operator=(Document&& other) noexcept { if (this != &other) { if (handle_) if (auto& a = instance(); a.destroy) a.destroy(handle_); handle_ = std::exchange(other.handle_, nullptr); } return *this; }
Document::~Document() { if (handle_) if (auto& a = instance(); a.destroy) a.destroy(handle_); }
size_t Document::format_to(std::span<char> output) const noexcept { size_t size{}; auto& a = instance(); return handle_ && a.format ? (a.format(handle_, output.data(), output.size(), &size), size) : 0; }
size_t Document::json_to(std::span<char> output) const noexcept { size_t size{}; auto& a = instance(); return handle_ && a.json ? (a.json(handle_, output.data(), output.size(), &size), size) : 0; }
Node Document::root() const noexcept { auto& a = instance(); return Node{handle_ && a.root ? a.root(handle_) : nullptr}; }
Node Document::find(view_type path) const noexcept { auto& a = instance(); return Node{handle_ && a.node_path ? a.node_path(handle_, path.data(), path.size()) : nullptr}; }
uint32_t Node::type() const noexcept { auto& a = instance(); return handle_ && a.type ? a.type(handle_) : 0; } size_t Node::size() const noexcept { auto& a = instance(); return handle_ && a.size ? a.size(handle_) : 0; } Node Node::get(view_type key) const noexcept { auto& a = instance(); return Node{handle_ && a.key ? a.key(handle_, key.data(), key.size()) : nullptr}; } Node Node::at(size_t index) const noexcept { auto& a = instance(); return Node{handle_ && a.index ? a.index(handle_, index) : nullptr}; } view_type Node::key_at(size_t index) const noexcept { thread_local std::array<char, 1024> buffer; size_t size{}; auto& a = instance(); return handle_ && a.key_at && a.key_at(handle_, index, buffer.data(), buffer.size(), &size) == 0 ? view_type{buffer.data(), size} : view_type{}; } size_t Node::text_to(std::span<char> output) const noexcept { size_t size{}; auto& a = instance(); return handle_ && a.text ? (a.text(handle_, output.data(), output.size(), &size), size) : 0; } bool Node::set_text(view_type value) noexcept { auto& a = instance(); return handle_ && a.set_text && a.set_text(handle_, value.data(), value.size()) == 0; } bool Node::erase(view_type key) noexcept { auto& a = instance(); return handle_ && a.erase_key && a.erase_key(handle_, key.data(), key.size()) == 0; } bool Node::erase(size_t index) noexcept { auto& a = instance(); return handle_ && a.erase_index && a.erase_index(handle_, index) == 0; }
bool loaded() noexcept { return instance().ready(); } uint32_t abi_version() noexcept { auto& a = instance(); return a.abi ? a.abi() : 0; } std::string_view library_path() noexcept { auto& a = instance(); return a.path ? a.path : ""; } std::string_view load_error() noexcept { auto& a = instance(); return a.error ? a.error : ""; } bool valid(view_type input) noexcept { auto& a = instance(); return a.valid && a.valid(input.data(), input.size()) != 0; }
}
