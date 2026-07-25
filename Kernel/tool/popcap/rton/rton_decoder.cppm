module;
#include <bit>
#include <charconv>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <deque>
#include <span>
#include <string>
#include <string_view>
#include <type_traits>
#include <utility>
#include <vector>
export module tool.popcap.rton.decoder;
import utility.json;
import tool.popcap.rton.utils;

export struct RTONDecodeContext {
    std::vector<std::string_view> nativeIdx;
    std::vector<std::string_view> unicodeIdx;
    
    std::deque<std::string> stringCache;

    RTONDecodeContext() {
        nativeIdx.reserve(2048);
        unicodeIdx.reserve(1024);
    }

    inline void reset() noexcept {
        nativeIdx.clear();
        unicodeIdx.clear();
        stringCache.clear();
    }
};

export class RTONDecoder {
public:
    static std::string decode(std::span<const uint8_t> bytes,
                              std::string_view password = "",
                              bool encrypt = false,
                              RTONUtils::StringEncoding strEnc =
                                  RTONUtils::StringEncoding::UTF8) {
        thread_local RTONDecodeContext ctx;
        ctx.reset();

        std::span<const uint8_t> dataSpan;
        std::vector<uint8_t> decryptedData;

        if (encrypt) [[unlikely]] {
            decryptedData = RTONUtils::decryptBytes(
                bytes, password.empty() ? RTONUtils::DEFAULT_KEY : password);
            dataSpan = decryptedData;
        } else {
            dataSpan = bytes;
        }

        if (dataSpan.size() < 8 ||
            std::memcmp(dataSpan.data(), RTONUtils::RTON_HEADER, 4) != 0)
            return "";

        RTONDecoder dec(dataSpan.data() + 8, dataSpan.size() - 8, ctx, strEnc);

        json::MutDocument doc;
        if (!doc) return "";

        json::MutValue root = doc.mut_obj();
        doc.set_root(root);

        dec.readObject(root, doc);

        if (dec.hasError) return "";

        return doc.write(json::WriteFlag::Pretty);
    }

private:
    const uint8_t*             cur;
    const uint8_t*             end;
    RTONDecodeContext&         ctx;
    RTONUtils::StringEncoding  strEnc;
    bool                       hasError = false;

    explicit RTONDecoder(const uint8_t* data, size_t size,
                         RTONDecodeContext& context,
                         RTONUtils::StringEncoding enc)
        : cur(data), end(data + size), ctx(context), strEnc(enc) {}


    template<class T>
    [[nodiscard]] static T normalizeEndian(T val) noexcept {
        if constexpr (std::endian::native == std::endian::big) {
            if constexpr (std::is_integral_v<T>) {
                return std::byteswap(val);
            } else if constexpr (sizeof(T) == 4) {
                auto raw = std::byteswap(std::bit_cast<uint32_t>(val));
                return std::bit_cast<T>(raw);
            } else if constexpr (sizeof(T) == 8) {
                auto raw = std::byteswap(std::bit_cast<uint64_t>(val));
                return std::bit_cast<T>(raw);
            }
        }
        return val;
    }

    template<typename T>
    [[nodiscard]] inline T readRaw() noexcept {
        if (cur + sizeof(T) > end) [[unlikely]] { hasError = true; return T{}; }
        T val; std::memcpy(&val, cur, sizeof(T));
        cur += sizeof(T);
        val = normalizeEndian(val);
        return val;
    }

    [[nodiscard]] inline uint64_t readUVarInt64() noexcept {
        uint64_t res = 0; int shift = 0; uint8_t b;
        do {
            if (cur >= end) [[unlikely]] { hasError = true; return 0; }
            b = *cur++;
            res |= static_cast<uint64_t>(b & 0x7F) << shift;
            shift += 7;
        } while (b & 0x80);
        return res;
    }

    [[nodiscard]] static inline constexpr int64_t decodeZigZag64(uint64_t v) noexcept {
        return static_cast<int64_t>((v >> 1) ^ -(v & 1));
    }

    [[nodiscard]] inline std::string_view
    decodeStringBytes(const char* ptr, int64_t n) {
        if (strEnc == RTONUtils::StringEncoding::EASCII) {
            bool hasHigh = false;
            for (int64_t i = 0; i < n; ++i) {
                if (static_cast<unsigned char>(ptr[i]) >= 0x80u) {
                    hasHigh = true; break;
                }
            }
            if (hasHigh) {
                ctx.stringCache.push_back(
                    RTONUtils::latin1ToUtf8(std::string_view(ptr, n)));
                return ctx.stringCache.back();
            }
        }
        return std::string_view(ptr, n);
    }

