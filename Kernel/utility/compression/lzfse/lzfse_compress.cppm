export module utility.compression.lzfse.lzfse_compress;
import utility.compression.ncomp_core;
export namespace lzfse_ns { class Compressor { public: [[nodiscard]] static auto compress(ncomp_ns::view_type input, int level = 3) { return ncomp_ns::compress(ncomp_ns::Algorithm::lzfse, input, level); } }; }
