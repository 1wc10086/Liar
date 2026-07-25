import utility.io;
import tool.shell.plugin_base;
import tool.popcap.particleeffect.decode;
import tool.popcap.particleeffect.encode;
#include <string>
#include <string_view>

namespace {

auto readBytes(const Args& a, std::string_view key = "InputFile") {
    return FileUtils::readFileBytes(a.get(key));
}

auto readString(const Args& a, std::string_view key = "InputFile") {
    return FileUtils::readTextFile(a.get(key));
}

template<class T>
bool writeBytes(const Args& a, const T& data, std::string_view key = "OutputFile") {
    return !data.empty() && FileUtils::writeFileBytes(a.get(key), data);
}

template<class T>
bool writeString(const Args& a, const T& data, std::string_view key = "OutputFile") {
    return !data.empty() && FileUtils::writeTextFile(a.get(key), data);
}

PluginResult result(bool ok) {
    return ok ? PluginResult::ok() : PluginResult::fail();
}

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();
        f.reg("popcap.particleeffect.decode", [](const Args& a) {
            auto input = readBytes(a);
            auto out = PopCap::ParticleEffect::decode(input);
            return result(writeString(a, out));
        });
        f.reg("popcap.particleeffect.encode", [](const Args& a) {
            auto out = PopCap::ParticleEffect::encode(readString(a));
            return result(writeBytes(a, out));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