    void readObject(json::MutValue obj, json::MutDocument& doc) {
        while (cur < end && *cur != 0xFF) {
            json::MutValue key = readValue(doc);
            if (!key || cur >= end || *cur == 0xFF) break;
            json::MutValue val = readValue(doc);
            if (key && val && key.is_str()) obj.obj_add(key, val);
            else if (hasError) break;
        }
        if (cur < end && *cur == 0xFF) cur++;
    }

    void readArray(json::MutValue arr, json::MutDocument& doc) {
        if (cur >= end || *cur != 0xFD) return;
        cur++;
        auto count = readUVarInt64();
        for (uint64_t i = 0; i < count; ++i) {
            if (cur >= end) break;
            json::MutValue val = readValue(doc);
            if (val) arr.arr_append(val);
            if (hasError) break;
        }
        if (cur < end && *cur == 0xFE) cur++;
    }

    json::MutValue readValue(json::MutDocument& doc) {
        if (cur >= end) [[unlikely]] return doc.mut_null();
        uint8_t type = *cur++;

        switch (type) {
            case 0x00: return doc.mut_bool(false);
            case 0x01: return doc.mut_bool(true);
            case 0x02: return doc.mut_str("*");
            case 0x85: { json::MutValue o = doc.mut_obj(); readObject(o, doc); return o; }
            case 0x86: { json::MutValue a = doc.mut_arr(); readArray(a, doc);  return a; }

            case 0x08: return doc.mut_int(readRaw<int8_t>());
            case 0x09: return doc.mut_int(0);
            case 0x0A: return doc.mut_int(static_cast<int>(readRaw<uint8_t>()));
            case 0x0B: return doc.mut_int(0);
            case 0x10: return doc.mut_int(readRaw<int16_t>());
            case 0x11: return doc.mut_int(0);
            case 0x12: return doc.mut_uint(readRaw<uint16_t>());
            case 0x13: return doc.mut_int(0);
            case 0x20: return doc.mut_int(readRaw<int32_t>());
            case 0x21: return doc.mut_int(0);
            case 0x22: return doc.mut_real(static_cast<double>(readRaw<float>()));
            case 0x23: return doc.mut_real(0.0);

            case 0x24: return doc.mut_int(static_cast<int32_t>(readUVarInt64()));
            case 0x28: return doc.mut_uint(static_cast<uint32_t>(readUVarInt64()));
            case 0x25: return doc.mut_int(static_cast<int32_t>(decodeZigZag64(readUVarInt64())));
            case 0x26: return doc.mut_uint(static_cast<uint64_t>(readRaw<uint32_t>()));
            case 0x27: return doc.mut_uint(0ULL);

            case 0x40: return doc.mut_int(readRaw<int64_t>());
            case 0x41: return doc.mut_int(0LL);
            case 0x42: return doc.mut_real(readRaw<double>());
            case 0x43: return doc.mut_real(0.0);
            case 0x44: return doc.mut_int(static_cast<int64_t>(readUVarInt64()));
            case 0x48: return doc.mut_uint(readUVarInt64());
            case 0x45: return doc.mut_int(decodeZigZag64(readUVarInt64()));
            case 0x46: return doc.mut_uint(readRaw<uint64_t>());
            case 0x47: return doc.mut_uint(0ULL);

            case 0x81: {
                auto n = readUVarInt64();
                if (cur + n > end) [[unlikely]] { hasError = true; return doc.mut_str(""); }
                if (n == 0) return doc.mut_str("");
                const char* ptr = reinterpret_cast<const char*>(cur); cur += n;
                return doc.mut_str(decodeStringBytes(ptr, n));
            }
            case 0x82: {
                (void)readUVarInt64();               
                auto bsz = readUVarInt64();
                if (cur + bsz > end) [[unlikely]] { hasError = true; return doc.mut_str(""); }
                if (bsz == 0) return doc.mut_str("");
                const char* ptr = reinterpret_cast<const char*>(cur); cur += bsz;
                return doc.mut_str(std::string_view(ptr, bsz));
            }
            case 0x90: {
                auto n = readUVarInt64();
                if (cur + n > end) [[unlikely]] {
                    hasError = true;
                    ctx.nativeIdx.emplace_back("");
                    return doc.mut_str("");
                }
                if (n == 0) {
                    ctx.nativeIdx.emplace_back("");
                    return doc.mut_str("");
                }
                const char* ptr = reinterpret_cast<const char*>(cur); cur += n;
                std::string_view sv = decodeStringBytes(ptr, n);
                ctx.nativeIdx.emplace_back(sv);
                return doc.mut_str(sv);
            }
            case 0x91: {
                auto i = readUVarInt64();
                if (static_cast<size_t>(i) < ctx.nativeIdx.size()) [[likely]]
                    return doc.mut_str(ctx.nativeIdx[i]);
                return doc.mut_str("");
            }
            case 0x92: {
                (void)readUVarInt64();               
                auto bsz = readUVarInt64();
                if (cur + bsz > end) [[unlikely]] {
                    hasError = true;
                    ctx.unicodeIdx.emplace_back("");
                    return doc.mut_str("");
                }
                if (bsz == 0) {
                    ctx.unicodeIdx.emplace_back("");
                    return doc.mut_str("");
                }
                const char* ptr = reinterpret_cast<const char*>(cur); cur += bsz;
                std::string_view sv(ptr, bsz);
                ctx.unicodeIdx.emplace_back(sv);
                return doc.mut_str(sv);
            }
            case 0x93: {
                auto i = readUVarInt64();
                if (static_cast<size_t>(i) < ctx.unicodeIdx.size()) [[likely]]
                    return doc.mut_str(ctx.unicodeIdx[i]);
                return doc.mut_str("");
            }
            
            case 0x83: return readRTID(doc);
            case 0x84: return doc.mut_str("RTID(0)");
            
            case 0x87: {
                if (cur >= end) [[unlikely]] { hasError = true; return doc.mut_str(""); }
                cur++;
                auto len = readUVarInt64();
                if (cur + len > end) [[unlikely]] { hasError = true; return doc.mut_str(""); }
                std::string_view bin(reinterpret_cast<const char*>(cur), len);
                cur += len;
                auto i = readUVarInt64();
                std::string res = "$BINARY(\"";
                res += bin;
                res += "\", ";
                res += std::to_string(i);
                res += ")";
                return doc.mut_strdup(res);
            }
            
            case 0xBC: return doc.mut_bool(readRaw<uint8_t>() != 0);

            default: return doc.mut_str("UNKNOWN_TYPE");
        }
    }

