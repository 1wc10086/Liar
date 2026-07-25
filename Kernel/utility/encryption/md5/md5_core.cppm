module;
#include <array>
#include <bit>
#include <cstddef>
#include <cstdint>
#include <cstring>
export module utility.md5.core;

export class MD5Calculator {
public:
    MD5Calculator() noexcept { reset(); }

    void reset() noexcept {
        state_ = kInitState;
        count_ = 0;
        buffer_.fill(0);
    }

    void update(const uint8_t* input, size_t length) noexcept {
        static_assert(std::endian::native == std::endian::little);

        size_t index   = static_cast<size_t>(count_ & 0x3F);
        count_        += length;
        size_t partLen = 64 - index;
        size_t i       = 0;

        if (length >= partLen) {
            std::memcpy(&buffer_[index], input, partLen);
            transform(buffer_.data());
            for (i = partLen; i + 63 < length; i += 64)
                transform(&input[i]);
            index = 0;
        }
        std::memcpy(&buffer_[index], &input[i], length - i);
    }

    void finalize(uint8_t digest[16]) noexcept {
        const uint64_t bitCount = count_ * 8;
        uint8_t bits[8];
        for (int i = 0; i < 8; ++i)
            bits[i] = static_cast<uint8_t>(bitCount >> (i * 8));
            
        const size_t index  = static_cast<size_t>(count_ & 0x3F);
        const size_t padLen = (index < 56) ? (56 - index) : (120 - index);

        std::array<uint8_t, 64> padding{};
        padding[0] = 0x80;
        update(padding.data(), padLen);
        update(bits, 8);

        for (int i = 0; i < 4; ++i) {
            digest[i * 4]     = static_cast<uint8_t>( state_[i]        & 0xFF);
            digest[i * 4 + 1] = static_cast<uint8_t>((state_[i] >>  8) & 0xFF);
            digest[i * 4 + 2] = static_cast<uint8_t>((state_[i] >> 16) & 0xFF);
            digest[i * 4 + 3] = static_cast<uint8_t>((state_[i] >> 24) & 0xFF);
        }
    }

private:
    static constexpr std::array<uint32_t, 4> kInitState = {
        0x67452301u, 0xEFCDAB89u, 0x98BADCFEu, 0x10325476u
    };

    std::array<uint32_t, 4> state_;
    uint64_t                count_;
    std::array<uint8_t, 64> buffer_;

    [[nodiscard]] static constexpr uint32_t F(uint32_t x, uint32_t y, uint32_t z) noexcept {
        return (x & y) | (~x & z);
    }
    [[nodiscard]] static constexpr uint32_t G(uint32_t x, uint32_t y, uint32_t z) noexcept {
        return (x & z) | (y & ~z);
    }
    [[nodiscard]] static constexpr uint32_t H(uint32_t x, uint32_t y, uint32_t z) noexcept {
        return x ^ y ^ z;
    }
    [[nodiscard]] static constexpr uint32_t I(uint32_t x, uint32_t y, uint32_t z) noexcept {
        return y ^ (x | ~z);
    }

    template<auto Fn>
    static constexpr void step(uint32_t& a, uint32_t b, uint32_t c, uint32_t d,
                                uint32_t  x, int       s, uint32_t t) noexcept {
        a += Fn(b, c, d) + x + t;
        a  = std::rotl(a, s);
        a += b;
    }

