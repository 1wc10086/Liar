export module utility.compression.zpaq.zpaq_uncompress;
import utility.compression.ncomp_core;
export namespace zpaq_ns { class Decompressor { public: [[nodiscard]] static auto decompress(ncomp_ns::view_type input, decltype(ncomp_ns::view_type{}.size()) expected_size = 0) { return ncomp_ns::decompress(ncomp_ns::Algorithm::zpaq, input, expected_size); } }; }
