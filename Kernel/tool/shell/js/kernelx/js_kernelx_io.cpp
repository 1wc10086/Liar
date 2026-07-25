module;
#include <algorithm>
#include <cerrno>
#include <cstdint>
#include <fcntl.h>
#include <initializer_list>
#include <mutex>
#include <string>
#include <string_view>
#include <tuple>
#include <utility>
#include <vector>
#include <unistd.h>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

import tool.shell.js_utils;
import utility.io;

namespace kernelx {

using FnList = std::initializer_list<std::tuple<const char*, JSCFunction*, int>>;

int32_t intArg(JSContext* ctx, JSValueConst value, int32_t fallback) noexcept;
int64_t int64Arg(JSContext* ctx, JSValueConst value, int64_t fallback) noexcept;
bool boolArg(JSContext* ctx, JSValueConst value, bool fallback) noexcept;
std::vector<std::string> stringList(JSContext* ctx, JSValueConst value);
JSValue stringArray(JSContext* ctx, const std::vector<std::string>& values);
JSValue object(JSContext* ctx, FnList funcs);
void setConst(JSContext* ctx, JSValueConst obj, const char* name, JSValue value);
class DynamicLibrary;

namespace {

std::string currentFilename(JSContext* ctx) {
    JSValue global = JS_GetGlobalObject(ctx);
    JSValue value = JS_GetPropertyStr(ctx, global, "__filename");
    JS_FreeValue(ctx, global);
    if (JS_IsException(value)) {
        js::clearException(ctx);
        return {};
    }
    auto out = js::toString(ctx, value);
    JS_FreeValue(ctx, value);
    return out;
}

std::string currentDirname(JSContext* ctx) {
    return FileUtils::getParentDirectory(currentFilename(ctx));
}

std::string currentLibraryBase(JSContext* ctx) {
    JSValue global = JS_GetGlobalObject(ctx);
    JSValue value = JS_GetPropertyStr(ctx, global, "__kernelx_library__");
    JS_FreeValue(ctx, global);
    if (JS_IsException(value)) {
        js::clearException(ctx);
        return {};
    }
    auto out = js::toString(ctx, value);
    JS_FreeValue(ctx, value);
    return out;
}

struct MmapOwner { const uint8_t* data{}; size_t size{}; };

struct FileWriter {
    int fd{-1};
    uint64_t position{};
    bool failed{};
};

JSClassID fileWriterClass{};
std::once_flag fileWriterClassId;

void fileWriterFinalizer(JSRuntime*, JSValue value) {
    auto* writer = static_cast<FileWriter*>(JS_GetOpaque(value, fileWriterClass));
    if (!writer) return;
    if (writer->fd >= 0) ::close(writer->fd);
    delete writer;
}

const JSClassDef fileWriterClassDef{"KernelxFileWriter", fileWriterFinalizer, nullptr, nullptr, nullptr};

FileWriter* fileWriter(JSContext* ctx, JSValueConst value) {
    return static_cast<FileWriter*>(JS_GetOpaque2(ctx, value, fileWriterClass));
}

bool writeAll(int fd, const uint8_t* data, size_t size) noexcept {
    while (size) {
        const auto written = ::write(fd, data, size);
        if (written < 0) {
            if (errno == EINTR) continue;
            return false;
        }
        data += static_cast<size_t>(written);
        size -= static_cast<size_t>(written);
    }
    return true;
}

void freeMmap(JSRuntime*, void* opaque, void*) {
    auto* owner = static_cast<MmapOwner*>(opaque);
    if (owner && owner->data && owner->size) FileUtils::munmapFile(owner->data, owner->size);
    delete owner;
}

JSValue readText(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewString(ctx, "");
    try { return js::string(ctx, FileUtils::readTextFile(js::toString(ctx, argv[0]))); }
    catch (...) { return JS_NewString(ctx, ""); }
}

JSValue writeText(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    return JS_NewBool(ctx, argc > 1 && FileUtils::writeTextFile(js::toString(ctx, argv[0]), js::toString(ctx, argv[1])));
}

JSValue appendText(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    return JS_NewBool(ctx, argc > 1 && FileUtils::appendTextFile(js::toString(ctx, argv[0]), js::toString(ctx, argv[1])));
}

JSValue readBytes(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    try { return js::bytes(ctx, FileUtils::readFileBytes(js::toString(ctx, argv[0]))); }
    catch (...) { return js::bytes(ctx, nullptr, 0); }
}

JSValue writeBytes(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_FALSE;
    auto view = js::bytesView(ctx, argv[1]);
    return JS_NewBool(ctx, view && FileUtils::writeFileBytes(js::toString(ctx, argv[0]), view.span()));
}

JSValue appendBytes(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_FALSE;
    auto view = js::bytesView(ctx, argv[1]);
    return JS_NewBool(ctx, view && FileUtils::appendFileBytes(js::toString(ctx, argv[0]), view.span()));
}

JSValue openWriter(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    const auto path = js::toString(ctx, argv[0]);
    if (path.empty()) return JS_NULL;
    const auto parent = FileUtils::getParentDirectory(path);
    if (!parent.empty()) FileUtils::createDirectory(parent);
    const bool append = argc > 1 && boolArg(ctx, argv[1], false);
    const int fd = ::open(path.c_str(), O_CREAT | O_WRONLY | O_CLOEXEC | (append ? O_APPEND : O_TRUNC), 0644);
    if (fd < 0) return JS_NULL;
    std::call_once(fileWriterClassId, [&] { JS_NewClassID(JS_GetRuntime(ctx), &fileWriterClass); });
    JS_NewClass(JS_GetRuntime(ctx), fileWriterClass, &fileWriterClassDef);
    JSValue object = JS_NewObjectClass(ctx, fileWriterClass);
    if (JS_IsException(object)) {
        ::close(fd);
        return object;
    }
    auto* writer = new FileWriter{fd, append ? static_cast<uint64_t>(std::max<int64_t>(0, FileUtils::getFileSize(path))) : 0, false};
    JS_SetOpaque(object, writer);
    js::setFunction(ctx, object, "write", [](JSContext* ctx, JSValueConst self, int count, JSValueConst* values) -> JSValue {
        auto* writer = fileWriter(ctx, self);
        if (!writer || writer->fd < 0 || count < 1) return JS_FALSE;
        auto bytes = js::bytesView(ctx, values[0]);
        if (!bytes) return JS_FALSE;
        writer->failed = !writeAll(writer->fd, bytes.data, bytes.size);
        if (!writer->failed) writer->position += bytes.size;
        return JS_NewBool(ctx, !writer->failed);
    }, 1);
    js::setFunction(ctx, object, "close", [](JSContext* ctx, JSValueConst self, int, JSValueConst*) -> JSValue {
        auto* writer = fileWriter(ctx, self);
        if (!writer || writer->fd < 0) return JS_FALSE;
        writer->failed = ::close(std::exchange(writer->fd, -1)) != 0 || writer->failed;
        return JS_NewBool(ctx, !writer->failed);
    }, 0);
    js::setFunction(ctx, object, "position", [](JSContext* ctx, JSValueConst self, int, JSValueConst*) -> JSValue {
        const auto* writer = fileWriter(ctx, self);
        return JS_NewBigUint64(ctx, writer ? writer->position : 0);
    }, 0);
    js::setFunction(ctx, object, "failed", [](JSContext* ctx, JSValueConst self, int, JSValueConst*) -> JSValue {
        const auto* writer = fileWriter(ctx, self);
        return JS_NewBool(ctx, !writer || writer->failed);
    }, 0);
    return object;
}

JSValue mmapRead(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    auto [data, size] = FileUtils::mmapReadFile(js::toString(ctx, argv[0]));
    if (!data || !size) return js::bytes(ctx, nullptr, 0);
    auto* owner = new MmapOwner{data, size};
    JSValue out = JS_NewArrayBuffer(ctx, const_cast<uint8_t*>(data), size, freeMmap, owner, false);
    if (JS_IsException(out)) {
        freeMmap(JS_GetRuntime(ctx), owner, nullptr);
        return out;
    }
    JS_SetImmutableArrayBuffer(out, true);
    return out;
}

JSValue posixReadInto(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_FALSE;
    auto view = js::bytesView(ctx, argv[1]);
    if (!view) return JS_FALSE;
    const auto size = argc > 2 ? std::min<size_t>(static_cast<size_t>(std::max<int64_t>(0, int64Arg(ctx, argv[2], 0))), view.size) : view.size;
    const auto offset = argc > 3 ? int64Arg(ctx, argv[3], 0) : 0;
    return JS_NewBool(ctx, FileUtils::posixReadInto(js::toString(ctx, argv[0]), const_cast<uint8_t*>(view.data), size, offset));
}

JSValue posixWrite(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2) return JS_FALSE;
    auto view = js::bytesView(ctx, argv[1]);
    if (!view) return JS_FALSE;
    FileUtils::posixWrite(js::toString(ctx, argv[0]), view.data, view.size);
    return JS_TRUE;
}

JSValue posixWriteV(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 3) return JS_FALSE;
    auto header = js::bytesView(ctx, argv[1]);
    auto body = js::bytesView(ctx, argv[2]);
    if (!header || !body) return JS_FALSE;
    FileUtils::posixWriteV(js::toString(ctx, argv[0]), header.data, header.size, body.data, body.size);
    return JS_TRUE;
}

