export module utility.compression.zpaq.zpaq_compress;
import utility.compression.ncomp_core;
export namespace zpaq_ns { class Compressor { public: [[nodiscard]] static auto compress(ncomp_ns::view_type input, int level = 1) { return ncomp_ns::compress(ncomp_ns::Algorithm::zpaq, input, level); } }; }
