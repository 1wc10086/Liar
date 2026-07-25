import utility.io;
import tool.shell.plugin_base;
import tool.popcap.rendereffect.decode;
import tool.popcap.rendereffect.encode;

#include <cstdint>

namespace {

PluginResult result(bool success) {
    return success ? PluginResult::ok() : PluginResult::fail();
}

struct Registrar {
    Registrar() {
        auto& factory = PluginFactory::get();
        factory.reg("pvz2.rendereffect.decode", [](const Args& args) {
            const auto input = FileUtils::readFileBytes(args.get("InputFile"));
            const auto output = PopCap::RenderEffect::Decoder::decode_to_json(input, {
                static_cast<uint32_t>(args.getInt("VersionNumber", 1)),
                static_cast<uint32_t>(args.getInt("VersionVariant", 3))});
            return result(!output.empty() && FileUtils::writeTextFile(args.get("OutputFile"), output));
        });
        factory.reg("pvz2.rendereffect.encode", [](const Args& args) {
            const auto input = FileUtils::readTextFile(args.get("InputFile"));
            const auto output = PopCap::RenderEffect::Encoder::encode_from_json(input, {
                static_cast<uint32_t>(args.getInt("VersionNumber", 1)),
                static_cast<uint32_t>(args.getInt("VersionVariant", 3))});
            return result(!output.empty() && FileUtils::writeFileBytes(args.get("OutputFile"), output));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
