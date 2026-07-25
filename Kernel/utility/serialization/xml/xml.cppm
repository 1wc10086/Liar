module;
#include "lib/pugixml/pugixml.hpp"
#include <string_view>
#include <string>
#include <cstdint>
#include <utility>
#include <expected>
#include <iterator>
#include <memory>
export module utility.xml.xml;
export {
namespace xml {

enum class NodeType : uint8_t {
    Null = 0,
    Document = 1,
    Element = 2,
    PCDATA = 3,
    CDATA = 4,
    Comment = 5,
    PI = 6,
    Declaration = 7,
    DocType = 8
};

class Attribute {
    pugi::xml_attribute attr;
public:
    inline Attribute(pugi::xml_attribute a = {}) noexcept : attr(a) {}

    [[nodiscard]] inline explicit operator bool() const noexcept { return !attr.empty(); }
    [[nodiscard]] inline bool empty() const noexcept { return attr.empty(); }
    
    [[nodiscard]] inline pugi::xml_attribute raw() const noexcept { return attr; }

    [[nodiscard]] inline std::string_view name() const noexcept { return attr.name(); }
    [[nodiscard]] inline std::string_view value() const noexcept { return attr.value(); }

    [[nodiscard]] inline const char* as_string(const char* def = "") const noexcept { return attr.as_string(def); }
    [[nodiscard]] inline int64_t as_int(int64_t def = 0) const noexcept { return attr.as_llong(def); }
    [[nodiscard]] inline uint64_t as_uint(uint64_t def = 0) const noexcept { return attr.as_ullong(def); }
    [[nodiscard]] inline double as_double(double def = 0.0) const noexcept { return attr.as_double(def); }
    [[nodiscard]] inline bool as_bool(bool def = false) const noexcept { return attr.as_bool(def); }

    inline bool set_name(const char* n) noexcept { return attr.set_name(n); }
    inline bool set_name(std::string_view n) { return attr.set_name(std::string(n).c_str()); }

    inline bool set_value(const char* v) noexcept { return attr.set_value(v); }
    inline bool set_value(std::string_view v) { return attr.set_value(std::string(v).c_str()); }
    inline bool set_value(int64_t v) noexcept { return attr.set_value(static_cast<long long>(v)); }
    inline bool set_value(uint64_t v) noexcept { return attr.set_value(static_cast<unsigned long long>(v)); }
    inline bool set_value(double v) noexcept { return attr.set_value(v); }
    inline bool set_value(bool v) noexcept { return attr.set_value(v); }
};

class Node {
protected:
    pugi::xml_node node;
public:
    inline Node(pugi::xml_node n = {}) noexcept : node(n) {}

    [[nodiscard]] inline explicit operator bool() const noexcept { return !node.empty(); }
    [[nodiscard]] inline bool empty() const noexcept { return node.empty(); }
    [[nodiscard]] inline pugi::xml_node raw() const noexcept { return node; }

    [[nodiscard]] inline NodeType type() const noexcept { return static_cast<NodeType>(node.type()); }
    [[nodiscard]] inline bool is_null() const noexcept { return type() == NodeType::Null; }
    [[nodiscard]] inline bool is_element() const noexcept { return type() == NodeType::Element; }

    [[nodiscard]] inline std::string_view name() const noexcept { return node.name(); }
    [[nodiscard]] inline std::string_view value() const noexcept { return node.value(); }
    [[nodiscard]] inline std::string_view text() const noexcept { return node.text().get(); }

    inline bool set_name(const char* n) noexcept { return node.set_name(n); }
    inline bool set_name(std::string_view n) { return node.set_name(std::string(n).c_str()); }

    inline bool set_value(const char* v) noexcept { return node.set_value(v); }
    inline bool set_value(std::string_view v) { return node.set_value(std::string(v).c_str()); }

    [[nodiscard]] inline Attribute attribute(const char* name) const noexcept { return Attribute(node.attribute(name)); }
    [[nodiscard]] inline Attribute attribute(std::string_view name) const { return Attribute(node.attribute(std::string(name).c_str())); }

    [[nodiscard]] inline Node child(const char* name) const noexcept { return Node(node.child(name)); }
    [[nodiscard]] inline Node child(std::string_view name) const { return Node(node.child(std::string(name).c_str())); }

    [[nodiscard]] inline Node first_child() const noexcept { return Node(node.first_child()); }
    [[nodiscard]] inline Node last_child() const noexcept { return Node(node.last_child()); }
    
    [[nodiscard]] inline Node next_sibling() const noexcept { return Node(node.next_sibling()); }
    [[nodiscard]] inline Node next_sibling(const char* name) const noexcept { return Node(node.next_sibling(name)); }
    [[nodiscard]] inline Node next_sibling(std::string_view name) const { return Node(node.next_sibling(std::string(name).c_str())); }

    [[nodiscard]] inline Node previous_sibling() const noexcept { return Node(node.previous_sibling()); }
    [[nodiscard]] inline Node previous_sibling(const char* name) const noexcept { return Node(node.previous_sibling(name)); }
    [[nodiscard]] inline Node previous_sibling(std::string_view name) const { return Node(node.previous_sibling(std::string(name).c_str())); }

    inline Attribute append_attribute(const char* name) noexcept { return Attribute(node.append_attribute(name)); }
    inline Attribute append_attribute(std::string_view name) { return Attribute(node.append_attribute(std::string(name).c_str())); }

    inline Node append_child(NodeType t = NodeType::Element) noexcept { return Node(node.append_child(static_cast<pugi::xml_node_type>(t))); }
    inline Node append_child(const char* name) noexcept { return Node(node.append_child(name)); }
    inline Node append_child(std::string_view name) { return Node(node.append_child(std::string(name).c_str())); }
    
    inline Node prepend_child(NodeType t = NodeType::Element) noexcept { return Node(node.prepend_child(static_cast<pugi::xml_node_type>(t))); }
    inline Node prepend_child(const char* name) noexcept { return Node(node.prepend_child(name)); }
    inline Node prepend_child(std::string_view name) { return Node(node.prepend_child(std::string(name).c_str())); }

    inline bool remove_attribute(const char* name) noexcept { return node.remove_attribute(name); }
    inline bool remove_attribute(std::string_view name) { return node.remove_attribute(std::string(name).c_str()); }
    inline bool remove_attribute(Attribute a) noexcept { return node.remove_attribute(a.raw()); }

    inline bool remove_child(const char* name) noexcept { return node.remove_child(name); }
    inline bool remove_child(std::string_view name) { return node.remove_child(std::string(name).c_str()); }
    inline bool remove_child(Node n) noexcept { return node.remove_child(n.raw()); }

    inline void set_text(const char* text) noexcept { node.text().set(text); }
    inline void set_text(std::string_view text) { node.text().set(std::string(text).c_str()); }
    inline void set_text(int64_t v) noexcept { node.text().set(static_cast<long long>(v)); }
    inline void set_text(uint64_t v) noexcept { node.text().set(static_cast<unsigned long long>(v)); }
    inline void set_text(double v) noexcept { node.text().set(v); }
    inline void set_text(bool v) noexcept { node.text().set(v); }

    class AttributeIter {
        pugi::xml_attribute_iterator it;
    public:
        using iterator_category = std::input_iterator_tag;
        using value_type = Attribute;
        using difference_type = std::ptrdiff_t;
        using pointer = void;
        using reference = Attribute;

        inline AttributeIter() noexcept = default;
        inline AttributeIter(pugi::xml_attribute_iterator i) noexcept : it(i) {}
        [[nodiscard]] inline Attribute operator*() const noexcept { return Attribute(*it); }
        inline AttributeIter& operator++() noexcept { ++it; return *this; }
        inline AttributeIter operator++(int) noexcept { auto temp = *this; ++it; return temp; }
        [[nodiscard]] inline bool operator!=(const AttributeIter& o) const noexcept { return it != o.it; }
        [[nodiscard]] inline bool operator==(const AttributeIter& o) const noexcept { return it == o.it; }
    };
    struct AttributeRange {
        pugi::xml_node n;
        [[nodiscard]] inline AttributeIter begin() const noexcept { return AttributeIter(n.attributes_begin()); }
        [[nodiscard]] inline AttributeIter end() const noexcept { return AttributeIter(n.attributes_end()); }
    };
    [[nodiscard]] inline AttributeRange attributes() const noexcept { return {node}; }

    class ChildIter {
        pugi::xml_node_iterator it;
    public:
        using iterator_category = std::input_iterator_tag;
        using value_type = Node;
        using difference_type = std::ptrdiff_t;
        using pointer = void;
        using reference = Node;

        inline ChildIter() noexcept = default;
        inline ChildIter(pugi::xml_node_iterator i) noexcept : it(i) {}
        [[nodiscard]] inline Node operator*() const noexcept { return Node(*it); }
        inline ChildIter& operator++() noexcept { ++it; return *this; }
        inline ChildIter operator++(int) noexcept { auto temp = *this; ++it; return temp; }
        [[nodiscard]] inline bool operator!=(const ChildIter& o) const noexcept { return it != o.it; }
        [[nodiscard]] inline bool operator==(const ChildIter& o) const noexcept { return it == o.it; }
    };
    struct ChildRange {
        pugi::xml_node n;
        [[nodiscard]] inline ChildIter begin() const noexcept { return ChildIter(n.begin()); }
        [[nodiscard]] inline ChildIter end() const noexcept { return ChildIter(n.end()); }
    };
    [[nodiscard]] inline ChildRange children() const noexcept { return {node}; }

    class NamedChildIter {
        pugi::xml_named_node_iterator it;
    public:
        using iterator_category = std::input_iterator_tag;
        using value_type = Node;
        using difference_type = std::ptrdiff_t;
        using pointer = void;
        using reference = Node;

        inline NamedChildIter() noexcept = default;
        inline NamedChildIter(pugi::xml_named_node_iterator i) noexcept : it(i) {}
        [[nodiscard]] inline Node operator*() const noexcept { return Node(*it); }
        inline NamedChildIter& operator++() noexcept { ++it; return *this; }
        inline NamedChildIter operator++(int) noexcept { auto temp = *this; ++it; return temp; }
        [[nodiscard]] inline bool operator!=(const NamedChildIter& o) const noexcept { return it != o.it; }
        [[nodiscard]] inline bool operator==(const NamedChildIter& o) const noexcept { return it == o.it; }
    };
    struct CStrNamedChildRange {
        pugi::xml_node n;
        const char* name;
        [[nodiscard]] inline NamedChildIter begin() const noexcept { return NamedChildIter(n.children(name).begin()); }
        [[nodiscard]] inline NamedChildIter end() const noexcept { return NamedChildIter(n.children(name).end()); }
    };
    struct StrNamedChildRange {
        pugi::xml_node n;
        std::string name;
        [[nodiscard]] inline NamedChildIter begin() const noexcept { return NamedChildIter(n.children(name.c_str()).begin()); }
        [[nodiscard]] inline NamedChildIter end() const noexcept { return NamedChildIter(n.children(name.c_str()).end()); }
    };
    [[nodiscard]] inline CStrNamedChildRange children(const char* name) const noexcept { return {node, name}; }
    [[nodiscard]] inline StrNamedChildRange children(std::string_view name) const { return {node, std::string(name)}; }
};

struct ParseError {
    std::string description;
    ptrdiff_t offset;
};

class StringWriter final : public pugi::xml_writer {
public:
    std::string result;
    void write(const void* data, size_t size) override {
        result.append(static_cast<const char*>(data), size);
    }
};

class Document : public Node {
    std::unique_ptr<pugi::xml_document> doc;
public:
    inline Document() : Node(), doc(std::make_unique<pugi::xml_document>()) {
        node = *doc;
    }
    
    Document(const Document&) = delete;
    Document& operator=(const Document&) = delete;
    
    inline Document(Document&& other) noexcept : Node(other.node), doc(std::move(other.doc)) {
        other.node = pugi::xml_node();
    }
    
    inline Document& operator=(Document&& other) noexcept {
        if (this != &other) {
            doc = std::move(other.doc);
            node = other.node;
            other.node = pugi::xml_node();
        }
        return *this;
    }

    [[nodiscard]] inline explicit operator bool() const noexcept { return doc != nullptr && !node.empty(); }
    [[nodiscard]] inline Node root() const noexcept { return Node(*doc); }

    [[nodiscard]] static inline std::expected<Document, ParseError> parse(std::string_view str) {
        Document d;
        pugi::xml_parse_result res = d.doc->load_buffer(str.data(), str.size());
        if (!res) {
            return std::unexpected(ParseError{res.description(), res.offset});
        }
        return d;
    }

    [[nodiscard]] static inline std::expected<Document, ParseError> load_file(const char* path) {
        Document d;
        pugi::xml_parse_result res = d.doc->load_file(path);
        if (!res) {
            return std::unexpected(ParseError{res.description(), res.offset});
        }
        return d;
    }
    [[nodiscard]] static inline std::expected<Document, ParseError> load_file(std::string_view path) {
        return load_file(std::string(path).c_str());
    }

    [[nodiscard]] inline std::string write(std::string_view indent = "  ") const {
        StringWriter writer;
        if (doc) {
            doc->save(writer, std::string(indent).c_str());
        }
        return std::move(writer.result);
    }

    inline bool save_file(const char* path, std::string_view indent = "  ") const {
        return doc ? doc->save_file(path, std::string(indent).c_str()) : false;
    }
    inline bool save_file(std::string_view path, std::string_view indent = "  ") const {
        return save_file(std::string(path).c_str(), indent);
    }
};

}

}
