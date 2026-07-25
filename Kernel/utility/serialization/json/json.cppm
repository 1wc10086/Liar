module;
#include "lib/yyjson/yyjson.h"
#include <cstdint>
#include <cstring>
#include <string>
#include <string_view>
#include <utility>
export module utility.json;
export {
namespace json {

enum class ReadFlag : uint32_t {
    None                = YYJSON_READ_NOFLAG,
    Insitu              = YYJSON_READ_INSITU,
    StopWhenDone        = YYJSON_READ_STOP_WHEN_DONE,
    AllowTrailingCommas = YYJSON_READ_ALLOW_TRAILING_COMMAS,
    AllowComments       = YYJSON_READ_ALLOW_COMMENTS,
    AllowInfAndNan      = YYJSON_READ_ALLOW_INF_AND_NAN
};
[[nodiscard]] inline ReadFlag operator|(ReadFlag a, ReadFlag b) noexcept {
    return static_cast<ReadFlag>(static_cast<uint32_t>(a) | static_cast<uint32_t>(b));
}

enum class WriteFlag : uint32_t {
    None           = YYJSON_WRITE_NOFLAG,
    Pretty         = YYJSON_WRITE_PRETTY,
    EscapeUnicode  = YYJSON_WRITE_ESCAPE_UNICODE,
    EscapeSlashes  = YYJSON_WRITE_ESCAPE_SLASHES,
    AllowInfAndNan = YYJSON_WRITE_ALLOW_INF_AND_NAN
};
[[nodiscard]] inline WriteFlag operator|(WriteFlag a, WriteFlag b) noexcept {
    return static_cast<WriteFlag>(static_cast<uint32_t>(a) | static_cast<uint32_t>(b));
}

enum class Type : uint8_t {
    None = 0, Raw = 1, Null = 2, Bool = 3, Num = 4, Str = 5, Arr = 6, Obj = 7
};
enum class NumSubtype : uint8_t {
    Uint = 0, Sint = 8, Real = 16
};

class Value {
protected:
    yyjson_val* val;
public:
    inline Value(yyjson_val* v = nullptr) noexcept : val(v) {}
    [[nodiscard]] inline explicit operator bool() const noexcept { return val != nullptr; }
    [[nodiscard]] inline yyjson_val* raw() const noexcept { return val; }

    [[nodiscard]] inline Type       type()        const noexcept { return static_cast<Type>(yyjson_get_type(val)); }
    [[nodiscard]] inline NumSubtype num_subtype() const noexcept { return static_cast<NumSubtype>(yyjson_get_subtype(val)); }

    [[nodiscard]] inline bool is_null() const noexcept { return type() == Type::Null; }
    [[nodiscard]] inline bool is_bool() const noexcept { return type() == Type::Bool; }
    [[nodiscard]] inline bool is_num()  const noexcept { return type() == Type::Num;  }
    [[nodiscard]] inline bool is_str()  const noexcept { return type() == Type::Str;  }
    [[nodiscard]] inline bool is_arr()  const noexcept { return type() == Type::Arr;  }
    [[nodiscard]] inline bool is_obj()  const noexcept { return type() == Type::Obj;  }
    [[nodiscard]] inline bool is_int()  const noexcept {
        return is_num() && (num_subtype() == NumSubtype::Sint || num_subtype() == NumSubtype::Uint);
    }
    [[nodiscard]] inline bool is_real() const noexcept {
        return is_num() && num_subtype() == NumSubtype::Real;
    }
    [[nodiscard]] inline bool is_uint() const noexcept {
        return is_num() && num_subtype() == NumSubtype::Uint;
    }

    [[nodiscard]] inline bool        get_bool()     const noexcept { return yyjson_get_bool(val); }
    [[nodiscard]] inline uint64_t    get_uint()     const noexcept { return yyjson_get_uint(val); }
    [[nodiscard]] inline int64_t     get_sint()     const noexcept { return yyjson_get_sint(val); }
    [[nodiscard]] inline double      get_real()     const noexcept { return yyjson_get_real(val); }

    [[nodiscard]] inline double      get_num()      const noexcept { return yyjson_get_num(val); }
    [[nodiscard]] inline std::string_view get_str_view() const noexcept {
        return {yyjson_get_str(val), yyjson_get_len(val)};
    }
    [[nodiscard]] inline const char* get_str()      const noexcept { return yyjson_get_str(val); }
    [[nodiscard]] inline size_t      get_len()      const noexcept { return yyjson_get_len(val); }

    [[nodiscard]] inline size_t arr_size() const noexcept { return yyjson_arr_size(val); }
    [[nodiscard]] inline size_t obj_size() const noexcept { return yyjson_obj_size(val); }

    [[nodiscard]] inline Value arr_get(size_t idx) const noexcept {
        return Value(yyjson_arr_get(val, idx));
    }
    [[nodiscard]] inline Value arr_get_first() const noexcept {
        return Value(yyjson_arr_get_first(val));
    }
    [[nodiscard]] inline Value arr_get_last() const noexcept {
        return Value(yyjson_arr_get_last(val));
    }
    [[nodiscard]] inline Value obj_get(const char* key) const noexcept {
        return Value(yyjson_obj_get(val, key));
    }
    [[nodiscard]] inline Value obj_getn(const char* key, size_t klen) const noexcept {
        return Value(yyjson_obj_getn(val, key, klen));
    }
    [[nodiscard]] inline Value obj_get(std::string_view key) const noexcept {
        return Value(yyjson_obj_getn(val, key.data(), key.size()));
    }

    class ArrayIter {
        yyjson_arr_iter iter{};
        yyjson_val*     current{nullptr};
    public:
        inline ArrayIter(yyjson_val* arr, bool end = false) noexcept {
            if (end || !yyjson_is_arr(arr)) { current = nullptr; }
            else {
                yyjson_arr_iter_init(arr, &iter);
                current = yyjson_arr_iter_next(&iter);
            }
        }
        [[nodiscard]] inline Value operator*() const noexcept { return Value(current); }
        inline ArrayIter& operator++() noexcept {
            current = yyjson_arr_iter_next(&iter);
            return *this;
        }
        [[nodiscard]] inline bool operator!=(const ArrayIter& o) const noexcept {
            return current != o.current;
        }
    };
    struct ArrayRange {
        yyjson_val* arr;
        [[nodiscard]] inline ArrayIter begin() const noexcept { return ArrayIter(arr); }
        [[nodiscard]] inline ArrayIter end()   const noexcept { return ArrayIter(arr, true); }
    };
    [[nodiscard]] inline ArrayRange array() const noexcept { return {val}; }

    class ObjectIter {
        yyjson_obj_iter iter{};
        yyjson_val*     key{nullptr};
        yyjson_val*     value{nullptr};
    public:
        inline ObjectIter(yyjson_val* obj, bool end = false) noexcept {
            if (end || !yyjson_is_obj(obj)) { key = nullptr; value = nullptr; }
            else {
                yyjson_obj_iter_init(obj, &iter);
                key   = yyjson_obj_iter_next(&iter);
                value = key ? yyjson_obj_iter_get_val(key) : nullptr;
            }
        }
        [[nodiscard]] inline std::pair<Value, Value> operator*() const noexcept {
            return {Value(key), Value(value)};
        }
        inline ObjectIter& operator++() noexcept {
            key   = yyjson_obj_iter_next(&iter);
            value = key ? yyjson_obj_iter_get_val(key) : nullptr;
            return *this;
        }
        [[nodiscard]] inline bool operator!=(const ObjectIter& o) const noexcept {
            return key != o.key;
        }
    };
    struct ObjectRange {
        yyjson_val* obj;
        [[nodiscard]] inline ObjectIter begin() const noexcept { return ObjectIter(obj); }
        [[nodiscard]] inline ObjectIter end()   const noexcept { return ObjectIter(obj, true); }
    };
    [[nodiscard]] inline ObjectRange object() const noexcept { return {val}; }
};

class MutValue : public Value {
public:
    inline MutValue(yyjson_mut_val* v = nullptr) noexcept
        : Value(reinterpret_cast<yyjson_val*>(v)) {}
    [[nodiscard]] inline yyjson_mut_val* mut_raw() const noexcept {
        return reinterpret_cast<yyjson_mut_val*>(val);
    }

    inline bool obj_add(MutValue key, MutValue value) noexcept {
        return yyjson_mut_obj_add(mut_raw(), key.mut_raw(), value.mut_raw());
    }
    inline bool arr_append(MutValue value) noexcept {
        return yyjson_mut_arr_append(mut_raw(), value.mut_raw());
    }
    inline bool arr_prepend(MutValue value) noexcept {
        return yyjson_mut_arr_prepend(mut_raw(), value.mut_raw());
    }
    inline bool arr_insert(MutValue value, size_t idx) noexcept {
        return yyjson_mut_arr_insert(mut_raw(), value.mut_raw(), idx);
    }

    [[nodiscard]] inline MutValue mut_obj_get(const char* key) const noexcept {
        return MutValue(yyjson_mut_obj_get(mut_raw(), key));
    }
    [[nodiscard]] inline MutValue mut_obj_getn(const char* key, size_t klen) const noexcept {
        return MutValue(yyjson_mut_obj_getn(mut_raw(), key, klen));
    }
    [[nodiscard]] inline MutValue mut_obj_get(std::string_view key) const noexcept {
        return MutValue(yyjson_mut_obj_getn(mut_raw(), key.data(), key.size()));
    }

    inline bool obj_remove_key(const char* key) noexcept {
        return yyjson_mut_obj_remove_key(mut_raw(), key);
    }
    inline bool obj_remove_keyn(const char* key, size_t klen) noexcept {
        return yyjson_mut_obj_remove_keyn(mut_raw(), key, klen);
    }
    inline bool obj_remove_key(std::string_view key) noexcept {
        return yyjson_mut_obj_remove_keyn(mut_raw(), key.data(), key.size());
    }

    [[nodiscard]] inline MutValue mut_arr_get(size_t idx) const noexcept {
        return MutValue(yyjson_mut_arr_get(mut_raw(), idx));
    }
    [[nodiscard]] inline MutValue mut_arr_get_first() const noexcept {
        return MutValue(yyjson_mut_arr_get_first(mut_raw()));
    }
    [[nodiscard]] inline MutValue mut_arr_get_last() const noexcept {
        return MutValue(yyjson_mut_arr_get_last(mut_raw()));
    }

    [[nodiscard]] inline MutValue arr_remove_last() noexcept {
        return MutValue(yyjson_mut_arr_remove_last(mut_raw()));
    }
    [[nodiscard]] inline MutValue arr_remove_first() noexcept {
        return MutValue(yyjson_mut_arr_remove_first(mut_raw()));
    }
    [[nodiscard]] inline MutValue arr_remove(size_t idx) noexcept {
        return MutValue(yyjson_mut_arr_remove(mut_raw(), idx));
    }
    inline bool arr_clear() noexcept {
        return yyjson_mut_arr_clear(mut_raw());
    }

    class MutArrayIter {
        yyjson_mut_arr_iter iter{};
        yyjson_mut_val*     current{nullptr};
    public:
        inline MutArrayIter(yyjson_mut_val* arr, bool end = false) noexcept {
            if (end || !yyjson_mut_is_arr(arr)) { current = nullptr; }
            else {
                yyjson_mut_arr_iter_init(arr, &iter);
                current = yyjson_mut_arr_iter_next(&iter);
            }
        }
        [[nodiscard]] inline MutValue operator*() const noexcept { return MutValue(current); }
        inline MutArrayIter& operator++() noexcept {
            current = yyjson_mut_arr_iter_next(&iter);
            return *this;
        }
        [[nodiscard]] inline bool operator!=(const MutArrayIter& o) const noexcept {
            return current != o.current;
        }
        inline MutValue remove() noexcept {
            return MutValue(yyjson_mut_arr_iter_remove(&iter));
        }
    };
    struct MutArrayRange {
        yyjson_mut_val* arr;
        [[nodiscard]] inline MutArrayIter begin() const noexcept { return MutArrayIter(arr); }
        [[nodiscard]] inline MutArrayIter end()   const noexcept { return MutArrayIter(arr, true); }
    };
    [[nodiscard]] inline MutArrayRange mut_array() const noexcept { return {mut_raw()}; }

    class MutObjectIter {
        yyjson_mut_obj_iter iter{};
        yyjson_mut_val*     key{nullptr};
        yyjson_mut_val*     value{nullptr};
    public:
        inline MutObjectIter(yyjson_mut_val* obj, bool end = false) noexcept {
            if (end || !yyjson_mut_is_obj(obj)) { key = nullptr; value = nullptr; }
            else {
                yyjson_mut_obj_iter_init(obj, &iter);
                key   = yyjson_mut_obj_iter_next(&iter);
                value = key ? yyjson_mut_obj_iter_get_val(key) : nullptr;
            }
        }
        [[nodiscard]] inline std::pair<MutValue, MutValue> operator*() const noexcept {
            return {MutValue(key), MutValue(value)};
        }
        inline MutObjectIter& operator++() noexcept {
            key   = yyjson_mut_obj_iter_next(&iter);
            value = key ? yyjson_mut_obj_iter_get_val(key) : nullptr;
            return *this;
        }
        [[nodiscard]] inline bool operator!=(const MutObjectIter& o) const noexcept {
            return key != o.key;
        }
        inline MutValue remove() noexcept {
            return MutValue(yyjson_mut_obj_iter_remove(&iter));
        }
    };
    struct MutObjectRange {
        yyjson_mut_val* obj;
        [[nodiscard]] inline MutObjectIter begin() const noexcept { return MutObjectIter(obj); }
        [[nodiscard]] inline MutObjectIter end()   const noexcept { return MutObjectIter(obj, true); }
    };
    [[nodiscard]] inline MutObjectRange mut_object() const noexcept { return {mut_raw()}; }
};

class Document {
    yyjson_doc* doc;
public:
    inline Document(yyjson_doc* d = nullptr) noexcept : doc(d) {}
    Document(const Document&) = delete;
    Document& operator=(const Document&) = delete;
    inline Document(Document&& other) noexcept : doc(other.doc) { other.doc = nullptr; }
    inline Document& operator=(Document&& other) noexcept {
        if (this != &other) {
            if (doc) yyjson_doc_free(doc);
            doc = other.doc;
            other.doc = nullptr;
        }
        return *this;
    }
    inline ~Document() noexcept { if (doc) yyjson_doc_free(doc); }

    [[nodiscard]] inline explicit operator bool() const noexcept { return doc != nullptr; }
    [[nodiscard]] inline Value root() const noexcept { return Value(yyjson_doc_get_root(doc)); }
    [[nodiscard]] inline size_t val_count() const noexcept {
        return doc ? yyjson_doc_get_val_count(doc) : 0;
    }

    [[nodiscard]] static inline Document parse(
        std::string_view str,
        ReadFlag         flags = ReadFlag::None,
        yyjson_alc*      alc   = nullptr) noexcept
    {
        return Document(yyjson_read_opts(
            const_cast<char*>(str.data()),
            str.size(),
            static_cast<uint32_t>(flags),
            alc, nullptr));
    }
};

class MutDocument {
    yyjson_mut_doc* doc;
public:
    inline MutDocument(yyjson_alc* alc = nullptr) noexcept {
        doc = yyjson_mut_doc_new(alc);
    }
    MutDocument(const MutDocument&) = delete;
    MutDocument& operator=(const MutDocument&) = delete;
    inline MutDocument(MutDocument&& other) noexcept : doc(other.doc) { other.doc = nullptr; }
    inline MutDocument& operator=(MutDocument&& other) noexcept {
        if (this != &other) {
            if (doc) yyjson_mut_doc_free(doc);
            doc = other.doc;
            other.doc = nullptr;
        }
        return *this;
    }
    inline ~MutDocument() noexcept { if (doc) yyjson_mut_doc_free(doc); }

    [[nodiscard]] inline explicit operator bool() const noexcept { return doc != nullptr; }
    inline void set_root(MutValue root) noexcept {
        yyjson_mut_doc_set_root(doc, root.mut_raw());
    }
    [[nodiscard]] inline MutValue get_root() const noexcept {
        return MutValue(yyjson_mut_doc_get_root(doc));
    }

    [[nodiscard]] inline std::string write(
        WriteFlag   flags = WriteFlag::None,
        yyjson_alc* alc   = nullptr) const noexcept
    {
        size_t len = 0;
        char*  str = yyjson_mut_write_opts(doc, static_cast<uint32_t>(flags), alc, &len, nullptr);
        if (!str) return "";
        std::string res(str, len);
        if (alc && alc->free) alc->free(alc->ctx, str); else free(str);
        return res;
    }

    [[nodiscard]] inline MutValue mut_null()             noexcept { return MutValue(yyjson_mut_null(doc)); }
    [[nodiscard]] inline MutValue mut_bool(bool v)       noexcept { return MutValue(yyjson_mut_bool(doc, v)); }
    [[nodiscard]] inline MutValue mut_int(int64_t v)     noexcept { return MutValue(yyjson_mut_int(doc, v)); }
    [[nodiscard]] inline MutValue mut_uint(uint64_t v)   noexcept { return MutValue(yyjson_mut_uint(doc, v)); }
    [[nodiscard]] inline MutValue mut_real(double v)     noexcept { return MutValue(yyjson_mut_real(doc, v)); }
    [[nodiscard]] inline MutValue mut_obj()              noexcept { return MutValue(yyjson_mut_obj(doc)); }
    [[nodiscard]] inline MutValue mut_arr()              noexcept { return MutValue(yyjson_mut_arr(doc)); }

    [[nodiscard]] inline MutValue mut_str(std::string_view s) noexcept {
        return MutValue(yyjson_mut_strn(doc, s.data(), s.size()));
    }

    [[nodiscard]] inline MutValue mut_strcpy(const char* s) noexcept {
        return s ? MutValue(yyjson_mut_strcpy(doc, s)) : mut_null();
    }

    [[nodiscard]] inline MutValue mut_strncpy(const char* s, size_t len) noexcept {
        return s ? MutValue(yyjson_mut_strncpy(doc, s, len)) : mut_null();
    }

    [[nodiscard]] inline MutValue mut_strdup(const std::string& s) noexcept {
        return MutValue(yyjson_mut_strncpy(doc, s.data(), s.size()));
    }

    [[nodiscard]] inline MutValue mut_copy(Value val) noexcept {
        return MutValue(yyjson_val_mut_copy(doc, val.raw()));
    }

    inline void obj_add_null(MutValue obj, std::string_view key) noexcept {
        obj.obj_add(mut_str(key), mut_null());
    }

    inline void obj_add_bool(MutValue obj, std::string_view key, bool v) noexcept {
        obj.obj_add(mut_str(key), mut_bool(v));
    }

    inline void obj_add_int(MutValue obj, std::string_view key, int64_t v) noexcept {
        obj.obj_add(mut_str(key), mut_int(v));
    }

    inline void obj_add_real(MutValue obj, std::string_view key, double v) noexcept {
        obj.obj_add(mut_str(key), mut_real(v));
    }

    inline void obj_add_str(MutValue obj, std::string_view key, std::string_view v) noexcept {
    obj.obj_add(mut_str(key), MutValue(yyjson_mut_strncpy(doc, v.data(), v.size())));
}
};

}

}
