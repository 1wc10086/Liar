module;
#include <cstdint>
#include <string_view>
export module tool.popcap.rton.core;

export namespace RTONUtils {

inline constexpr uint8_t MAGIC[2] = {0x10, 0x00};
inline constexpr uint8_t RTON_HEADER[8] = {82, 84, 79, 78, 1, 0, 0, 0};
inline constexpr uint8_t DONE_FOOTER[4] = {68, 79, 78, 69};
inline constexpr std::string_view DEFAULT_KEY = "";

enum class StringEncoding : uint8_t { UTF8 = 0, EASCII = 1 };

}
