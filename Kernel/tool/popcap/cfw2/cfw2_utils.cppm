module;
#include <string>
#include <string_view>
export module tool.popcap.cfw2.utils;
import utility.json;
import tool.popcap.cfw2.core;

export namespace CFW2 {

[[nodiscard]] inline bool isValidDefinition(std::string_view source) {
    const auto document = json::Document::parse(source);
    const auto root = document.root();
    return document && root && root.is_obj() && root.obj_get("ascent") && root.obj_get("height");
}

[[nodiscard]] inline std::string extensionOf(std::string_view path) {
    const auto dot = path.find_last_of('.');
    return dot == std::string_view::npos ? std::string{} : std::string(path.substr(dot));
}

[[nodiscard]] inline std::string withExtension(std::string_view path, std::string_view extension) {
    const auto dot = path.find_last_of('.');
    return dot == std::string_view::npos
        ? std::string(path) + std::string(extension)
        : std::string(path.substr(0, dot)) + std::string(extension);
}

}
