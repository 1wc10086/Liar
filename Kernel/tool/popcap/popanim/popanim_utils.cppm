module;
#include <algorithm>
#include <array>
#include <charconv>
#include <cmath>
#include <cstdio>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.popanim.utils;
import utility.json;

export namespace PopAnim::Utils {

inline json::Value member(json::Value object, std::string_view key) noexcept {
    return object && object.is_obj() ? object.obj_get(key) : json::Value{};
}

inline json::Value element(json::Value array, size_t index) noexcept {
    return array && array.is_arr() ? array.arr_get(index) : json::Value{};
}

inline double number(json::Value value, double fallback = 0.0) noexcept {
    return value && value.is_num() ? value.get_num() : fallback;
}

inline double numberAt(json::Value array, size_t index, double fallback = 0.0) noexcept {
    return number(element(array, index), fallback);
}

inline int integer(json::Value value, int fallback = 0) noexcept {
    return value && value.is_num() ? static_cast<int>(value.get_num()) : fallback;
}

inline int integerAt(json::Value array, size_t index, int fallback = 0) noexcept {
    return integer(element(array, index), fallback);
}

inline bool boolean(json::Value value, bool fallback = false) noexcept {
    if (!value) return fallback;
    if (value.is_bool()) return value.get_bool();
    return value.is_num() ? value.get_num() != 0.0 : fallback;
}

inline std::string_view string(json::Value value, std::string_view fallback = {}) noexcept {
    return value && value.is_str() ? value.get_str_view() : fallback;
}

inline json::MutValue copy(json::MutDocument& doc, json::Value value) noexcept {
    return value ? doc.mut_copy(value) : doc.mut_null();
}

inline void add(json::MutDocument& doc, json::MutValue object, std::string_view key, json::MutValue value) noexcept {
    object.obj_add(doc.mut_str(key), value);
}

inline void addString(json::MutDocument& doc, json::MutValue object, std::string_view key, std::string_view value) noexcept {
    add(doc, object, key, value.empty() ? doc.mut_null() : doc.mut_strncpy(value.data(), value.size()));
}

inline std::string decimal(double value) {
    if (std::abs(value) < 1e-10) value = 0.0;
    char buffer[64];
    const auto [end, ec] = std::to_chars(std::begin(buffer), std::end(buffer), value, std::chars_format::fixed, 6);
    return ec == std::errc{} ? std::string(buffer, end) : "0";
}

inline constexpr int StandardResolution = 1200;
inline constexpr std::string_view XflContent = "PROXY-CS5";

inline std::array<double, 6> standardTransform(json::Value values) noexcept {
    std::array<double, 6> result{1.0, 0.0, 0.0, 1.0, 0.0, 0.0};
    const auto count = values && values.is_arr() ? values.arr_size() : 0;
    if (count == 2) { result[4] = numberAt(values, 0); result[5] = numberAt(values, 1); }
    else if (count == 3) {
        const auto angle = numberAt(values, 0);
        result = {std::cos(angle), std::sin(angle), -std::sin(angle), std::cos(angle), numberAt(values, 1), numberAt(values, 2)};
    } else if (count >= 6) for (size_t i = 0; i < 6; ++i) result[i] = numberAt(values, i);
    return result;
}

inline std::vector<double> variantTransform(const std::array<double, 6>& value) {
    if (std::abs(value[0] - value[3]) < 1e-6 && std::abs(value[1] + value[2]) < 1e-6) {
        if (std::abs(value[0] - 1.0) < 1e-6 && std::abs(value[1]) < 1e-6) return {value[4], value[5]};
        const auto a = std::acos(std::clamp(value[0], -1.0, 1.0));
        const auto b = std::asin(std::clamp(value[1], -1.0, 1.0));
        if (std::abs(std::abs(a) - std::abs(b)) <= 1e-2) return {b, value[4], value[5]};
    }
    return {value.begin(), value.end()};
}

}
