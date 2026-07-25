module;
#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>

export module utility.serialization.rapidyaml.rapidyaml_core;

export namespace rapidyaml {
using view_type = std::span<const char>;
inline constexpr int64_t none = -1;
class Node {
public:
    Node() = default;
    [[nodiscard]] explicit operator bool() const noexcept { return document_ && id_ != none; }
    [[nodiscard]] int64_t id() const noexcept { return id_; }
    [[nodiscard]] uint32_t type() const noexcept;
    [[nodiscard]] size_t size() const noexcept;
    [[nodiscard]] Node get(view_type key) const noexcept;
    [[nodiscard]] Node at(size_t index) const noexcept;
    [[nodiscard]] Node parent() const noexcept;
    [[nodiscard]] view_type key() const noexcept;
    [[nodiscard]] view_type value() const noexcept;
    [[nodiscard]] bool set_value(view_type value) noexcept;
    [[nodiscard]] Node append(view_type key, view_type value) noexcept;
    [[nodiscard]] bool erase() noexcept;
private:
    Node(void* document, int64_t id) noexcept : document_(document), id_(id) {}
    void* document_{};
    int64_t id_{none};
    friend class Document;
};
class Document {
public:
    Document() = default; explicit Document(view_type input) noexcept; Document(const Document&) = delete; Document& operator=(const Document&) = delete; Document(Document&&) noexcept; Document& operator=(Document&&) noexcept; ~Document();
    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; } [[nodiscard]] size_t format_to(std::span<char> output) const noexcept; [[nodiscard]] size_t json_to(std::span<char> output) const noexcept; [[nodiscard]] Node root() const noexcept;
private: void* handle_{};
};
[[nodiscard]] bool loaded() noexcept; [[nodiscard]] uint32_t abi_version() noexcept; [[nodiscard]] std::string_view library_path() noexcept; [[nodiscard]] std::string_view load_error() noexcept; [[nodiscard]] bool valid(view_type input) noexcept;
[[nodiscard]] size_t json_to_yaml(view_type input, std::span<char> output) noexcept;
}
