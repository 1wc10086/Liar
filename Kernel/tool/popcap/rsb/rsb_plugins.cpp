import utility.io;
import tool.shell.plugin_base;
import tool.popcap.rsb.rsb_pack;
import tool.popcap.rsb.rsb_unpack;
#include <string>
#include <string_view>

namespace {

[[nodiscard]] std::string getStr(const Args& a, std::string_view k) {
    return a.get(k);
}

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();

        f.reg("pvz2.rsb.unpack", [](const Args& a) {
            Rsb::unpack(
                getStr(a, "InputFile"),
                getStr(a, "OutputFolder"),
                a.getBool("ChangeImage"),
                a.getBool("DeleteAfterConvert"),
                a.opt("Fmt0Mode", "ARGB")
            );
            return PluginResult::ok();
        });

        f.reg("pvz2.rsb.pack", [](const Args& a) {
            Rsb::pack(
                getStr(a, "InputFolder"),
                getStr(a, "OutputFile")
            );
            return PluginResult::ok();
        });

        f.reg("pvz2.rsb.unpack_lenient", [](const Args& a) {
            Rsb::unpackLenient(
                getStr(a, "InputFile"),
                getStr(a, "OutputFolder"),
                a.getBool("ChangeImage"),
                a.getBool("DeleteAfterConvert"),
                a.opt("Fmt0Mode", "ARGB")
            );
            return PluginResult::ok();
        });
    }
};

[[maybe_unused]] const Registrar registrar;

} 