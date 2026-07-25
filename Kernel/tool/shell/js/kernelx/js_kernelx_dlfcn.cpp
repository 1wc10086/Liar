module;
#include <dlfcn.h>
#include <cstdint>
#include <string>
#include <string_view>
#include <utility>

module tool.shell.js_engine;

import tool.shell.config_manager;
import utility.io;

namespace kernelx {

namespace {

[[nodiscard]] std::string fallbackLibraryPath(std::string_view name, const void* symbol) {
    if (!symbol) return {};
    Dl_info info{};
    if (!::dladdr(symbol, &info) || !info.dli_fname) return {};
    return FileUtils::joinPath(FileUtils::getParentDirectory(info.dli_fname), std::string{name});
}

}

DynamicLibrary::DynamicLibrary(uint64_t handle, std::string path, std::string error, ClosePolicy closePolicy) noexcept
    : handle_(handle), path_(std::move(path)), error_(std::move(error)), closePolicy_(closePolicy) {}

DynamicLibrary DynamicLibrary::adopt(uint64_t handle, std::string path, std::string error) noexcept {
    return {handle, std::move(path), std::move(error), ClosePolicy::manual};
}

DynamicLibrary::DynamicLibrary(DynamicLibrary&& other) noexcept
    : handle_(std::exchange(other.handle_, 0)), path_(std::move(other.path_)), error_(std::move(other.error_)), closePolicy_(other.closePolicy_) {}

DynamicLibrary& DynamicLibrary::operator=(DynamicLibrary&& other) noexcept {
    if (this != &other) {
        close();
        handle_ = std::exchange(other.handle_, 0);
        path_ = std::move(other.path_);
        error_ = std::move(other.error_);
        closePolicy_ = other.closePolicy_;
    }
    return *this;
}

DynamicLibrary::~DynamicLibrary() {
    close();
}

DynamicLibrary DynamicLibrary::open(std::string path, ClosePolicy closePolicy) {
    ::dlerror();
    const auto handle = reinterpret_cast<uint64_t>(::dlopen(path.c_str(), RTLD_NOW | RTLD_LOCAL));
    return {handle, std::move(path), handle ? std::string{} : currentDlError(), closePolicy};
}

DynamicLibrary DynamicLibrary::openConfigured(std::string_view name, const void* fallbackSymbol, ClosePolicy closePolicy) {
    auto lib = open(configuredLibraryPath(name), closePolicy);
    if (lib.loaded()) return lib;

    const auto fallback = fallbackLibraryPath(name, fallbackSymbol);
    if (fallback.empty() || fallback == lib.path()) return lib;

    auto local = open(fallback, closePolicy);
    return local.loaded() ? std::move(local) : std::move(lib);
}

void* DynamicLibrary::symbol(std::string_view name) const noexcept {
    if (!handle_) return nullptr;
    return ::dlsym(reinterpret_cast<void*>(handle_), std::string{name}.c_str());
}

uint64_t DynamicLibrary::release() noexcept {
    return std::exchange(handle_, 0);
}

void DynamicLibrary::close() noexcept {
    if (handle_ && closePolicy_ == ClosePolicy::automatic) ::dlclose(reinterpret_cast<void*>(handle_));
    handle_ = 0;
}

void DynamicLibrary::closeNow() noexcept {
    if (handle_) ::dlclose(reinterpret_cast<void*>(handle_));
    handle_ = 0;
}

std::string configuredLibraryPath(std::string_view name) {
    const auto baseSetting = ConfigManager::get().getSetting("library");
    if (baseSetting.empty()) return std::string{name};
    return FileUtils::joinPath(FileUtils::joinPath(ConfigManager::get().getScriptDir().string(), baseSetting), std::string{name});
}

std::string currentDlError() {
    if (const char* err = ::dlerror()) return err;
    return "dlopen failed";
}

}
