import utility.io;
import tool.shell.plugin_base;
import tool.popcap.pak.pack;
import tool.popcap.pak.unpack;

namespace {

struct Registrar {
    Registrar() {
        auto& factory = PluginFactory::get();
        factory.reg("pvz2.pak.decode", [](const Args& args) {
            Pak::Unpack::unpack(args.get("InputFile"), args.get("OutputFolder"));
            return PluginResult::ok();
        });
        factory.reg("pvz2.pak.encode", [](const Args& args) {
            Pak::Pack::pack(args.get("InputFolder"), args.get("OutputFile"));
            return PluginResult::ok();
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
