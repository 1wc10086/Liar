import utility.io;
import tool.shell.plugin_base;
import tool.popcap.popanim.decode;
import tool.popcap.popanim.encode;
#include <string_view>

namespace {

template<class Data>
bool writeBytes(const Args& args, const Data& data, std::string_view key = "OutputFile") {
    return !data.empty() && FileUtils::writeFileBytes(args.get(key), data);
}

bool writeText(const Args& args, const std::string& data, std::string_view key = "OutputFile") {
    return !data.empty() && FileUtils::writeTextFile(args.get(key), data);
}

PluginResult result(bool success) { return success ? PluginResult::ok() : PluginResult::fail(); }

struct Registrar {
    Registrar() {
        auto& factory = PluginFactory::get();
        factory.reg("pvz2.pam.decode", [](const Args& args) {
            return result(writeText(args, PopAnim::Decoder::decode(FileUtils::readFileBytes(args.get("InputFile")))));
        });
        factory.reg("pvz2.pam.encode", [](const Args& args) {
            return result(writeBytes(args, PopAnim::Encoder::encode(FileUtils::readTextFile(args.get("InputFile")))));
        });
        factory.reg("pvz2.pam.xfl.decode", [](const Args& args) {
            return result(writeText(args, PopAnim::XflDecoder::decode(args.get("InputFolder"))));
        });
        factory.reg("pvz2.pam.xfl.encode", [](const Args& args) {
            return result(PopAnim::XflEncoder::encode(FileUtils::readTextFile(args.get("InputFile")), args.get("OutputFolder"), args.getInt("Resolution", 768)));
        });
    }
};

[[maybe_unused]] const Registrar registrar;

}
