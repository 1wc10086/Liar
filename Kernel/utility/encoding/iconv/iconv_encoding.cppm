module;
#include <algorithm>
#include <array>
#include <cerrno>
#include <cstddef>
#include <limits>
#include <optional>
#include <string_view>
#include <vector>

export module utility.encoding.iconv.iconv_encoding;

import utility.encoding.iconv.iconv_core;

export namespace iconv_ns {

class Encoding {
public:
    [[nodiscard]] static std::optional<buffer_type> convert(view_type input, const char* from_encoding, const char* to_encoding) {
        if (!from_encoding || !to_encoding || input.size() > std::numeric_limits<size_t>::max() / 4) return std::nullopt;

        buffer_type output(std::max<size_t>(input.size() * 2, 64));
        for (;;) {
            const auto result = convert_to(to_encoding, from_encoding, input, output);
            if (result) {
                output.resize(result.output_written);
                return output;
            }
            if (result.error != E2BIG || output.size() > std::numeric_limits<size_t>::max() / 2) return std::nullopt;
            output.resize(output.size() * 2);
        }
    }

    [[nodiscard]] static std::optional<buffer_type> convert(view_type input, std::string_view from_encoding, std::string_view to_encoding) {
        if (from_encoding.find('\0') != std::string_view::npos || to_encoding.find('\0') != std::string_view::npos || from_encoding.size() >= 128 || to_encoding.size() >= 128) return std::nullopt;
        std::array<char, 128> from_name{};
        std::array<char, 128> to_name{};
        std::copy(from_encoding.begin(), from_encoding.end(), from_name.begin());
        std::copy(to_encoding.begin(), to_encoding.end(), to_name.begin());
        return convert(input, from_name.data(), to_name.data());
    }
};

}