JSValue streamRead(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2 || !JS_IsFunction(ctx, argv[1])) return JS_FALSE;
    const auto path = js::toString(ctx, argv[0]);
    const auto chunk = argc > 2 ? static_cast<size_t>(std::max<int64_t>(4096, int64Arg(ctx, argv[2], 65536))) : 65536ull;
    bool ok = FileUtils::streamReadFile(path, chunk, [&](const uint8_t* data, size_t size) {
        JSValue arg = js::bytes(ctx, data, size);
        JSValue ret = JS_Call(ctx, argv[1], JS_UNDEFINED, 1, &arg);
        JS_FreeValue(ctx, arg);
        const bool keep = !JS_IsException(ret) && JS_ToBool(ctx, ret) != 0;
        if (JS_IsException(ret)) js::clearException(ctx);
        JS_FreeValue(ctx, ret);
        return keep;
    });
    return JS_NewBool(ctx, ok);
}

JSValue exists(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::fileExists(js::toString(ctx, argv[0]))); }
JSValue createFile(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::createFile(js::toString(ctx, argv[0]))); }
JSValue mkdir(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::createDirectory(js::toString(ctx, argv[0]))); }
JSValue clearDir(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::clearDirectory(js::toString(ctx, argv[0]))); }
JSValue remove(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && (FileUtils::deleteDirectory(js::toString(ctx, argv[0])) || FileUtils::deleteFile(js::toString(ctx, argv[0])))); }
JSValue copy(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 1 && FileUtils::copyFile(js::toString(ctx, argv[0]), js::toString(ctx, argv[1]))); }
JSValue copyDir(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 1 && FileUtils::copyDirectory(js::toString(ctx, argv[0]), js::toString(ctx, argv[1]))); }
JSValue move(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 1 && FileUtils::moveFile(js::toString(ctx, argv[0]), js::toString(ctx, argv[1]))); }
JSValue truncate(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::truncateFile(js::toString(ctx, argv[0]))); }
JSValue size(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt64(ctx, argc > 0 ? FileUtils::getFileSize(js::toString(ctx, argv[0])) : -1); }
JSValue posixSize(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt64(ctx, argc > 0 ? FileUtils::posixFileSize(js::toString(ctx, argv[0])) : -1); }
JSValue dirSize(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt64(ctx, argc > 0 ? FileUtils::getDirectorySize(js::toString(ctx, argv[0])) : -1); }
JSValue modifiedTime(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt64(ctx, argc > 0 ? FileUtils::getFileModifiedTime(js::toString(ctx, argv[0])) : -1); }
JSValue permissions(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt32(ctx, argc > 0 ? FileUtils::getFilePermissions(js::toString(ctx, argv[0])) : -1); }
JSValue setPermissions(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 1 && FileUtils::setFilePermissions(js::toString(ctx, argv[0]), intArg(ctx, argv[1], 0))); }
JSValue isDir(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::isDirectory(js::toString(ctx, argv[0]))); }
JSValue isFile(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::isRegularFile(js::toString(ctx, argv[0]))); }
JSValue isLink(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::isSymbolicLink(js::toString(ctx, argv[0]))); }
JSValue readable(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::isReadable(js::toString(ctx, argv[0]))); }
JSValue writable(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::isWritable(js::toString(ctx, argv[0]))); }
JSValue executable(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 0 && FileUtils::isExecutable(js::toString(ctx, argv[0]))); }
JSValue compare(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewBool(ctx, argc > 1 && FileUtils::compareFiles(js::toString(ctx, argv[0]), js::toString(ctx, argv[1]))); }
JSValue list(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? stringArray(ctx, FileUtils::listDirectory(js::toString(ctx, argv[0]), argc > 1 && boolArg(ctx, argv[1], false))) : JS_NewArray(ctx); }
JSValue listFiles(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? stringArray(ctx, FileUtils::listDirectory(js::toString(ctx, argv[0]), true)) : JS_NewArray(ctx); }
JSValue collect(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? stringArray(ctx, FileUtils::collectFiles(js::toString(ctx, argv[0]))) : JS_NewArray(ctx); }
JSValue collectExt(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? stringArray(ctx, FileUtils::collectFilesByExtension(js::toString(ctx, argv[0]), argc > 1 ? stringList(ctx, argv[1]) : std::vector<std::string>{})) : JS_NewArray(ctx); }
JSValue count(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt32(ctx, argc > 0 ? FileUtils::countFiles(js::toString(ctx, argv[0])) : 0); }

