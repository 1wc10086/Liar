import utility.io;
import utility.md5.utils;
import tool.shell.plugin_base;
import tool.popcap.rton.decoder;
import tool.popcap.rton.encoder;
import tool.popcap.rton.utils;
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

RTONUtils::StringEncoding stringEncoding(const Args& a) {
    return a.opt("StringEncoding") == "EASCII" ? RTONUtils::StringEncoding::EASCII : RTONUtils::StringEncoding::UTF8;
}

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();
        f.reg("util.md5.compute", [](const Args& a) {
            auto hash = MD5Utils::computeFileHash(a.get("InputFile"));
            return result(hash && writeString(a, *hash));
        });
        f.reg("pvz2.rton.decode", [](const Args& a) {
            auto input = readBytes(a, "RTONFile");
            auto out = RTONDecoder::decode(input, a.opt("Key"), a.getBool("Encrypted"), stringEncoding(a));
            return result(writeString(a, out, "JSONFile"));
        });
        f.reg("pvz2.rton.encode", [](const Args& a) {
            auto out = RTONEncoder::encode(readString(a, "JSONFile"), a.opt("Key"), a.getBool("Encrypted"), stringEncoding(a));
            return result(writeBytes(a, out, "RTONFile"));
        });
        f.reg("pvz2.rton.decrypt", [](const Args& a) {
            auto input = readBytes(a);
            auto out = RTONUtils::decryptBytes(input, a.opt("Key", RTONUtils::DEFAULT_KEY));
            return result(writeBytes(a, out));
        });
        f.reg("pvz2.rton.encrypt", [](const Args& a) {
            auto input = readBytes(a);
            auto out = RTONUtils::encryptBytes(input, a.opt("Key", RTONUtils::DEFAULT_KEY));
            return result(writeBytes(a, out));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
