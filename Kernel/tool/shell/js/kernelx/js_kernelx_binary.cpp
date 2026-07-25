module;
#include <algorithm>
#include <cstdint>
#include <mutex>
#include <span>
#include <string>
#include <type_traits>
#include <utility>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.binary.unified_binary_stream;

namespace kernelx {
namespace {

JSClassID binaryReaderClass{};
JSClassID binaryWriterClass{};
std::once_flag binaryClassIds;

struct BinaryReader {
    JSValue owner{JS_UNDEFINED};
    UnifiedBinaryStream stream;
    BinaryReader(JSContext* ctx, JSValueConst input, std::span<const uint8_t> bytes, UnifiedBinaryStream::Endian endian)
        : owner(JS_DupValue(ctx, input)), stream(bytes, endian) {}
};
struct BinaryWriter { UnifiedBinaryStream stream; explicit BinaryWriter(UnifiedBinaryStream::Endian endian) : stream(UnifiedBinaryStream::Mode::Write, endian) {} };

void readerFinalizer(JSRuntime* runtime, JSValue value) {
    if (auto* reader = static_cast<BinaryReader*>(JS_GetOpaque(value, binaryReaderClass))) {
        JS_FreeValueRT(runtime, reader->owner);
        delete reader;
    }
}
void writerFinalizer(JSRuntime*, JSValue value) { delete static_cast<BinaryWriter*>(JS_GetOpaque(value, binaryWriterClass)); }
const JSClassDef readerClassDef{"KernelxBinaryReader", readerFinalizer, nullptr, nullptr, nullptr};
const JSClassDef writerClassDef{"KernelxBinaryWriter", writerFinalizer, nullptr, nullptr, nullptr};

BinaryReader* reader(JSContext* ctx, JSValueConst value) { return static_cast<BinaryReader*>(JS_GetOpaque2(ctx, value, binaryReaderClass)); }
BinaryWriter* writer(JSContext* ctx, JSValueConst value) { return static_cast<BinaryWriter*>(JS_GetOpaque2(ctx, value, binaryWriterClass)); }

UnifiedBinaryStream::Endian endian(JSContext* ctx, int argc, JSValueConst* argv) {
    if (!argc) return UnifiedBinaryStream::Endian::Little;
    auto value = js::toString(ctx, argv[0]);
    return value == "be" || value == "big" || value == "bigEndian" ? UnifiedBinaryStream::Endian::Big : UnifiedBinaryStream::Endian::Little;
}

void initialize(JSContext* ctx) {
    auto* runtime = JS_GetRuntime(ctx);
    std::call_once(binaryClassIds, [&] {
        JS_NewClassID(runtime, &binaryReaderClass);
        JS_NewClassID(runtime, &binaryWriterClass);
    });
    JS_NewClass(runtime, binaryReaderClass, &readerClassDef);
    JS_NewClass(runtime, binaryWriterClass, &writerClassDef);
}

template <class T, auto Read>
JSValue readNumber(JSContext* ctx, JSValueConst self, int, JSValueConst*) {
    auto* value = reader(ctx, self);
    if (!value) return JS_NULL;
    if constexpr (std::is_same_v<T, uint64_t>) return JS_NewBigUint64(ctx, (value->stream.*Read)());
    else if constexpr (std::is_same_v<T, int64_t>) return JS_NewBigInt64(ctx, (value->stream.*Read)());
    else if constexpr (std::is_same_v<T, float> || std::is_same_v<T, double>) return JS_NewFloat64(ctx, (value->stream.*Read)());
    else return JS_NewInt64(ctx, (value->stream.*Read)());
}

template <class T, auto Write>
JSValue writeNumber(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) {
    auto* value = writer(ctx, self);
    if (!value || argc < 1) return JS_FALSE;
    T input{};
    if constexpr (std::is_same_v<T, uint64_t>) JS_ToBigUint64(ctx, &input, argv[0]);
    else if constexpr (std::is_same_v<T, int64_t>) JS_ToBigInt64(ctx, &input, argv[0]);
    else if constexpr (std::is_same_v<T, float> || std::is_same_v<T, double>) {
        double number{};
        JS_ToFloat64(ctx, &number, argv[0]);
        input = static_cast<T>(number);
    } else {
        int64_t number{};
        JS_ToInt64(ctx, &number, argv[0]);
        input = static_cast<T>(number);
    }
    (value->stream.*Write)(input);
    return JS_NewBool(ctx, !value->stream.hasErrorOccurred());
}

JSValue readerBytes(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* value = reader(ctx, self); if (!value || argc < 1) return JS_NULL; int64_t count{}; JS_ToInt64(ctx, &count, argv[0]); return count < 0 ? JS_NULL : js::bytes(ctx, value->stream.readBytes(static_cast<size_t>(count))); }
JSValue readerString(JSContext* ctx, JSValueConst self, int, JSValueConst*) { auto* value = reader(ctx, self); return value ? js::string(ctx, value->stream.readString()) : JS_NULL; }
JSValue readerPosition(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* value = reader(ctx, self); return JS_NewBigUint64(ctx, value ? value->stream.getPosition() : 0); }
JSValue readerRemaining(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* value = reader(ctx, self); return JS_NewBigUint64(ctx, value ? value->stream.getLength() - std::min(value->stream.getPosition(), value->stream.getLength()) : 0); }
JSValue readerSeek(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* value = reader(ctx, self); int64_t position{}; if (!value || argc < 1 || JS_ToInt64(ctx, &position, argv[0]) || position < 0 || static_cast<size_t>(position) > value->stream.getLength()) return JS_FALSE; value->stream.setPosition(static_cast<size_t>(position)); return JS_TRUE; }
JSValue readerError(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* value = reader(ctx, self); return JS_NewBool(ctx, !value || value->stream.hasErrorOccurred()); }

JSValue writerBytes(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* value = writer(ctx, self); if (!value || argc < 1) return JS_FALSE; auto input = js::bytesView(ctx, argv[0]); if (!input) return JS_FALSE; value->stream.writeBytes(input.span()); return JS_NewBool(ctx, !value->stream.hasErrorOccurred()); }
JSValue writerString(JSContext* ctx, JSValueConst self, int argc, JSValueConst* argv) { auto* value = writer(ctx, self); if (!value || argc < 1) return JS_FALSE; value->stream.writeString(js::toString(ctx, argv[0])); return JS_NewBool(ctx, !value->stream.hasErrorOccurred()); }
JSValue writerData(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* value = writer(ctx, self); return value ? js::bytes(ctx, value->stream.getData()) : JS_NULL; }
JSValue writerPosition(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* value = writer(ctx, self); return JS_NewBigUint64(ctx, value ? value->stream.getPosition() : 0); }
JSValue writerError(JSContext* ctx, JSValueConst self, int, JSValueConst*) { const auto* value = writer(ctx, self); return JS_NewBool(ctx, !value || value->stream.hasErrorOccurred()); }

void readerMethods(JSContext* ctx, JSValue object) {
    js::setFunction(ctx, object, "u8", readNumber<uint8_t, &UnifiedBinaryStream::readUInt8>, 0); js::setFunction(ctx, object, "i8", readNumber<int8_t, &UnifiedBinaryStream::readInt8>, 0);
    js::setFunction(ctx, object, "u16", readNumber<uint16_t, &UnifiedBinaryStream::readUInt16>, 0); js::setFunction(ctx, object, "i16", readNumber<int16_t, &UnifiedBinaryStream::readInt16>, 0);
    js::setFunction(ctx, object, "u32", readNumber<uint32_t, &UnifiedBinaryStream::readUInt32>, 0); js::setFunction(ctx, object, "i32", readNumber<int32_t, &UnifiedBinaryStream::readInt32>, 0);
    js::setFunction(ctx, object, "u64", readNumber<uint64_t, &UnifiedBinaryStream::readUInt64>, 0); js::setFunction(ctx, object, "i64", readNumber<int64_t, &UnifiedBinaryStream::readInt64>, 0);
    js::setFunction(ctx, object, "f32", readNumber<float, &UnifiedBinaryStream::readFloat32>, 0); js::setFunction(ctx, object, "f64", readNumber<double, &UnifiedBinaryStream::readDouble>, 0);
    js::setFunction(ctx, object, "varI32", readNumber<int32_t, &UnifiedBinaryStream::readVarInt32>, 0); js::setFunction(ctx, object, "varI64", readNumber<int64_t, &UnifiedBinaryStream::readVarInt64>, 0);
    js::setFunction(ctx, object, "bytes", readerBytes, 1); js::setFunction(ctx, object, "string", readerString, 0); js::setFunction(ctx, object, "position", readerPosition, 0); js::setFunction(ctx, object, "remaining", readerRemaining, 0); js::setFunction(ctx, object, "seek", readerSeek, 1); js::setFunction(ctx, object, "error", readerError, 0);
}

void writerMethods(JSContext* ctx, JSValue object) {
    js::setFunction(ctx, object, "u8", writeNumber<uint8_t, &UnifiedBinaryStream::writeUInt8>, 1); js::setFunction(ctx, object, "i8", writeNumber<int8_t, &UnifiedBinaryStream::writeInt8>, 1);
    js::setFunction(ctx, object, "u16", writeNumber<uint16_t, &UnifiedBinaryStream::writeUInt16>, 1); js::setFunction(ctx, object, "i16", writeNumber<int16_t, &UnifiedBinaryStream::writeInt16>, 1);
    js::setFunction(ctx, object, "u32", writeNumber<uint32_t, &UnifiedBinaryStream::writeUInt32>, 1); js::setFunction(ctx, object, "i32", writeNumber<int32_t, &UnifiedBinaryStream::writeInt32>, 1);
    js::setFunction(ctx, object, "u64", writeNumber<uint64_t, &UnifiedBinaryStream::writeUInt64>, 1); js::setFunction(ctx, object, "i64", writeNumber<int64_t, &UnifiedBinaryStream::writeInt64>, 1);
    js::setFunction(ctx, object, "f32", writeNumber<float, &UnifiedBinaryStream::writeFloat32>, 1); js::setFunction(ctx, object, "f64", writeNumber<double, &UnifiedBinaryStream::writeDouble>, 1);
    js::setFunction(ctx, object, "varI32", writeNumber<int32_t, &UnifiedBinaryStream::writeVarInt32>, 1); js::setFunction(ctx, object, "varI64", writeNumber<int64_t, &UnifiedBinaryStream::writeVarInt64>, 1);
    js::setFunction(ctx, object, "bytes", writerBytes, 1); js::setFunction(ctx, object, "string", writerString, 1); js::setFunction(ctx, object, "data", writerData, 0); js::setFunction(ctx, object, "finish", writerData, 0); js::setFunction(ctx, object, "position", writerPosition, 0); js::setFunction(ctx, object, "error", writerError, 0);
}

JSValue createReader(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { if (argc < 1) return JS_NULL; auto input = js::bytesView(ctx, argv[0]); if (!input) return JS_NULL; JSValue object = JS_NewObjectClass(ctx, binaryReaderClass); if (JS_IsException(object)) return object; JS_SetOpaque(object, new BinaryReader{ctx, argv[0], input.span(), endian(ctx, argc - 1, argv + 1)}); readerMethods(ctx, object); return object; }
JSValue createWriter(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { JSValue object = JS_NewObjectClass(ctx, binaryWriterClass); if (JS_IsException(object)) return object; JS_SetOpaque(object, new BinaryWriter{endian(ctx, argc, argv)}); writerMethods(ctx, object); return object; }

}

JSValue createBinary(JSContext* ctx) {
    initialize(ctx);
    JSValue api = JS_NewObject(ctx);
    js::setFunction(ctx, api, "reader", createReader, 2);
    js::setFunction(ctx, api, "writer", createWriter, 1);
    return api;
}

}
