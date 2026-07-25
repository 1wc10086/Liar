export module utility.compression.fastlz.fastlz_uncompress;
import utility.compression.ncomp_core;
export namespace fastlz_ns { class Decompressor { public: [[nodiscard]] static auto decompress(ncomp_ns::view_type input, decltype(ncomp_ns::view_type{}.size()) expected_size = 0) { return ncomp_ns::decompress(ncomp_ns::Algorithm::fastlz, input, expected_size); } }; }
