import utility.io;
import tool.shell.plugin_base;
import tool.popcap.wwise.bnk.unpack;
import tool.popcap.wwise.bnk.pack;
#include <string_view>

namespace {

PluginResult result(bool ok) {
    return ok ? PluginResult::ok() : PluginResult::fail();
}

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();
        f.reg("audio.bnk.unpack", [](const Args& a) {
            return result(WwiseSoundBank::Unpack::unpack(
                a.get("InputFile"),
                a.get("OutputFolder")));
        });
        f.reg("audio.bnk.pack", [](const Args& a) {
            return result(WwiseSoundBank::Pack::packDirectory(
                a.get("InputFolder"),
                a.get("OutputFile")));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
