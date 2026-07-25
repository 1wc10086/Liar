#include <cstddef>
#include <cstdint>
#include <cstring>
#include <algorithm>
#include <exception>
#include <ostream>
#include <span>
#include <streambuf>
#include <string_view>
#include <utility>
#include <vector>

#include "tomlplusplus/toml.hpp"
#include "ryml.hpp"
#include "ryml_std.hpp"
#include "c4/yml/emit_buf.hpp"
#include "c4/yml/emit_container.hpp"
#include "c4/yml/parse.hpp"

#if defined(__GNUC__)
#define SERDES_EXPORT __attribute__((visibility("default")))
#else
#define SERDES_EXPORT
#endif

namespace {

class Buffer final : public std::streambuf {
public:
    explicit Buffer(std::span<char> output = {}) : output_(output) {}
    [[nodiscard]] size_t size() const noexcept { return size_; }
protected:
    std::streamsize xsputn(const char* data, std::streamsize size) override {
        const auto count = static_cast<size_t>(size);
        if (size_ < output_.size()) std::memcpy(output_.data() + size_, data, std::min(count, output_.size() - size_));
        size_ += count;
        return size;
    }
    int_type overflow(int_type value) override {
        if (!traits_type::eq_int_type(value, traits_type::eof())) { const auto character = traits_type::to_char_type(value); xsputn(&character, 1); }
        return value;
    }
private:
    std::span<char> output_;
    size_t size_{};
};

template <class Document>
int write_document(Document& document, char* output, size_t capacity, size_t* size) noexcept {
    try {
        Buffer counter;
        std::ostream count_stream{&counter};
        count_stream << document;
        *size = counter.size();
        if (capacity < *size) return 1;
        Buffer buffer{{output, capacity}};
        std::ostream stream{&buffer};
        stream << document;
        return stream ? 0 : -1;
    } catch (...) { return -1; }
}

struct TomlDocument { toml::table value; };
struct YamlDocument { ryml::Tree value; };

int copy_view(std::string_view value, char* output, size_t capacity, size_t* size) noexcept {
    if (!size) return -1;
    *size = value.size();
    if (capacity < value.size()) return 1;
    if (value.size()) std::memcpy(output, value.data(), value.size());
    return 0;
}

int write_view(std::string_view value, char* output, size_t capacity, size_t* size) noexcept {
    if (!size) return -1;
    *size = value.size();
    if (capacity < value.size()) return 1;
    if (value.size()) std::memcpy(output, value.data(), value.size());
    return 0;
}

int write_toml_node(const toml::node* node, char* output, size_t capacity, size_t* size) noexcept {
    if (!node) return -1;
    auto formatter = toml::toml_formatter{*node};
    return write_document(formatter, output, capacity, size);
}

int64_t yaml_none() noexcept { return static_cast<int64_t>(ryml::NONE); }

}

