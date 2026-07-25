import tool.shell.plugin_base;
import tool.popcap.dz.pack;
import tool.popcap.dz.unpack;

namespace {

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();
        f.reg("pvz2.dz.unpack", [](const Args& a) {
            Dz::DzUnpacker::unpack(a.get("InputFile"), a.get("OutputFolder"));
            return PluginResult::ok();
        });
        f.reg("pvz2.dz.pack", [](const Args& a) {
            Dz::DzPacker::pack(a.get("InputFolder"), a.get("OutputFile"));
            return PluginResult::ok();
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
