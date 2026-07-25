module;
#include <cstddef>
#include <optional>
#include <utility>
#include <vector>

export module utility.serialization.lexbor.lexbor_html;

import utility.serialization.lexbor.lexbor_core;

export namespace lexbor {

[[nodiscard]] inline std::optional<std::vector<byte>> serialize(void* node, bool tree = true, bool pretty = false, size_t indent = 2) {
    std::vector<byte> output;
    const auto append = +[](view_type part, void* context) noexcept -> int {
        auto& target = *static_cast<std::vector<byte>*>(context);
        target.insert(target.end(), part.begin(), part.end());
        return 0;
    };
    return lexbor::serialize(node, tree, pretty, indent, append, &output) == status_ok ? std::optional<std::vector<byte>>{std::move(output)} : std::nullopt;
}

}