extern "C" {

SERDES_EXPORT uint32_t kserdes_abi_version() noexcept { return 1; }

SERDES_EXPORT void* ktoml_document_parse(const char* input, size_t size) noexcept {
    if (!input && size) return nullptr;
    try { return new TomlDocument{toml::parse(std::string_view{input, size})}; }
    catch (...) { return nullptr; }
}

SERDES_EXPORT void ktoml_document_destroy(void* document) noexcept { delete static_cast<TomlDocument*>(document); }
SERDES_EXPORT int ktoml_document_format(const void* document, char* output, size_t capacity, size_t* size) noexcept {
    if (!document || !size) return -1;
    auto& value = const_cast<TomlDocument*>(static_cast<const TomlDocument*>(document))->value;
    return write_document(value, output, capacity, size);
}
SERDES_EXPORT int ktoml_document_json(const void* document, char* output, size_t capacity, size_t* size) noexcept {
    if (!document || !size) return -1;
    auto& value = const_cast<TomlDocument*>(static_cast<const TomlDocument*>(document))->value;
    auto formatter = toml::json_formatter{value};
    return write_document(formatter, output, capacity, size);
}
SERDES_EXPORT int ktoml_valid(const char* input, size_t size) noexcept {
    auto* document = ktoml_document_parse(input, size);
    ktoml_document_destroy(document);
    return document ? 1 : 0;
}
SERDES_EXPORT void* ktoml_document_root(void* document) noexcept { return document ? &static_cast<TomlDocument*>(document)->value : nullptr; }
SERDES_EXPORT void* ktoml_node_path(void* document, const char* path, size_t size) noexcept {
    if (!document || (!path && size)) return nullptr;
    return static_cast<TomlDocument*>(document)->value.at_path(std::string_view{path, size}).node();
}
SERDES_EXPORT void* ktoml_node_key(void* node, const char* key, size_t size) noexcept {
    if (!node || (!key && size)) return nullptr;
    auto* table = static_cast<toml::node*>(node)->as_table();
    return table ? table->get(std::string_view{key, size}) : nullptr;
}
SERDES_EXPORT void* ktoml_node_index(void* node, size_t index) noexcept {
    auto* array = node ? static_cast<toml::node*>(node)->as_array() : nullptr;
    return array ? array->get(index) : nullptr;
}
SERDES_EXPORT uint32_t ktoml_node_type(const void* node) noexcept { return node ? static_cast<uint32_t>(static_cast<const toml::node*>(node)->type()) : 0; }
SERDES_EXPORT size_t ktoml_node_size(const void* node) noexcept {
    if (!node) return 0;
    const auto* value = static_cast<const toml::node*>(node);
    if (const auto* table = value->as_table()) return table->size();
    if (const auto* array = value->as_array()) return array->size();
    return 0;
}
SERDES_EXPORT int ktoml_node_key_at(const void* node, size_t index, char* output, size_t capacity, size_t* size) noexcept {
    const auto* table = node ? static_cast<const toml::node*>(node)->as_table() : nullptr;
    if (!table || index >= table->size()) return -1;
    auto iterator = table->begin();
    std::advance(iterator, static_cast<ptrdiff_t>(index));
    return write_view(iterator->first.str(), output, capacity, size);
}
SERDES_EXPORT int ktoml_node_text(const void* node, char* output, size_t capacity, size_t* size) noexcept { return write_toml_node(static_cast<const toml::node*>(node), output, capacity, size); }
SERDES_EXPORT int ktoml_node_set_text(void* node, const char* value, size_t size) noexcept {
    if (!node || (!value && size)) return -1;
    auto* target = static_cast<toml::node*>(node);
    if (auto* text = target->as_string()) { text->get() = std::string{value, size}; return 0; }
    return -1;
}
SERDES_EXPORT int ktoml_table_erase(void* node, const char* key, size_t size) noexcept {
    auto* table = node ? static_cast<toml::node*>(node)->as_table() : nullptr;
    return table ? static_cast<int>(table->erase(std::string_view{key, size})) : -1;
}
SERDES_EXPORT int ktoml_array_erase(void* node, size_t index) noexcept {
    auto* array = node ? static_cast<toml::node*>(node)->as_array() : nullptr;
    if (!array || index >= array->size()) return -1;
    array->erase(array->cbegin() + static_cast<ptrdiff_t>(index));
    return 0;
}

SERDES_EXPORT void* kryml_document_parse(const char* input, size_t size) noexcept {
    if (!input && size) return nullptr;
    try {
        auto* document = new YamlDocument;
        ryml::parse_in_arena(ryml::csubstr{input, size}, &document->value);
        return document;
    } catch (...) { return nullptr; }
}

SERDES_EXPORT void kryml_document_destroy(void* document) noexcept { delete static_cast<YamlDocument*>(document); }
SERDES_EXPORT int kryml_document_format(const void* document, char* output, size_t capacity, size_t* size) noexcept {
    if (!document || !size) return -1;
    try {
        const auto emitted = ryml::emit_yaml(static_cast<const YamlDocument*>(document)->value, ryml::substr{output, capacity}, false);
        *size = emitted.len;
        if (capacity < emitted.len) return 1;
        return 0;
    } catch (...) { return -1; }
}
SERDES_EXPORT int kryml_document_json(const void* document, char* output, size_t capacity, size_t* size) noexcept {
    if (!document || !size) return -1;
    try {
        const auto emitted = ryml::emit_json(static_cast<const YamlDocument*>(document)->value, ryml::substr{output, capacity}, false);
        *size = emitted.len;
        return capacity < emitted.len ? 1 : 0;
    } catch (...) { return -1; }
}
SERDES_EXPORT int kryml_json_to_yaml(const char* input, size_t input_size, char* output, size_t capacity, size_t* size) noexcept {
    if ((!input && input_size) || !size) return -1;
    try {
        ryml::Tree document;
        ryml::parse_json_in_arena(ryml::csubstr{input, input_size}, &document);
        const auto emitted = ryml::emit_yaml(document, ryml::substr{output, capacity}, false);
        *size = emitted.len;
        return capacity < emitted.len ? 1 : 0;
    } catch (...) { return -1; }
}
SERDES_EXPORT int kryml_valid(const char* input, size_t size) noexcept {
    auto* document = kryml_document_parse(input, size);
    kryml_document_destroy(document);
    return document ? 1 : 0;
}
SERDES_EXPORT int64_t kryml_document_root(const void* document) noexcept { return document ? static_cast<int64_t>(static_cast<const YamlDocument*>(document)->value.root_id_maybe()) : yaml_none(); }
SERDES_EXPORT int64_t kryml_node_key(const void* document, int64_t node, const char* key, size_t size) noexcept {
    if (!document || node == yaml_none() || (!key && size)) return yaml_none();
    return static_cast<int64_t>(static_cast<const YamlDocument*>(document)->value.find_child(static_cast<ryml::id_type>(node), ryml::csubstr{key, size}));
}
SERDES_EXPORT int64_t kryml_node_index(const void* document, int64_t node, size_t index) noexcept {
    if (!document || node == yaml_none()) return yaml_none();
    return static_cast<int64_t>(static_cast<const YamlDocument*>(document)->value.child(static_cast<ryml::id_type>(node), static_cast<ryml::id_type>(index)));
}
SERDES_EXPORT int64_t kryml_node_parent(const void* document, int64_t node) noexcept { return document && node != yaml_none() ? static_cast<int64_t>(static_cast<const YamlDocument*>(document)->value.parent(static_cast<ryml::id_type>(node))) : yaml_none(); }
SERDES_EXPORT uint32_t kryml_node_type(const void* document, int64_t node) noexcept { return document && node != yaml_none() ? static_cast<uint32_t>(static_cast<const YamlDocument*>(document)->value.type(static_cast<ryml::id_type>(node)).m_bits) : 0; }
SERDES_EXPORT size_t kryml_node_size(const void* document, int64_t node) noexcept { return document && node != yaml_none() ? static_cast<size_t>(static_cast<const YamlDocument*>(document)->value.num_children(static_cast<ryml::id_type>(node))) : 0; }
SERDES_EXPORT const char* kryml_node_key_view(const void* document, int64_t node, size_t* size) noexcept {
    if (!document || node == yaml_none() || !size) return nullptr;
    const auto& tree = static_cast<const YamlDocument*>(document)->value;
    if (!tree.has_key(static_cast<ryml::id_type>(node))) return nullptr;
    const auto value = tree.key(static_cast<ryml::id_type>(node)); *size = value.len; return value.str;
}
SERDES_EXPORT const char* kryml_node_value_view(const void* document, int64_t node, size_t* size) noexcept {
    if (!document || node == yaml_none() || !size) return nullptr;
    const auto& tree = static_cast<const YamlDocument*>(document)->value;
    if (!tree.has_val(static_cast<ryml::id_type>(node))) return nullptr;
    const auto value = tree.val(static_cast<ryml::id_type>(node)); *size = value.len; return value.str;
}
SERDES_EXPORT int kryml_node_set_value(void* document, int64_t node, const char* value, size_t size) noexcept {
    if (!document || node == yaml_none() || (!value && size)) return -1;
    auto& tree = static_cast<YamlDocument*>(document)->value;
    const auto id = static_cast<ryml::id_type>(node);
    if (tree.is_container(id)) return -1;
    tree.set_val(id, tree.copy_to_arena({value, size}));
    return 0;
}
SERDES_EXPORT int64_t kryml_map_append(void* document, int64_t node, const char* key, size_t key_size, const char* value, size_t value_size) noexcept {
    if (!document || node == yaml_none() || (!key && key_size) || (!value && value_size)) return yaml_none();
    auto& tree = static_cast<YamlDocument*>(document)->value;
    const auto parent = static_cast<ryml::id_type>(node);
    if (!tree.is_map(parent)) return yaml_none();
    const auto child = tree.append_child(parent);
    tree.set_key(child, tree.copy_to_arena({key, key_size}));
    tree.set_val(child, tree.copy_to_arena({value, value_size}));
    return static_cast<int64_t>(child);
}
SERDES_EXPORT int kryml_node_remove(void* document, int64_t node) noexcept {
    if (!document || node == yaml_none()) return -1;
    auto& tree = static_cast<YamlDocument*>(document)->value;
    const auto id = static_cast<ryml::id_type>(node);
    if (tree.is_root(id)) return -1;
    tree.remove(id);
    return 0;
}

}
