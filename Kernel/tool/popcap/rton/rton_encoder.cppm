module;
#include <algorithm>
#include <bit>
#include <charconv>
#include <cstdint>
#include <cstddef>
#include <cstring>
#include <deque>
#include <span>
#include <string>
#include <string_view>
#include <type_traits>
#include <utility>
#include <vector>
#if defined(__ARM_NEON) || defined(__aarch64__)
#include <arm_neon.h>
#endif
export module tool.popcap.rton.encoder;
import utility.json;
import tool.popcap.rton.utils;

export class RawBuffer {
    std::vector<uint8_t> data_;
    size_t pos_ = 0;
public:
    RawBuffer() { data_.resize(16384); }

    void reset() noexcept { pos_ = 0; }

    void ensure(size_t n) {
        const auto required = pos_ + n;
        if (required > data_.size()) data_.resize(std::max(data_.size() * 2, required + 8192));
    }


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
    void writeRaw(T val) noexcept {
        ensure(sizeof(T));
        val = normalizeEndian(val);
        std::memcpy(data_.data() + pos_, &val, sizeof(T));
        pos_ += sizeof(T);
    }

    void write8(uint8_t val) noexcept {
        ensure(1);
        data_[pos_++] = val;
    }

    void writeBytes(const void* src, size_t len) noexcept {
        if (!len) return;
        ensure(len);
        std::memcpy(data_.data() + pos_, src, len);
        pos_ += len;
    }

    void writeVarInt32(int32_t value) noexcept { writeVarInt64(static_cast<uint32_t>(value)); }

    void writeVarInt64(uint64_t uvalue) noexcept {
        ensure(10);
        while (uvalue >= 0x80) {
            data_[pos_++] = static_cast<uint8_t>((uvalue & 0x7F) | 0x80);
            uvalue >>= 7;
        }
        data_[pos_++] = static_cast<uint8_t>(uvalue);
    }

    [[nodiscard]] std::vector<uint8_t> toVector() const {
        return {data_.begin(), data_.begin() + static_cast<std::ptrdiff_t>(pos_)};
    }
};

export class EpochStringMap {
    struct Entry { std::string_view key; uint32_t id; uint32_t epoch; };
    std::vector<Entry> table_;
    size_t   mask_;
    uint32_t current_epoch_;
    uint32_t count_;

    [[nodiscard]] static inline uint32_t hash(std::string_view s) noexcept {
        uint64_t h = 0xcbf29ce484222325ULL;
        for (char c : s) { h ^= static_cast<uint8_t>(c); h *= 0x100000001b3ULL; }
        return static_cast<uint32_t>(h ^ (h >> 32));
    }

public:
    EpochStringMap(size_t capacity = 2048) : current_epoch_(1), count_(0) {
        size_t p2 = 1; while (p2 < capacity) p2 *= 2;
        table_.resize(p2, Entry{{}, 0, 0});
        mask_ = p2 - 1;
    }

    inline void clear() noexcept {
        current_epoch_++;
        count_ = 0;
        if (current_epoch_ == 0) [[unlikely]] {
            current_epoch_ = 1;
            std::memset(table_.data(), 0, table_.size() * sizeof(Entry));
        }
    }

    bool findOrInsert(std::string_view s, uint32_t id, uint32_t& outId) {
        if (count_ * 2 >= table_.size()) [[unlikely]] {
            std::vector<Entry> oldTable = std::move(table_);
            table_.assign(oldTable.size() * 2, Entry{{}, 0, 0});
            mask_ = table_.size() - 1;
            for (const auto& e : oldTable) {
                if (e.epoch == current_epoch_) {
                    uint32_t i = hash(e.key) & mask_;
                    while (table_[i].epoch == current_epoch_) i = (i + 1) & mask_;
                    table_[i] = e;
                }
            }
        }

        uint32_t i = hash(s) & mask_;
        while (table_[i].epoch == current_epoch_) {
            if (table_[i].key == s) { outId = table_[i].id; return true; }
            i = (i + 1) & mask_;
        }
        table_[i] = {s, id, current_epoch_};
        count_++;
        return false;
    }
};

export struct RTONEncodeContext {
    RawBuffer      buffer;
    EpochStringMap nativeIdx;
    EpochStringMap unicodeIdx;
    std::deque<std::string> stringCache;

    RTONEncodeContext() : nativeIdx(2048), unicodeIdx(1024) {}
    
