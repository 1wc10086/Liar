import utility.io;
import tool.shell.plugin_base;
import tool.popcap.reanim.decoder;
import tool.popcap.reanim.encoder;
import tool.popcap.reanim.utils;
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
        f.reg("pvz2.reanim.decode", [](const Args& a) {
            auto out = Reanim::Decoder::decode(
                readBytes(a),
                Reanim::parseFormat(a.get("Format")),
                a.getBool("UseXml"),
                a.getBool("UseZlib", true));
            return result(writeString(a, out));
        });
        f.reg("pvz2.reanim.encode", [](const Args& a) {
            bool isXml = a.getBool("IsXml", a.getBool("UseXml"));
            auto out = Reanim::Encoder::encode(
                readString(a),
                Reanim::parseFormat(a.get("Format")),
                isXml,
                a.getBool("UseZlib", true));
            return result(writeBytes(a, out));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
