module;
#include <vector>

export module utility.serialization.tomlplusplus.tomlplusplus_serialization;

import utility.serialization.tomlplusplus.tomlplusplus_core;

export namespace tomlplusplus {
[[nodiscard]] inline std::vector<char> format(view_type input) { Document document{input}; if (!document) return {}; std::vector<char> output{input.size() * 2 + 256}; for (;;) { const auto size = document.format_to(output); if (size <= output.size()) { output.resize(size); return output; } output.resize(size); } }
[[nodiscard]] inline std::vector<char> json(view_type input) { Document document{input}; if (!document) return {}; std::vector<char> output{input.size() * 2 + 256}; for (;;) { const auto size = document.json_to(output); if (size <= output.size()) { output.resize(size); return output; } output.resize(size); } }
}