JSValue batchRead(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsArray(argv[0])) return JS_NewArray(ctx);
    auto data = FileUtils::batchReadFiles(stringList(ctx, argv[0]));
    JSValue arr = JS_NewArray(ctx);
    for (uint32_t i = 0; i < data.size(); ++i) JS_SetPropertyUint32(ctx, arr, i, js::bytes(ctx, std::move(data[i])));
    return arr;
}

JSValue batchCopy(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt32(ctx, argc > 1 && JS_IsArray(argv[0]) && JS_IsArray(argv[1]) ? FileUtils::batchCopyFiles(stringList(ctx, argv[0]), stringList(ctx, argv[1])) : 0); }
JSValue batchRemove(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return JS_NewInt32(ctx, argc > 0 && JS_IsArray(argv[0]) ? FileUtils::batchDeleteFiles(stringList(ctx, argv[0])) : 0); }

JSValue join(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewString(ctx, "");
    std::string path = js::toString(ctx, argv[0]);
    for (int i = 1; i < argc; ++i) path = FileUtils::joinPath(path, js::toString(ctx, argv[i]));
    return js::string(ctx, path);
}

JSValue parent(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? js::string(ctx, FileUtils::getParentDirectory(js::toString(ctx, argv[0]))) : JS_NewString(ctx, ""); }
JSValue name(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? js::string(ctx, FileUtils::getFileName(js::toString(ctx, argv[0]))) : JS_NewString(ctx, ""); }
JSValue stem(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? js::string(ctx, FileUtils::getFileNameWithoutExtension(js::toString(ctx, argv[0]))) : JS_NewString(ctx, ""); }
JSValue ext(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? js::string(ctx, FileUtils::getFileExtension(js::toString(ctx, argv[0]))) : JS_NewString(ctx, ""); }
JSValue absolute(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? js::string(ctx, FileUtils::getAbsolutePath(js::toString(ctx, argv[0]))) : JS_NewString(ctx, ""); }
JSValue normalize(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 0 ? js::string(ctx, FileUtils::normalizeFsPath(js::toString(ctx, argv[0]))) : JS_NewString(ctx, ""); }
JSValue changeExt(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) { return argc > 1 ? js::string(ctx, FileUtils::changeFileExtension(js::toString(ctx, argv[0]), js::toString(ctx, argv[1]))) : JS_NewString(ctx, ""); }

