import tool.shell.plugin_base;
import tool.popcap.texture.ptx.ptx_core;
import tool.popcap.texture.cdat.cdat_core;
import tool.popcap.texture.ptxps3.ptxps3_core;
import tool.popcap.texture.ptxpsv.ptxpsv_core;
import tool.popcap.texture.ptxxbox360.ptxxbox360_core;
import tool.popcap.texture.tex.tex_core;
import tool.popcap.texture.textv.textv_core;
import tool.popcap.texture.txz.txz_core;
import tool.popcap.texture.xnb.xnb_core;
#include <string>
#include <string_view>

namespace {

struct Registrar {
    Registrar() {
        auto& f = PluginFactory::get();
        f.reg("pvz2.cdat.decode", [](const Args& a){ ImageCdatCodec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.cdat.encode", [](const Args& a){ ImageCdatCodec::encode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.ptx.decode", [](const Args& a){ ImagePtxCodec::decode(a.get("InputFile"), a.get("OutputFile"), a.getBool("UseHeader", true), std::string(a.opt("Format", "ARGB8888")), a.getInt("Width"), a.getInt("Height"), std::string(a.opt("Fmt0Mode", "ARGB"))); return PluginResult::ok(); });
        f.reg("pvz2.ptx.encode", [](const Args& a){ ImagePtxCodec::encode(a.get("InputFile"), a.get("OutputFile"), std::string(a.opt("Format", "ARGB8888")), a.getBool("WriteHeader", true), std::string(a.opt("Fmt0Mode", "ARGB"))); return PluginResult::ok(); });
        f.reg("pvz2.ptxps3.decode", [](const Args& a){ ImagePtxPS3Codec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.ptxps3.encode", [](const Args& a){ ImagePtxPS3Codec::encode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.ptxpsv.decode", [](const Args& a){ ImagePtxPSVCodec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.ptxpsv.encode", [](const Args& a){ ImagePtxPSVCodec::encode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.ptxxbox360.decode", [](const Args& a){ ImagePtxXbox360Codec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.ptxxbox360.encode", [](const Args& a){ ImagePtxXbox360Codec::encode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.tex.decode", [](const Args& a){ ImageTexCodec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.tex.encode", [](const Args& a){ ImageTexCodec::encode(a.get("InputFile"), a.get("OutputFile"), std::string(a.opt("Format", "ABGR8888"))); return PluginResult::ok(); });
        f.reg("pvz2.textv.decode", [](const Args& a){ ImageTexTVCodec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.textv.encode", [](const Args& a){ ImageTexTVCodec::encode(a.get("InputFile"), a.get("OutputFile"), std::string(a.opt("Format", "ARGB8888"))); return PluginResult::ok(); });
        f.reg("pvz2.txz.decode", [](const Args& a){ ImageTxzCodec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.txz.encode", [](const Args& a){ ImageTxzCodec::encode(a.get("InputFile"), a.get("OutputFile"), std::string(a.opt("Format", "ABGR8888"))); return PluginResult::ok(); });
        f.reg("pvz2.xnb.decode", [](const Args& a){ ImageXnbCodec::decode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
        f.reg("pvz2.xnb.encode", [](const Args& a){ ImageXnbCodec::encode(a.get("InputFile"), a.get("OutputFile")); return PluginResult::ok(); });
    }
};

[[maybe_unused]] const Registrar registrar;

}