    json::MutValue readRTID(json::MutDocument& doc) {
        if (cur >= end) [[unlikely]] return doc.mut_str("RTID(0)");
        uint8_t t = readRaw<uint8_t>();

        switch (t) {
            case 0x01: {
                auto mid = readUVarInt64(); 
                auto fst = readUVarInt64();
                if (cur + 4 > end) [[unlikely]] { hasError = true; return doc.mut_str("RTID(0)"); }
                uint32_t lst = readRaw<uint32_t>();
                
                char hex[64];
                int len = std::snprintf(hex, sizeof(hex), "RTID(%lld.%lld.%08x@)", 
                              static_cast<long long>(fst), 
                              static_cast<long long>(mid), 
                              static_cast<unsigned int>(lst));
                return doc.mut_strncpy(hex, len);
            }
            case 0x02: {
                (void)readUVarInt64();
                auto sbs = readUVarInt64();
                if (cur + sbs > end) [[unlikely]] { hasError = true; return doc.mut_str("RTID(0)"); }
                std::string_view sheet(reinterpret_cast<const char*>(cur), sbs);
                cur += sbs;

                auto mid = readUVarInt64(); 
                auto fst = readUVarInt64();
                if (cur + 4 > end) [[unlikely]] { hasError = true; return doc.mut_str("RTID(0)"); }
                uint32_t lst = readRaw<uint32_t>();

                char hex[64];
                std::snprintf(hex, sizeof(hex), "RTID(%lld.%lld.%08x@", 
                              static_cast<long long>(fst), 
                              static_cast<long long>(mid), 
                              static_cast<unsigned int>(lst));
                std::string res = hex;
                res += sheet;
                res += ")";
                return doc.mut_strdup(res);
            }
            case 0x03: {
                (void)readUVarInt64(); 
                auto sbs = readUVarInt64();
                if (cur + sbs > end) [[unlikely]] { hasError = true; return doc.mut_str("RTID(0)"); }
                std::string_view sheet(reinterpret_cast<const char*>(cur), sbs);
                cur += sbs;

                (void)readUVarInt64(); 
                auto abs_ = readUVarInt64();
                if (cur + abs_ > end) [[unlikely]] { hasError = true; return doc.mut_str("RTID(0)"); }
                std::string_view alias(reinterpret_cast<const char*>(cur), abs_);
                cur += abs_;

                std::string res = "RTID(";
                res += alias;
                res += "@";
                res += sheet;
                res += ")";
                return doc.mut_strdup(res);
            }
            default:
                return doc.mut_str("RTID(0)");
        }
    }
};