JSValue scriptFilename(JSContext* ctx, JSValueConst, int, JSValueConst*) { return js::string(ctx, currentFilename(ctx)); }
JSValue scriptDirname(JSContext* ctx, JSValueConst, int, JSValueConst*) { return js::string(ctx, FileUtils::getParentDirectory(currentFilename(ctx))); }
JSValue scriptResolve(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    std::string path = FileUtils::getParentDirectory(currentFilename(ctx));
    for (int i = 0; i < argc; ++i) path = FileUtils::joinPath(path, js::toString(ctx, argv[i]));
    return js::string(ctx, FileUtils::normalizeFsPath(path));
}
JSValue scriptReadText(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NewString(ctx, "");
    const std::string path = FileUtils::joinPath(FileUtils::getParentDirectory(currentFilename(ctx)), js::toString(ctx, argv[0]));
    try { return js::string(ctx, FileUtils::readTextFile(path)); }
    catch (...) { return JS_NewString(ctx, ""); }
}
JSValue scriptReadBytes(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return js::bytes(ctx, nullptr, 0);
    const std::string path = FileUtils::joinPath(FileUtils::getParentDirectory(currentFilename(ctx)), js::toString(ctx, argv[0]));
    try { return js::bytes(ctx, FileUtils::readFileBytes(path)); }
    catch (...) { return js::bytes(ctx, nullptr, 0); }
}

JSValue libraryOpen(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1) return JS_NULL;
    const auto name = js::toString(ctx, argv[0]);
    auto lib = DynamicLibrary::openConfigured(name, reinterpret_cast<const void*>(&libraryOpen), DynamicLibrary::ClosePolicy::manual);
    if (!lib.loaded()) return JS_NULL;
    JSValue obj = JS_NewObject(ctx);
    setConst(ctx, obj, "path", js::string(ctx, lib.path()));
    setConst(ctx, obj, "handle", JS_NewBigUint64(ctx, lib.release()));
    setConst(ctx, obj, "error", js::string(ctx, lib.error()));
    JS_SetPropertyStr(ctx, obj, "close", JS_NewCFunction(ctx, [](JSContext* ctx, JSValueConst thisVal, int, JSValueConst*) -> JSValue {
        JSValue handle = JS_GetPropertyStr(ctx, thisVal, "handle");
        uint64_t raw = 0;
        JS_ToBigUint64(ctx, &raw, handle);
        JS_FreeValue(ctx, handle);
        if (raw) DynamicLibrary::adopt(raw).closeNow();
        JS_SetPropertyStr(ctx, thisVal, "handle", JS_NewBigUint64(ctx, 0));
        return JS_UNDEFINED;
    }, "close", 0));
    return obj;
}

