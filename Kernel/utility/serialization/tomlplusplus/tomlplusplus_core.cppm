module;
#include <cstddef>
#include <cstdint>
#include <memory>
#include <span>
#include <string_view>

export module utility.serialization.tomlplusplus.tomlplusplus_core;

export namespace tomlplusplus {

using view_type = std::span<const char>;

class Node {
public:
    Node() = default;
    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] uint32_t type() const noexcept;
    [[nodiscard]] size_t size() const noexcept;
    [[nodiscard]] Node get(view_type key) const noexcept;
    [[nodiscard]] Node at(size_t index) const noexcept;
    [[nodiscard]] view_type key_at(size_t index) const noexcept;
    [[nodiscard]] size_t text_to(std::span<char> output) const noexcept;
    [[nodiscard]] bool set_text(view_type value) noexcept;
    [[nodiscard]] bool erase(view_type key) noexcept;
    [[nodiscard]] bool erase(size_t index) noexcept;
private:
    explicit Node(void* handle) noexcept : handle_(handle) {}
    void* handle_{};
    friend class Document;
};

class Document {
public:
    Document() = default;
    explicit Document(view_type input) noexcept;
    Document(const Document&) = delete;
    Document& operator=(const Document&) = delete;
    Document(Document&&) noexcept;
    Document& operator=(Document&&) noexcept;
    ~Document();
    [[nodiscard]] explicit operator bool() const noexcept { return handle_ != nullptr; }
    [[nodiscard]] size_t format_to(std::span<char> output) const noexcept;
    [[nodiscard]] size_t json_to(std::span<char> output) const noexcept;
    [[nodiscard]] Node root() const noexcept;
    [[nodiscard]] Node find(view_type path) const noexcept;
private:
    void* handle_{};
};

[[nodiscard]] bool loaded() noexcept;
[[nodiscard]] uint32_t abi_version() noexcept;
[[nodiscard]] std::string_view library_path() noexcept;
[[nodiscard]] std::string_view load_error() noexcept;
[[nodiscard]] bool valid(view_type input) noexcept;

}
