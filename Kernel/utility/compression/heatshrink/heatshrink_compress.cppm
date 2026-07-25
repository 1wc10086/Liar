module;
#include <cstdint>

export module utility.compression.heatshrink.heatshrink_compress;

import utility.compression.ncomp_core;

export namespace heatshrink_ns { class Compressor { public: [[nodiscard]] static auto compress(ncomp_ns::view_type input, uint8_t window_bits = 8, uint8_t lookahead_bits = 4) { return ncomp_ns::compress(ncomp_ns::Algorithm::heatshrink, input, (static_cast<int32_t>(window_bits) << 8) | lookahead_bits); } }; }