    inline void reset() noexcept {
        buffer.reset();
        nativeIdx.clear();
        unicodeIdx.clear();
        stringCache.clear();
    }
};

export namespace RTON_SIMD {
    [[nodiscard]] inline bool isAsciiUltra(std::string_view s) noexcept {
        const uint8_t* p   = reinterpret_cast<const uint8_t*>(s.data());
        size_t         len = s.size();
#if defined(__ARM_NEON) || defined(__aarch64__)
        uint8x16_t mask = vdupq_n_u8(0);
        while (len >= 16) { mask = vorrq_u8(mask, vld1q_u8(p)); p += 16; len -= 16; }
        if (vmaxvq_u8(mask) >= 0x80) return false;
#else
        while (len >= 8) {
            uint64_t v; std::memcpy(&v, p, 8);
            if (v & 0x8080808080808080ULL) return false;
            p += 8; len -= 8;
        }
#endif
        while (len--) { if (*p++ >= 0x80u) return false; }
        return true;
    }

    [[nodiscard]] inline size_t utf16CharCount(std::string_view s) noexcept {
        size_t count = 0;
        for (size_t i = 0; i < s.size(); ) {
            unsigned char c = s[i];
            if      (c < 0x80) { i += 1; count += 1; }
            else if (c < 0xE0) { i += 2; count += 1; }
            else if (c < 0xF0) { i += 3; count += 1; }
            else               { i += 4; count += 2; }
        }
        return count;
    }
}


export class RTONEncoder {
public:
    static std::vector<uint8_t> encode(std::string_view json_str,
                                       std::string_view password = "",
                                       bool encrypt = false,
                                       RTONUtils::StringEncoding strEnc =
                                           RTONUtils::StringEncoding::UTF8) {
        thread_local RTONEncodeContext ctx;
        ctx.reset();

        std::string jsonInput(json_str);
        json::Document doc = json::Document::parse(jsonInput, json::ReadFlag::Insitu);
        if (!doc) return {};

        json::Value root = doc.root();
        if (!root || !root.is_obj()) return {};

        ctx.buffer.writeBytes(RTONUtils::RTON_HEADER, 8);

        RTONEncoder enc(ctx, strEnc);
        enc.writeObject(root);

        ctx.buffer.writeBytes(RTONUtils::DONE_FOOTER, 4);

        if (encrypt) [[unlikely]] {
            auto encryptedBytes = RTONUtils::encryptBytes(
                ctx.buffer.toVector(),
                password.empty() ? RTONUtils::DEFAULT_KEY : password);
            return encryptedBytes.empty() ? std::vector<uint8_t>{} : encryptedBytes;
        }

        return ctx.buffer.toVector();
    }

private:
    RTONEncodeContext&         ctx_;
    RTONUtils::StringEncoding  strEnc_;
    uint32_t                   nativeCount_  = 0;
    uint32_t                   unicodeCount_ = 0;

    explicit RTONEncoder(RTONEncodeContext& ctx,
                         RTONUtils::StringEncoding enc)
        : ctx_(ctx), strEnc_(enc) {}

    [[nodiscard]] static inline constexpr uint64_t encodeZigZag64(int64_t v) noexcept {
        return (static_cast<uint64_t>(v) << 1) ^ static_cast<uint64_t>(v >> 63);
    }
    [[nodiscard]] static inline constexpr uint32_t encodeZigZag32(int32_t v) noexcept {
        return (static_cast<uint32_t>(v) << 1) ^ static_cast<uint32_t>(v >> 31);
    }

    void writeString(std::string_view s) {
        if (!s.empty()) [[likely]] {
            switch (s.front()) {
                case '*': [[unlikely]] if (s.size() == 1) { ctx_.buffer.write8(0x02); return; } break;
                case 'R': [[unlikely]] if (writeRTID(s)) return; break;
                case '$': [[unlikely]] if (writeBinary(s)) return; break;
                default: break;
            }
        }

        if (strEnc_ == RTONUtils::StringEncoding::EASCII) {
            writeStringEascii(s);
        } else {
            writeStringUtf8(s);
        }
    }