    void transform(const uint8_t blk[64]) noexcept {
        uint32_t x[16];
        std::memcpy(x, blk, 64);

        auto [a, b, c, d] = state_;

        step<F>(a,b,c,d, x[ 0], 7, 0xD76AA478u); step<F>(d,a,b,c, x[ 1],12, 0xE8C7B756u);
        step<F>(c,d,a,b, x[ 2],17, 0x242070DBu); step<F>(b,c,d,a, x[ 3],22, 0xC1BDCEEEu);
        step<F>(a,b,c,d, x[ 4], 7, 0xF57C0FAFu); step<F>(d,a,b,c, x[ 5],12, 0x4787C62Au);
        step<F>(c,d,a,b, x[ 6],17, 0xA8304613u); step<F>(b,c,d,a, x[ 7],22, 0xFD469501u);
        step<F>(a,b,c,d, x[ 8], 7, 0x698098D8u); step<F>(d,a,b,c, x[ 9],12, 0x8B44F7AFu);
        step<F>(c,d,a,b, x[10],17, 0xFFFF5BB1u); step<F>(b,c,d,a, x[11],22, 0x895CD7BEu);
        step<F>(a,b,c,d, x[12], 7, 0x6B901122u); step<F>(d,a,b,c, x[13],12, 0xFD987193u);
        step<F>(c,d,a,b, x[14],17, 0xA679438Eu); step<F>(b,c,d,a, x[15],22, 0x49B40821u);

        step<G>(a,b,c,d, x[ 1], 5, 0xF61E2562u); step<G>(d,a,b,c, x[ 6], 9, 0xC040B340u);
        step<G>(c,d,a,b, x[11],14, 0x265E5A51u); step<G>(b,c,d,a, x[ 0],20, 0xE9B6C7AAu);
        step<G>(a,b,c,d, x[ 5], 5, 0xD62F105Du); step<G>(d,a,b,c, x[10], 9, 0x02441453u);
        step<G>(c,d,a,b, x[15],14, 0xD8A1E681u); step<G>(b,c,d,a, x[ 4],20, 0xE7D3FBC8u);
        step<G>(a,b,c,d, x[ 9], 5, 0x21E1CDE6u); step<G>(d,a,b,c, x[14], 9, 0xC33707D6u);
        step<G>(c,d,a,b, x[ 3],14, 0xF4D50D87u); step<G>(b,c,d,a, x[ 8],20, 0x455A14EDu);
        step<G>(a,b,c,d, x[13], 5, 0xA9E3E905u); step<G>(d,a,b,c, x[ 2], 9, 0xFCEFA3F8u);
        step<G>(c,d,a,b, x[ 7],14, 0x676F02D9u); step<G>(b,c,d,a, x[12],20, 0x8D2A4C8Au);
        
        step<H>(a,b,c,d, x[ 5], 4, 0xFFFA3942u); step<H>(d,a,b,c, x[ 8],11, 0x8771F681u);
        step<H>(c,d,a,b, x[11],16, 0x6D9D6122u); step<H>(b,c,d,a, x[14],23, 0xFDE5380Cu);
        step<H>(a,b,c,d, x[ 1], 4, 0xA4BEEA44u); step<H>(d,a,b,c, x[ 4],11, 0x4BDECFA9u);
        step<H>(c,d,a,b, x[ 7],16, 0xF6BB4B60u); step<H>(b,c,d,a, x[10],23, 0xBEBFBC70u);
        step<H>(a,b,c,d, x[13], 4, 0x289B7EC6u); step<H>(d,a,b,c, x[ 0],11, 0xEAA127FAu);
        step<H>(c,d,a,b, x[ 3],16, 0xD4EF3085u); step<H>(b,c,d,a, x[ 6],23, 0x04881D05u);
        step<H>(a,b,c,d, x[ 9], 4, 0xD9D4D039u); step<H>(d,a,b,c, x[12],11, 0xE6DB99E5u);
        step<H>(c,d,a,b, x[15],16, 0x1FA27CF8u); step<H>(b,c,d,a, x[ 2],23, 0xC4AC5665u);

        step<I>(a,b,c,d, x[ 0], 6, 0xF4292244u); step<I>(d,a,b,c, x[ 7],10, 0x432AFF97u);
        step<I>(c,d,a,b, x[14],15, 0xAB9423A7u); step<I>(b,c,d,a, x[ 5],21, 0xFC93A039u);
        step<I>(a,b,c,d, x[12], 6, 0x655B59C3u); step<I>(d,a,b,c, x[ 3],10, 0x8F0CCC92u);
        step<I>(c,d,a,b, x[10],15, 0xFFEFF47Du); step<I>(b,c,d,a, x[ 1],21, 0x85845DD1u);
        step<I>(a,b,c,d, x[ 8], 6, 0x6FA87E4Fu); step<I>(d,a,b,c, x[15],10, 0xFE2CE6E0u);
        step<I>(c,d,a,b, x[ 6],15, 0xA3014314u); step<I>(b,c,d,a, x[13],21, 0x4E0811A1u);
        step<I>(a,b,c,d, x[ 4], 6, 0xF7537E82u); step<I>(d,a,b,c, x[11],10, 0xBD3AF235u);
        step<I>(c,d,a,b, x[ 2],15, 0x2AD7D2BBu); step<I>(b,c,d,a, x[ 9],21, 0xEB86D391u);

        state_[0] += a;
        state_[1] += b;
        state_[2] += c;
        state_[3] += d;
    }
};
