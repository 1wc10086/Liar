module;
#include <string>
export module tool.popcap.bbone.definition;

export namespace BBone {

struct Rect {
    int x{};
    int y{};
    int w{};
    int h{};
};

struct Matrix {
    float a = 1.0f;
    float b = 0.0f;
    float c = 0.0f;
    float d = 1.0f;
    float tx = 0.0f;
    float ty = 0.0f;

    [[nodiscard]] constexpr Matrix mul(const Matrix& r) const noexcept {
        return {
            a * r.a + c * r.b,
            b * r.a + d * r.b,
            a * r.c + c * r.d,
            b * r.c + d * r.d,
            a * r.tx + c * r.ty + tx,
            b * r.tx + d * r.ty + ty
        };
    }
};

struct FrameEntry {
    std::string name;
    Matrix matrix;
    float alpha = 1.0f;
};

}
