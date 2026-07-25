module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>

export module utility.serialization.lexbor.lexbor_core;

export namespace lexbor {

using byte = uint8_t;
using view_type = std::span<const byte>;
using callback_type = int (*)(view_type, void*) noexcept;
using node_callback_type = int (*)(void*, void*) noexcept;

inline constexpr int32_t status_ok = 0;

class Document {
public:
    Document() noexcept;
    Document(const Document&) = delete;
    Document& operator=(const Document&) = delete;
    Document(Document&&) noexcept;
    Document& operator=(Document&&) noexcept;
    ~Document();
    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] void* node() const noexcept;
    [[nodiscard]] void* create_element(view_type name) noexcept;
    [[nodiscard]] void* create_text(view_type text) noexcept;
    [[nodiscard]] int32_t parse(view_type html) noexcept;
    [[nodiscard]] int32_t begin() noexcept;
    [[nodiscard]] int32_t append(view_type html) noexcept;
    [[nodiscard]] int32_t end() noexcept;
private:
    void* handle_{};
};

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] void* first_child(void* node) noexcept;
[[nodiscard]] void* next(void* node) noexcept;
[[nodiscard]] void* parent(void* node) noexcept;
[[nodiscard]] void* previous(void* node) noexcept;
[[nodiscard]] void* last_child(void* node) noexcept;
[[nodiscard]] int32_t type(void* node) noexcept;
[[nodiscard]] view_type name(void* node) noexcept;
[[nodiscard]] view_type text(void* node) noexcept;
[[nodiscard]] int32_t set_text(void* node, view_type value) noexcept;
[[nodiscard]] view_type attribute(void* element, view_type name) noexcept;
[[nodiscard]] int32_t set_attribute(void* element, view_type name, view_type value) noexcept;
[[nodiscard]] int32_t remove_attribute(void* element, view_type name) noexcept;
[[nodiscard]] void* clone(void* node, bool deep) noexcept;
[[nodiscard]] int32_t append(void* parent, void* node) noexcept;
[[nodiscard]] int32_t replace(void* parent, void* node, void* child) noexcept;
[[nodiscard]] int32_t remove(void* parent, void* child) noexcept;
[[nodiscard]] int32_t set_inner_html(void* element, view_type html) noexcept;
[[nodiscard]] int32_t query(void* root, view_type selector, node_callback_type callback, void* user) noexcept;
[[nodiscard]] int32_t serialize(void* node, bool tree, bool pretty, size_t indent, callback_type callback, void* user) noexcept;

}
