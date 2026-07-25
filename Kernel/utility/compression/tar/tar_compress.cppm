module;
#include <span>
#include <string_view>

export module utility.compression.tar.tar_compress;
export import utility.compression.tar.tar_core;

export namespace tar_ns { class Compressor { public: [[nodiscard]] static auto create_file(view_type input, std::string_view name) { return tar_ns::create_file(input, name); } [[nodiscard]] static auto create(std::span<const InputEntry> entries) { return tar_ns::create(entries); } }; }
