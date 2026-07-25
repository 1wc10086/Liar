export module utility.compression.bsc.bsc_uncompress;
import utility.compression.ncomp_core;
export namespace bsc_ns { class Decompressor { public: [[nodiscard]] static auto decompress(ncomp_ns::view_type input, decltype(ncomp_ns::view_type{}.size()) expected_size = 0) { return ncomp_ns::decompress(ncomp_ns::Algorithm::bsc, input, expected_size); } }; }
