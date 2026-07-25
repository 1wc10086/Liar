import tool.shell.plugin_base;
import tool.popcap.bbone.decode;
import tool.popcap.bbone.encode;
#include <string_view>

namespace {

PluginResult result(bool ok) {
    return ok ? PluginResult::ok() : PluginResult::fail();
}

const std::string& arg(const Args& a, std::string_view key) {
    return a.get(key);
}

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();
        f.reg("pvz2.bbone.decode", [](const Args& a) {
            return result(BBone::Decoder::decode(arg(a, "InputFile"), arg(a, "OutputFolder")));
        });
        f.reg("pvz2.bbone.encode", [](const Args& a) {
            return result(BBone::Encoder::encode(arg(a, "InputFolder"), arg(a, "OutputFile")));
        });
        f.reg("pvz2.bbone.xfl.decode", [](const Args& a) {
            return result(BBone::XFL::Decoder::decode(arg(a, "InputFolder"), arg(a, "OutputFile")));
        });
        f.reg("pvz2.bbone.xfl.encode", [](const Args& a) {
            return result(BBone::XFL::Encoder::encode(a.m.contains("InputFile") ? arg(a, "InputFile") : arg(a, "InputFolder"), arg(a, "OutputFolder")));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