    void writeStringUtf8(std::string_view s) {
        uint32_t foundId;
        if (RTON_SIMD::isAsciiUltra(s)) [[likely]] {
            if (ctx_.nativeIdx.findOrInsert(s, nativeCount_, foundId)) {
                ctx_.buffer.write8(0x91);
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(foundId));
            } else {
                ctx_.buffer.write8(0x90);
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(s.size()));
                ctx_.buffer.writeBytes(s.data(), s.size());
                nativeCount_++;
            }
        } else {
            if (ctx_.unicodeIdx.findOrInsert(s, unicodeCount_, foundId)) {
                ctx_.buffer.write8(0x93);
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(foundId));
            } else {
                ctx_.buffer.write8(0x92);
                ctx_.buffer.writeVarInt32(
                    static_cast<int32_t>(RTON_SIMD::utf16CharCount(s)));
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(s.size()));
                ctx_.buffer.writeBytes(s.data(), s.size());
                unicodeCount_++;
            }
        }
    }

    void writeStringEascii(std::string_view s) {
        uint32_t foundId;
        if (RTON_SIMD::isAsciiUltra(s)) [[likely]] {
            if (ctx_.nativeIdx.findOrInsert(s, nativeCount_, foundId)) {
                ctx_.buffer.write8(0x91);
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(foundId));
            } else {
                ctx_.buffer.write8(0x90);
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(s.size()));
                ctx_.buffer.writeBytes(s.data(), s.size());
                nativeCount_++;
            }
        } else {
            if (ctx_.nativeIdx.findOrInsert(s, nativeCount_, foundId)) {
                ctx_.buffer.write8(0x91);
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(foundId));
            } else {
                ctx_.stringCache.push_back(RTONUtils::utf8ToLatin1(s));
                std::string_view latin1sv = ctx_.stringCache.back();
                ctx_.buffer.write8(0x90);
                ctx_.buffer.writeVarInt32(static_cast<int32_t>(latin1sv.size()));
                ctx_.buffer.writeBytes(latin1sv.data(), latin1sv.size());
                nativeCount_++;
            }
        }
    }

    void writeObject(json::Value obj) {
        for (auto [key, val] : obj.object()) {
            writeString(key.get_str_view());
            writeValue(val);
        }
        ctx_.buffer.write8(0xFF);
    }

    void writeArray(json::Value arr) {
        ctx_.buffer.write8(0xFD);
        ctx_.buffer.writeVarInt64(arr.arr_size());
        for (auto val : arr.array()) writeValue(val);
        ctx_.buffer.write8(0xFE);
    }

    void writeValue(json::Value v) {
        switch (v.type()) {
            case json::Type::Obj:  ctx_.buffer.write8(0x85); writeObject(v); break;
            case json::Type::Arr:  ctx_.buffer.write8(0x86); writeArray(v);  break;
            case json::Type::Str:  writeString(v.get_str_view()); break;
            case json::Type::Num:  writeNumber(v); break;
            case json::Type::Bool: ctx_.buffer.write8(v.get_bool() ? 0x01 : 0x00); break;
            case json::Type::Null: ctx_.buffer.write8(0x84); break;
            case json::Type::Raw:  break;
            case json::Type::None: break;
            default: std::unreachable();
        }
    }

    void writeNumber(json::Value v) {
        switch (v.num_subtype()) {
            case json::NumSubtype::Real: {
                double f64 = v.get_real();
                if (f64 == 0.0) {
                    ctx_.buffer.write8(0x23);
                } else {
                    float f32 = static_cast<float>(f64);
                    if (static_cast<double>(f32) == f64) [[likely]] {
                        ctx_.buffer.write8(0x22);
                        ctx_.buffer.writeRaw<float>(f32);
                    } else {
                        ctx_.buffer.write8(0x42);
                        ctx_.buffer.writeRaw<double>(f64);
                    }
                }
                break;
            }
            case json::NumSubtype::Uint: {
                uint64_t val = v.get_uint();
                if (val <= 2147483647ULL) {
                    if (val == 0) ctx_.buffer.write8(0x21);
                    else {
                        ctx_.buffer.write8(0x24);
                        ctx_.buffer.writeVarInt32(static_cast<int32_t>(val));
                    }
                } else if (val <= 9223372036854775807ULL) {
                    ctx_.buffer.write8(0x44);
                    ctx_.buffer.writeVarInt64(static_cast<int64_t>(val));
                } else {
                    ctx_.buffer.write8(0x46);
                    ctx_.buffer.writeRaw<uint64_t>(val);
                }
                break;
            }
            case json::NumSubtype::Sint: {
                int64_t val = v.get_sint();
                if (val == 0) {
                    ctx_.buffer.write8(0x21);
                } else if (val > 0) {
                    if (val <= 2147483647LL) {
                        ctx_.buffer.write8(0x24);
                        ctx_.buffer.writeVarInt32(static_cast<int32_t>(val));
                    } else {
                        ctx_.buffer.write8(0x44);
                        ctx_.buffer.writeVarInt64(val);
                    }
                } else {
                    if (val + 0x40000000LL >= 0) {
                        ctx_.buffer.write8(0x25);
                        ctx_.buffer.writeVarInt32(
                            static_cast<int32_t>(encodeZigZag32(static_cast<int32_t>(val))));
                    } else {
                        ctx_.buffer.write8(0x45);
                        ctx_.buffer.writeVarInt64(encodeZigZag64(val));
                    }
                }
                break;
            }
            default: std::unreachable();
        }
    }

    bool writeBinary(std::string_view s) {
        if (s.size() < 13 || !s.starts_with("$BINARY(\"") ||
            s.back() != ')') [[likely]] return false;
        auto mid = s.rfind("\", ");
        if (mid == std::string_view::npos || mid < 9 || mid + 3 > s.size() - 1)
            return false;

        std::string_view bin = s.substr(9, mid - 9);
        std::string_view num = s.substr(mid + 3, s.size() - mid - 4);

        int n = 0;
        auto r = std::from_chars(num.data(), num.data() + num.size(), n);
        if (r.ec == std::errc() && r.ptr == num.data() + num.size()) {
            ctx_.buffer.write8(0x87); ctx_.buffer.write8(0x00);
            ctx_.buffer.writeVarInt64(bin.size());
            ctx_.buffer.writeBytes(bin.data(), bin.size());
            ctx_.buffer.writeVarInt32(n);
            return true;
        }
        return false;
    }

    bool writeRTID(std::string_view s) {
        if (s.size() < 7 || !s.starts_with("RTID(") ||
            s.back() != ')') [[likely]] return false;
        if (s == "RTID(0)") { ctx_.buffer.write8(0x84); return true; }

        std::string_view content = s.substr(5, s.size() - 6);
        auto atPos = content.find('@');
        if (atPos == std::string_view::npos) { ctx_.buffer.write8(0x84); return true; }

        auto id   = content.substr(0, atPos);
        auto name = content.substr(atPos + 1);
        auto d1   = id.find('.');
        auto d2   = (d1 != std::string_view::npos) ? id.find('.', d1 + 1)
                                                    : std::string_view::npos;

        if (d1 != std::string_view::npos && d2 != std::string_view::npos) {
            int32_t mv = 0, fv = 0; uint32_t lv = 0;
            auto r1 = std::from_chars(id.data(), id.data() + d1, fv);
            auto r2 = std::from_chars(id.data() + d1 + 1, id.data() + d2, mv);
            auto r3 = std::from_chars(id.data() + d2 + 1, id.data() + id.size(), lv, 16);

            if (r1.ec == std::errc() && r1.ptr == id.data() + d1 &&
                r2.ec == std::errc() && r2.ptr == id.data() + d2 &&
                r3.ec == std::errc() && r3.ptr == id.data() + id.size()) {
                ctx_.buffer.write8(0x83); ctx_.buffer.write8(0x02);
                ctx_.buffer.writeVarInt64(RTON_SIMD::utf16CharCount(name));
                ctx_.buffer.writeVarInt64(name.size());
                ctx_.buffer.writeBytes(name.data(), name.size());
                ctx_.buffer.writeVarInt32(mv);
                ctx_.buffer.writeVarInt32(fv);
                ctx_.buffer.writeRaw<uint32_t>(lv);
                return true;
            }
        }

        ctx_.buffer.write8(0x83); ctx_.buffer.write8(0x03);
        ctx_.buffer.writeVarInt64(RTON_SIMD::utf16CharCount(name));
        ctx_.buffer.writeVarInt64(name.size());
        ctx_.buffer.writeBytes(name.data(), name.size());
        ctx_.buffer.writeVarInt64(RTON_SIMD::utf16CharCount(id));
        ctx_.buffer.writeVarInt64(id.size());
        ctx_.buffer.writeBytes(id.data(), id.size());
        return true;
    }
};
