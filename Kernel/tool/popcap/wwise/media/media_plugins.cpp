import tool.shell.plugin_base;
import tool.popcap.wwise.media.decode;

namespace {

struct Registrar {
    Registrar() {
        PluginFactory::get().reg("pvz2.wwise.wem.decode", [](const Args& args) {
            WwiseMedia::Decoder::decode(
                args.get("WEMFile"),
                args.get("OGGFile"),
                args.opt("PackedCodebooks", "packed_codebooks.bin"));
            return PluginResult::ok();
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