JSValue librarySymbol(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 2 || !JS_IsObject(argv[0])) return JS_NULL;
    JSValue handle = JS_GetPropertyStr(ctx, argv[0], "handle");
    uint64_t raw = 0;
    JS_ToBigUint64(ctx, &raw, handle);
    JS_FreeValue(ctx, handle);
    if (!raw) return JS_NULL;
    const auto name = js::toString(ctx, argv[1]);
    const auto sym = reinterpret_cast<uint64_t>(DynamicLibrary::adopt(raw).symbol(name));
    return sym ? JS_NewBigUint64(ctx, sym) : JS_NULL;
}

JSValue libraryClose(JSContext* ctx, JSValueConst, int argc, JSValueConst* argv) {
    if (argc < 1 || !JS_IsObject(argv[0])) return JS_FALSE;
    JSValue handle = JS_GetPropertyStr(ctx, argv[0], "handle");
    uint64_t raw = 0;
    JS_ToBigUint64(ctx, &raw, handle);
    JS_FreeValue(ctx, handle);
    if (!raw) return JS_FALSE;
    DynamicLibrary::adopt(raw).closeNow();
    JS_SetPropertyStr(ctx, argv[0], "handle", JS_NewBigUint64(ctx, 0));
    return JS_TRUE;
}

JSValue libraryError(JSContext* ctx, JSValueConst, int, JSValueConst*) {
    return js::string(ctx, currentDlError());
}

}

JSValue createIo(JSContext* ctx) {
    return object(ctx, {
        {"readText", readText, 1}, {"writeText", writeText, 2}, {"appendText", appendText, 2},
        {"readBytes", readBytes, 1}, {"writeBytes", writeBytes, 2}, {"appendBytes", appendBytes, 2}, {"mmapRead", mmapRead, 1}, {"streamRead", streamRead, 2},
        {"openWriter", openWriter, 2},
        {"posixReadInto", posixReadInto, 4}, {"posixWrite", posixWrite, 2}, {"posixWriteV", posixWriteV, 3}, {"posixSize", posixSize, 1},
        {"exists", exists, 1}, {"createFile", createFile, 1}, {"mkdir", mkdir, 1}, {"clearDir", clearDir, 1}, {"remove", remove, 1},
        {"copy", copy, 2}, {"copyDir", copyDir, 2}, {"move", move, 2}, {"truncate", truncate, 1},
        {"size", size, 1}, {"dirSize", dirSize, 1}, {"modifiedTime", modifiedTime, 1}, {"permissions", permissions, 1}, {"setPermissions", setPermissions, 2},
        {"isDir", isDir, 1}, {"isFile", isFile, 1}, {"isLink", isLink, 1}, {"readable", readable, 1}, {"writable", writable, 1}, {"executable", executable, 1}, {"compare", compare, 2},
        {"list", list, 1}, {"listFiles", listFiles, 1}, {"collect", collect, 1}, {"collectExt", collectExt, 2}, {"count", count, 1},
        {"batchRead", batchRead, 1}, {"batchCopy", batchCopy, 2}, {"batchRemove", batchRemove, 1},
    });
}

JSValue createPath(JSContext* ctx) {
    return object(ctx, {
        {"join", join, 2}, {"dir", parent, 1}, {"parent", parent, 1}, {"name", name, 1}, {"stem", stem, 1}, {"ext", ext, 1},
        {"absolute", absolute, 1}, {"abs", absolute, 1}, {"changeExt", changeExt, 2}, {"normalize", normalize, 1},
    });
}

JSValue createScript(JSContext* ctx) {
    return object(ctx, {
        {"filename", scriptFilename, 0}, {"dirname", scriptDirname, 0}, {"resolve", scriptResolve, 1},
        {"readText", scriptReadText, 1}, {"readBytes", scriptReadBytes, 1},
    });
}

JSValue createLibrary(JSContext* ctx) {
    return object(ctx, {
        {"open", libraryOpen, 1}, {"symbol", librarySymbol, 2}, {"close", libraryClose, 1}, {"error", libraryError, 0},
    });
}

}
