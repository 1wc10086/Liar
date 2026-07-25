import utility.io;
import tool.shell.plugin_base;
import tool.popcap.particles.utils;
import tool.popcap.particles.decode;

namespace {

PluginResult result(bool success) {
    return success ? PluginResult::ok() : PluginResult::fail();
}

struct Registrar {
    Registrar() {
        auto& factory = PluginFactory::get();
        factory.reg("pvz2.particles.decode", [](const Args& args) {
            const auto input = FileUtils::readFileBytes(args.get("InputFile"));
            const auto output = Particles::ParticlesDecoder::decode(
                input,
                Particles::Utils::format(args.get("Format")),
                args.getBool("UseXml"),
                args.getBool("UseZlib", true));
            return result(!output.empty() && FileUtils::writeTextFile(args.get("OutputFile"), output));
        });
        factory.reg("pvz2.particles.encode", [](const Args& args) {
            const auto input = FileUtils::readTextFile(args.get("InputFile"));
            const auto output = Particles::ParticlesEncoder::encode(
                input,
                Particles::Utils::format(args.get("Format")),
                args.getBool("IsXml"),
                args.getBool("UseZlib", true));
            return result(!output.empty() && FileUtils::writeFileBytes(args.get("OutputFile"), output));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
