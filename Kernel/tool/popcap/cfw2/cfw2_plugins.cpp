import utility.io;
import tool.shell.plugin_base;
import tool.popcap.cfw2.decode;
import tool.popcap.cfw2.encode;

namespace {

PluginResult result(bool success) {
    return success ? PluginResult::ok() : PluginResult::fail();
}

struct Registrar {
    Registrar() {
        auto& factory = PluginFactory::get();
        factory.reg("pvz2.cfw2.decode", [](const Args& args) {
            const auto data = FileUtils::readFileBytes(args.get("InputFile"));
            const auto output = CFW2::decode(data, args.getBool("PreserveHeader"));
            return result(!output.empty() && FileUtils::writeTextFile(args.get("OutputFile"), output));
        });
        factory.reg("pvz2.cfw2.encode", [](const Args& args) {
            const auto source = FileUtils::readTextFile(args.get("InputFile"));
            const auto output = CFW2::encode(source);
            return result(!output.empty() && FileUtils::writeFileBytes(args.get("OutputFile"), output));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
