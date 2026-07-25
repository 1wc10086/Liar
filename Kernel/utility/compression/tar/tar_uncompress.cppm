module;
#include <span>

export module utility.compression.tar.tar_uncompress;

export import utility.compression.tar.tar_core;

export namespace tar_ns {

class Extractor {
public:
    [[nodiscard]] static bool extract(view_type input, EntryCallback callback, void* context = nullptr) noexcept {
        return tar_ns::extract(input, callback, context);
    }
};

}
