export module utility.compression.lzw.lzw_uncompress;
import utility.compression.ncomp_core;
export namespace lzw_ns { class Decompressor { public: [[nodiscard]] static auto decompress(ncomp_ns::view_type input, decltype(ncomp_ns::view_type{}.size()) expected_size = 0) { return ncomp_ns::decompress(ncomp_ns::Algorithm::lzw, input, expected_size); } }; }
