import utility.io;
import tool.shell.plugin_base;
import tool.popcap.trail.decode;
import tool.popcap.trail.encode;
import tool.popcap.trail.utils;

namespace {

PluginResult result(bool success) {
    return success ? PluginResult::ok() : PluginResult::fail();
}

struct Registrar {
    Registrar() {
        auto& factory = PluginFactory::get();
        factory.reg("pvz2.trail.decode", [](const Args& args) {
            auto output = Trail::decode(FileUtils::readFileBytes(args.get("InputFile")), Trail::parseFormat(args.get("Format")), args.getBool("UseXml"), args.getBool("UseZlib", true));
            return result(!output.empty() && FileUtils::writeTextFile(args.get("OutputFile"), output));
        });
        factory.reg("pvz2.trail.encode", [](const Args& args) {
            auto output = Trail::encode(FileUtils::readTextFile(args.get("InputFile")), Trail::parseFormat(args.get("Format")), args.getBool("IsXml"), args.getBool("UseZlib", true));
            return result(!output.empty() && FileUtils::writeFileBytes(args.get("OutputFile"), output));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
