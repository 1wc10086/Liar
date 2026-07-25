export module utility.compression.fastlz.fastlz_compress;
import utility.compression.ncomp_core;
export namespace fastlz_ns { class Compressor { public: [[nodiscard]] static auto compress(ncomp_ns::view_type input, int level = 2) { return ncomp_ns::compress(ncomp_ns::Algorithm::fastlz, input, level); } }; }
