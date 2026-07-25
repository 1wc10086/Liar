module;
#include <algorithm>
#include <bit>
#include <cstdint>
#include <cstring>
#include <fstream>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>
export module utility.binary.unified_binary_stream;
export {
#define UBS_INLINE    [[clang::always_inline]] inline

#define UBS_COLD      [[clang::noinline]] [[gnu::cold]]

#define UBS_LIKELY(x)   __builtin_expect(!!(x), 1)
#define UBS_UNLIKELY(x) __builtin_expect(!!(x), 0)

#define UBS_PREFETCH_R(ptr) __builtin_prefetch((ptr), 0, 1)

class UnifiedBinaryStream {
public:
    enum class Endian : uint8_t { Little, Big  };
    enum class Mode   : uint8_t { Read,  Write };

    explicit UnifiedBinaryStream(Mode m = Mode::Write,
                                 Endian e = Endian::Little) noexcept
        : readPtr(nullptr), readSize(0), position(0),
          endian(e), mode(m), hasError(false) {}

    UnifiedBinaryStream(std::span<const uint8_t> data,
                        Endian e = Endian::Little) noexcept
        : readPtr(data.data()), readSize(data.size()), position(0),
          endian(e), mode(Mode::Read), hasError(false) {}

    UnifiedBinaryStream(const uint8_t* data, size_t size,
                        Endian e = Endian::Little) noexcept
        : UnifiedBinaryStream(std::span<const uint8_t>(data, size), e) {}

    explicit UnifiedBinaryStream(const std::string& filename,
                                 Endian e = Endian::Little)
        : readPtr(nullptr), readSize(0), position(0),
          endian(e), mode(Mode::Read), hasError(false) {
        std::ifstream file(filename, std::ios::binary | std::ios::ate);
        if (UBS_LIKELY(file.is_open())) {
            const size_t sz = static_cast<size_t>(file.tellg());
            file.seekg(0, std::ios::beg);
            writeBuffer.resize(sz);
            file.read(reinterpret_cast<char*>(writeBuffer.data()), sz);
            readPtr  = writeBuffer.data();
            readSize = sz;
        } else {
            hasError = true;
        }
    }

    void setEndian(Endian e) noexcept { endian = e; }
    [[nodiscard]] UBS_INLINE Endian getEndian() const noexcept { return endian; }

    void setMode(Mode m) noexcept { mode = m; }
    [[nodiscard]] UBS_INLINE Mode getMode() const noexcept { return mode; }

    [[nodiscard]] UBS_INLINE size_t getPosition() const noexcept { return position; }
    void setPosition(size_t pos) noexcept { position = pos; }

    [[nodiscard]] UBS_INLINE size_t getLength() const noexcept {
        return (mode == Mode::Read) ? readSize : writeBuffer.size();
    }

    [[nodiscard]] UBS_INLINE bool hasErrorOccurred() const noexcept { return hasError; }
    void clearError() noexcept { hasError = false; }

    void setReadBuffer(std::span<const uint8_t> data) noexcept {
        mode     = Mode::Read;
        readPtr  = data.data();
        readSize = data.size();
        position = 0;
        hasError = false;
    }
    void setReadBuffer(const uint8_t* data, size_t size) noexcept {
        setReadBuffer(std::span<const uint8_t>(data, size));
    }
    void setReadBuffer(const std::vector<uint8_t>& data) noexcept {
        setReadBuffer(std::span<const uint8_t>(data));
    }

    void backupHeader(size_t headerSize) {
        const uint8_t* ptr = (mode == Mode::Read) ? readPtr : writeBuffer.data();
        if (getLength() >= headerSize)
            headerBackup.assign(ptr, ptr + headerSize);
    }
    [[nodiscard]] const std::vector<uint8_t>& getHeaderBackup() const noexcept {
        return headerBackup;
    }

    void reserveHeader(size_t headerSize) {
        if (mode == Mode::Write && position == 0) {
            writeBuffer.resize(headerSize, 0);
            position = headerSize;
        }
    }

    [[nodiscard]] UBS_INLINE uint8_t peekUInt8(size_t pos) const noexcept {
        const uint8_t* p = (mode == Mode::Read) ? readPtr : writeBuffer.data();
        return (pos >= getLength()) ? 0 : p[pos];
    }
    [[nodiscard]] UBS_INLINE const uint8_t* getBufferPtr() const noexcept {
        return (mode == Mode::Read) ? readPtr : writeBuffer.data();
    }
    [[nodiscard]] UBS_INLINE bool checkBounds(size_t pos, size_t required) const noexcept {
        return pos + required <= getLength();
    }

    [[nodiscard]] std::vector<uint8_t> readBytes(size_t size) {
        if (UBS_UNLIKELY(mode != Mode::Read || !canRead(size))) {
            return setErrorRet<std::vector<uint8_t>>();
        }
        std::vector<uint8_t> result(readPtr + position, readPtr + position + size);
        position += size;
        return result;
    }

    [[nodiscard]] UBS_INLINE std::span<const uint8_t> readSpan(size_t size) noexcept {
        if (UBS_UNLIKELY(mode != Mode::Read || !canRead(size))) {
            hasError = true;
            return {};
        }
        std::span<const uint8_t> result(readPtr + position, size);
        position += size;
        return result;
    }

    void verifyBytes(std::span<const uint8_t> expected) {
        auto actual = readBytes(expected.size());
        if (hasError) return;
        if (UBS_UNLIKELY(!std::ranges::equal(actual, expected))) {
            hasError = true;
            throw std::runtime_error("Byte verification failed");
        }
    }
    void verifyBytes(const uint8_t* expected, size_t size) {
        verifyBytes(std::span<const uint8_t>(expected, size));
    }

    [[nodiscard]] UBS_INLINE int8_t readInt8() {
        return static_cast<int8_t>(readUInt8());
    }

    [[nodiscard]] UBS_INLINE uint8_t readUInt8() {
        if (UBS_UNLIKELY(!canRead(1))) return setErrorRet<uint8_t>();
        return readPtr[position++];
    }

    [[nodiscard]] UBS_INLINE int16_t readInt16() {
        return static_cast<int16_t>(readUInt16());
    }

    [[nodiscard]] UBS_INLINE uint16_t readUInt16() {
        if (UBS_UNLIKELY(!canRead(2))) return setErrorRet<uint16_t>();
        uint16_t v;
        __builtin_memcpy(&v, readPtr + position, 2);
        position += 2;
        return (UBS_UNLIKELY(endian == Endian::Big)) ? std::byteswap(v) : v;
    }

    [[nodiscard]] UBS_INLINE int32_t readInt32() {
        return static_cast<int32_t>(readUInt32());
    }

    [[nodiscard]] UBS_INLINE uint32_t readUInt32() {
        if (UBS_UNLIKELY(!canRead(4))) return setErrorRet<uint32_t>();
        uint32_t v;
        __builtin_memcpy(&v, readPtr + position, 4);
        position += 4;
        return (UBS_UNLIKELY(endian == Endian::Big)) ? std::byteswap(v) : v;
    }

    [[nodiscard]] UBS_INLINE int32_t peekInt32() {
        const size_t saved = position;
        const int32_t v    = readInt32();
        position = saved;
        return v;
    }

    [[nodiscard]] UBS_INLINE int64_t readInt64() {
        return static_cast<int64_t>(readUInt64());
    }

    [[nodiscard]] UBS_INLINE uint64_t readUInt64() {
        if (UBS_UNLIKELY(!canRead(8))) return setErrorRet<uint64_t>();
        uint64_t v;
        __builtin_memcpy(&v, readPtr + position, 8);
        position += 8;
        return (UBS_UNLIKELY(endian == Endian::Big)) ? std::byteswap(v) : v;
    }

    [[nodiscard]] UBS_INLINE float readFloat32() {
        if (UBS_UNLIKELY(!canRead(4))) return setErrorRet<float>();
        uint32_t tmp;
        __builtin_memcpy(&tmp, readPtr + position, 4);
        position += 4;
        if (UBS_UNLIKELY(endian == Endian::Big)) tmp = std::byteswap(tmp);
        return std::bit_cast<float>(tmp);
    }

    [[nodiscard]] UBS_INLINE double readDouble() {
        if (UBS_UNLIKELY(!canRead(8))) return setErrorRet<double>();
        uint64_t tmp;
        __builtin_memcpy(&tmp, readPtr + position, 8);
        position += 8;
        if (UBS_UNLIKELY(endian == Endian::Big)) tmp = std::byteswap(tmp);
        return std::bit_cast<double>(tmp);
    }

    [[nodiscard]] UBS_INLINE bool readBool() {
        if (UBS_UNLIKELY(!canRead(1))) return setErrorRet<bool>();
        return readPtr[position++] != 0;
    }

    [[nodiscard]] UBS_INLINE uint16_t readChar() { return readUInt16(); }

    [[nodiscard]] UBS_INLINE int32_t readVarInt32() {
        if (UBS_UNLIKELY(!canRead(1))) return setErrorRet<int32_t>();
        const uint8_t b0 = readPtr[position++];
        if (UBS_LIKELY(!(b0 & 0x80)))
            return static_cast<int32_t>(b0);
        return readVarInt32Slow(b0);
    }

    [[nodiscard]] UBS_INLINE int64_t readVarInt64() {
        if (UBS_UNLIKELY(!canRead(1))) return setErrorRet<int64_t>();
        const uint8_t b0 = readPtr[position++];
        if (UBS_LIKELY(!(b0 & 0x80)))
            return static_cast<int64_t>(b0);
        return readVarInt64Slow(b0);
    }

    [[nodiscard]] UBS_INLINE std::string readString() {
        return readStringByInt32Head();
    }
    [[nodiscard]] UBS_INLINE std::string readStringByUInt8Head() {
        const uint8_t len = readUInt8();
        return (UBS_UNLIKELY(hasError) || len == 0) ? "" : readStringRaw(len);
    }
    [[nodiscard]] UBS_INLINE std::string readStringByInt16Head() {
        const int16_t len = readInt16();
        return (UBS_UNLIKELY(hasError) || len <= 0) ? "" : readStringRaw(static_cast<size_t>(len));
    }
    [[nodiscard]] UBS_INLINE std::string readStringByInt32Head() {
        const int32_t len = readInt32();
        return (UBS_UNLIKELY(hasError) || len <= 0) ? "" : readStringRaw(static_cast<size_t>(len));
    }
    [[nodiscard]] UBS_INLINE std::string readStringByUInt32Head() {
        const uint32_t len = readUInt32();
        return (UBS_UNLIKELY(hasError) || len == 0) ? "" : readStringRaw(len);
    }
    [[nodiscard]] UBS_INLINE std::string readStringByVarInt32Head() {
        const int32_t len = readVarInt32();
        return (UBS_UNLIKELY(hasError) || len <= 0) ? "" : readStringRaw(static_cast<size_t>(len));
    }

    [[nodiscard]] UBS_INLINE int8_t  readSByte() { return readInt8();  }
    [[nodiscard]] UBS_INLINE uint8_t readByte()  { return readUInt8(); }

    [[nodiscard]] std::string readStringUnicode(size_t charCount) {
        if (UBS_UNLIKELY(!canRead(charCount * 2))) {
            hasError = true;
            return {};
        }
        std::string result;
        result.reserve(charCount * 3);
        for (size_t i = 0; i < charCount; ++i) {
            uint16_t ch;
            __builtin_memcpy(&ch, readPtr + position, 2);
            position += 2;
            if (UBS_UNLIKELY(endian == Endian::Big)) ch = std::byteswap(ch);

            if (ch < 0x80) {
                result += static_cast<char>(ch);
            } else if (ch < 0x800) {
                result += static_cast<char>(0xC0 | (ch >> 6));
                result += static_cast<char>(0x80 | (ch & 0x3F));
            } else {
                result += static_cast<char>(0xE0 | (ch >> 12));
                result += static_cast<char>(0x80 | ((ch >> 6) & 0x3F));
                result += static_cast<char>(0x80 | (ch & 0x3F));
            }
        }
        return result;
    }

    void writeBytes(std::span<const uint8_t> data) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        if (data.empty()) return;
        ensureCapacity(data.size());
        if (UBS_LIKELY(!hasError)) {
            std::memcpy(writeBuffer.data() + position, data.data(), data.size());
            position += data.size();
        }
    }
    void writeBytes(const uint8_t* data, size_t size) {
        writeBytes(std::span<const uint8_t>(data, size));
    }
    void writeBytes(const std::vector<uint8_t>& data) {
        writeBytes(std::span<const uint8_t>(data));
    }

    UBS_INLINE void writeInt8(int8_t v)   { writeUInt8(static_cast<uint8_t>(v)); }
    UBS_INLINE void writeSByte(int8_t v)  { writeUInt8(static_cast<uint8_t>(v)); }
    UBS_INLINE void writeByte(uint8_t v)  { writeUInt8(v); }

    UBS_INLINE void writeUInt8(uint8_t value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        ensureCapacity(1);
        if (UBS_LIKELY(!hasError)) writeBuffer[position++] = value;
    }

    UBS_INLINE void writeInt16(int16_t v)  { writeUInt16(static_cast<uint16_t>(v)); }

    UBS_INLINE void writeUInt16(uint16_t value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        if (UBS_UNLIKELY(endian == Endian::Big)) value = std::byteswap(value);
        ensureCapacity(2);
        if (UBS_LIKELY(!hasError)) {
            __builtin_memcpy(writeBuffer.data() + position, &value, 2);
            position += 2;
        }
    }

    UBS_INLINE void writeInt32(int32_t v)  { writeUInt32(static_cast<uint32_t>(v)); }

    UBS_INLINE void writeUInt32(uint32_t value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        if (UBS_UNLIKELY(endian == Endian::Big)) value = std::byteswap(value);
        ensureCapacity(4);
        if (UBS_LIKELY(!hasError)) {
            __builtin_memcpy(writeBuffer.data() + position, &value, 4);
            position += 4;
        }
    }

    UBS_INLINE void writeInt64(int64_t v)  { writeUInt64(static_cast<uint64_t>(v)); }

    UBS_INLINE void writeUInt64(uint64_t value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        if (UBS_UNLIKELY(endian == Endian::Big)) value = std::byteswap(value);
        ensureCapacity(8);
        if (UBS_LIKELY(!hasError)) {
            __builtin_memcpy(writeBuffer.data() + position, &value, 8);
            position += 8;
        }
    }

    UBS_INLINE void writeFloat32(float value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        uint32_t tmp = std::bit_cast<uint32_t>(value);
        if (UBS_UNLIKELY(endian == Endian::Big)) tmp = std::byteswap(tmp);
        ensureCapacity(4);
        if (UBS_LIKELY(!hasError)) {
            __builtin_memcpy(writeBuffer.data() + position, &tmp, 4);
            position += 4;
        }
    }

    UBS_INLINE void writeDouble(double value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        uint64_t tmp = std::bit_cast<uint64_t>(value);
        if (UBS_UNLIKELY(endian == Endian::Big)) tmp = std::byteswap(tmp);
        ensureCapacity(8);
        if (UBS_LIKELY(!hasError)) {
            __builtin_memcpy(writeBuffer.data() + position, &tmp, 8);
            position += 8;
        }
    }

    UBS_INLINE void writeBool(bool value)      { writeUInt8(value ? 1 : 0); }
    UBS_INLINE void writeChar(uint16_t value)  { writeUInt16(value); }

    UBS_INLINE void writeVarInt32(int32_t value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        auto uv = static_cast<uint32_t>(value);
        if (UBS_LIKELY(uv < 0x80)) {
            ensureCapacity(1);
            if (UBS_LIKELY(!hasError)) writeBuffer[position++] = static_cast<uint8_t>(uv);
            return;
        }
        writeVarInt32Slow(uv);
    }

    UBS_INLINE void writeVarInt64(int64_t value) {
        if (UBS_UNLIKELY(mode != Mode::Write)) { hasError = true; return; }
        auto uv = static_cast<uint64_t>(value);
        if (UBS_LIKELY(uv < 0x80)) {
            ensureCapacity(1);
            if (UBS_LIKELY(!hasError)) writeBuffer[position++] = static_cast<uint8_t>(uv);
            return;
        }
        writeVarInt64Slow(uv);
    }

    UBS_INLINE void writeString(std::string_view str) {
        writeStringByUInt32Head(str);
    }
    void writeStringByUInt8Head(std::string_view str) {
        if (UBS_UNLIKELY(str.length() > 255)) { hasError = true; return; }
        writeUInt8(static_cast<uint8_t>(str.length()));
        if (UBS_LIKELY(!hasError) && !str.empty()) writeStringRaw(str);
    }
    void writeStringByInt16Head(std::string_view str) {
        writeInt16(static_cast<int16_t>(str.length()));
        if (UBS_LIKELY(!hasError) && !str.empty()) writeStringRaw(str);
    }
    void writeStringByInt32Head(std::string_view str) {
        writeInt32(static_cast<int32_t>(str.length()));
        if (UBS_LIKELY(!hasError) && !str.empty()) writeStringRaw(str);
    }
    void writeStringByUInt32Head(std::string_view str) {
        writeUInt32(static_cast<uint32_t>(str.length()));
        if (UBS_LIKELY(!hasError) && !str.empty()) writeStringRaw(str);
    }
    void writeStringByVarInt32Head(std::string_view str) {
        writeVarInt32(static_cast<int32_t>(str.length()));
        if (UBS_LIKELY(!hasError) && !str.empty()) writeStringRaw(str);
    }

    void writeStringUnicode(std::string_view str) {
        for (const unsigned char c : str)
            writeUInt16(static_cast<uint16_t>(c));
    }

    void saveToFile(const std::string& filename) {
        const uint8_t* ptr = (mode == Mode::Read) ? readPtr : writeBuffer.data();
        std::ofstream file(filename, std::ios::binary);
        if (UBS_LIKELY(file.is_open()))
            file.write(reinterpret_cast<const char*>(ptr), getLength());
        else
            hasError = true;
    }

    [[nodiscard]] const std::vector<uint8_t>& getData()  const noexcept { return writeBuffer; }
    [[nodiscard]]       std::vector<uint8_t>& getData()        noexcept { return writeBuffer; }

    [[nodiscard]] std::vector<uint8_t> toByteArray() const {
        if (mode == Mode::Read)
            return std::vector<uint8_t>(readPtr, readPtr + readSize);
        return writeBuffer;
    }

    [[nodiscard]] UBS_INLINE static int8_t bytesToInt8(const uint8_t* b) noexcept {
        return static_cast<int8_t>(b[0]);
    }
    [[nodiscard]] UBS_INLINE static int16_t bytesToInt16(const uint8_t* b) noexcept {
        int16_t v; __builtin_memcpy(&v, b, 2); return v;
    }
    [[nodiscard]] UBS_INLINE static int32_t bytesToInt32(const uint8_t* b) noexcept {
        int32_t v; __builtin_memcpy(&v, b, 4); return v;
    }
    [[nodiscard]] UBS_INLINE static uint16_t bytesToUInt16(const uint8_t* b) noexcept {
        uint16_t v; __builtin_memcpy(&v, b, 2); return v;
    }
    [[nodiscard]] UBS_INLINE static uint32_t bytesToUInt32(const uint8_t* b) noexcept {
        uint32_t v; __builtin_memcpy(&v, b, 4); return v;
    }
    UBS_INLINE static void int16ToBytes(int16_t   v, uint8_t* b) noexcept { __builtin_memcpy(b, &v, 2); }
    UBS_INLINE static void int32ToBytes(int32_t   v, uint8_t* b) noexcept { __builtin_memcpy(b, &v, 4); }
    UBS_INLINE static void uInt16ToBytes(uint16_t v, uint8_t* b) noexcept { __builtin_memcpy(b, &v, 2); }
    UBS_INLINE static void uInt32ToBytes(uint32_t v, uint8_t* b) noexcept { __builtin_memcpy(b, &v, 4); }

private:

    const uint8_t* readPtr;
    size_t         readSize;
    size_t         position;
    Endian         endian;
    Mode           mode;
    bool           hasError;

    std::vector<uint8_t> writeBuffer;
    std::vector<uint8_t> headerBackup;

    [[nodiscard]] UBS_INLINE bool canRead(size_t bytes) const noexcept {
        if (UBS_UNLIKELY(hasError)) return false;
        const size_t limit = (mode == Mode::Read) ? readSize : writeBuffer.size();
        return position + bytes <= limit;
    }

    template<typename T>
    UBS_COLD T setErrorRet() noexcept { hasError = true; return T{}; }

    UBS_INLINE void ensureCapacity(size_t extra) {
        const size_t required = position + extra;
        if (UBS_LIKELY(writeBuffer.size() >= required)) return;
        growBuffer(required);
    }

    UBS_COLD void growBuffer(size_t required) {
        try {
            const size_t newCap = std::max(writeBuffer.capacity() * 2, required);
            if (writeBuffer.capacity() < required)
                writeBuffer.reserve(newCap);
            writeBuffer.resize(required);
        } catch (...) {
            hasError = true;
        }
    }

    UBS_INLINE std::string readStringRaw(size_t length) {
        if (UBS_UNLIKELY(!canRead(length))) {
            hasError = true;
            return {};
        }
        std::string result(reinterpret_cast<const char*>(readPtr + position), length);
        position += length;
        return result;
    }

    UBS_INLINE void writeStringRaw(std::string_view str) {
        ensureCapacity(str.size());
        if (UBS_LIKELY(!hasError)) {
            std::memcpy(writeBuffer.data() + position, str.data(), str.size());
            position += str.size();
        }
    }

    UBS_COLD int32_t readVarInt32Slow(uint8_t b0) {
        uint32_t result = b0 & 0x7F;
        int shift = 7;
        uint8_t byte;
        do {
            if (UBS_UNLIKELY(!canRead(1))) return setErrorRet<int32_t>();
            byte = readPtr[position++];
            if (shift < 32) result |= static_cast<uint32_t>(byte & 0x7F) << shift;
            shift += 7;
        } while (byte & 0x80);
        return static_cast<int32_t>(result);
    }

    UBS_COLD int64_t readVarInt64Slow(uint8_t b0) {
        int64_t result = b0 & 0x7F;
        int shift = 7;
        uint8_t byte;
        do {
            if (UBS_UNLIKELY(!canRead(1))) return setErrorRet<int64_t>();
            byte = readPtr[position++];
            result |= static_cast<int64_t>(byte & 0x7F) << shift;
            shift += 7;
            if (UBS_UNLIKELY(shift >= 64)) { hasError = true; return result; }
        } while (byte & 0x80);
        return result;
    }

    UBS_COLD void writeVarInt32Slow(uint32_t uv) {
        uint8_t buf[5]; int len = 0;
        while (uv >= 0x80) {
            buf[len++] = static_cast<uint8_t>((uv & 0x7F) | 0x80);
            uv >>= 7;
        }
        buf[len++] = static_cast<uint8_t>(uv);
        ensureCapacity(len);
        if (UBS_LIKELY(!hasError)) {
            std::memcpy(writeBuffer.data() + position, buf, len);
            position += len;
        }
    }

    UBS_COLD void writeVarInt64Slow(uint64_t uv) {
        uint8_t buf[10]; int len = 0;
        while (uv >= 0x80) {
            buf[len++] = static_cast<uint8_t>((uv & 0x7F) | 0x80);
            uv >>= 7;
        }
        buf[len++] = static_cast<uint8_t>(uv);
        ensureCapacity(len);
        if (UBS_LIKELY(!hasError)) {
            std::memcpy(writeBuffer.data() + position, buf, len);
            position += len;
        }
    }
};

#undef UBS_INLINE
#undef UBS_COLD
#undef UBS_LIKELY
#undef UBS_UNLIKELY
#undef UBS_PREFETCH_R

}
