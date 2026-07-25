module;
#include <algorithm>
#include <array>
#include <cstdint>
#include <expected>
#include <stdexcept>
#include <string>
#include <string_view>
#include <tuple>
#include <type_traits>
#include <vector>
export module tool.popcap.particleeffect.core;
import utility.json;

export namespace PopCap::ParticleEffect {

struct Vec2 { float x{}; float y{}; };
struct Size2 { int width{}; int height{}; };
struct Color { int red{}; int green{}; int blue{}; };

struct Value1Point {
    float time{};
    float value{};
    std::array<Vec2, 2> control_value{};
};

struct Value1 {
    bool control{};
    std::vector<Value1Point> point;
};

struct Value2Point {
    int time{};
    Vec2 value{};
    std::array<Vec2, 2> control_value{};
};

struct Value2 {
    float unknown_1{};
    float unknown_2{};
    bool control{};
    std::vector<Value2Point> point;
};

struct ColorPoint { float time{}; Color value{}; };
struct AlphaPoint { float time{}; int value{}; };

struct Texture {
    std::string name;
    std::string path;
    int cell{};
    int row{};
    bool padded{};
};

struct EmitterParticle {
    std::string name;
    int texture{};
    bool instance{};
    bool single{};
    bool flip_horizontal{};
    bool flip_vertical{};
    bool lock_aspect{};
    int animation_speed{};
    bool animation_start_on_random_frame{};
    bool use_emitter_angle_and_range{};
    bool attach_to_emitter{};
    float attach_value{};
    Vec2 reference_point_offset{};
    int angle_value{};
    int angle_range{};
    int angle_offset{};
    bool angle_random_align{};
    int angle_align_offset{};
    bool angle_align_to_motion{};
    bool angle_keep_aligned_to_motion{};
    int repeat_color{};
    bool random_gradient_color{};
    int number_of_each_color{};
    bool use_key_color_only{};
    bool use_next_color_key{};
    bool get_color_from_layer{};
    bool update_color_from_layer{};
    std::vector<ColorPoint> color;
    int repeat_alpha{};
    bool preserve_color{};
    bool link_transparency_to_color{};
    bool get_transparency_from_layer{};
    bool update_transparency_from_layer{};
    std::vector<AlphaPoint> alpha;
    std::vector<Value1> value;
    int unknown_1{};
    int unknown_2{};
    int unknown_3{};
    float unknown_4{};
    int unknown_5{};
    int unknown_6{};
    int unknown_7{};
    int unknown_8{};
    int unknown_9{};
    int unknown_10{};
    int unknown_11{};
    int unknown_12{};
    int unknown_13{};
    int unknown_14{};
    int unknown_15{};
    int unknown_16{};
    int unknown_17{};
    int unknown_18{};
    int unknown_19{};
    int unknown_20{};
    Value1 unknown_21{};
};

struct Emitter {
    std::string name;
    bool keep_in_order{};
    bool oldest_in_front{};
    std::vector<Value1> value;
    std::vector<EmitterParticle> particle;
    int unknown_1{};
    int unknown_2{};
    int unknown_3{};
    int unknown_4{};
    int unknown_5{};
};

struct LayerEmitter {
    std::string name;
    int type{};
    int geom{};
    bool geom_4_if_2{};
    bool is_super{};
    int preload_frame{};
    bool emit_in{};
    bool emit_out{};
    std::array<int, 2> emit_at_point{};
    Color tint_color{};
    bool mask{};
    std::string mask_name;
    std::vector<std::string> mask_path;
    bool invert_mask{};
    Value2 position{};
    std::vector<Value2> point;
    std::vector<Value1> value;
    std::vector<int> free;
    float unknown_1{};
    float unknown_2{};
    float unknown_3{};
    float unknown_4{};
    float unknown_5{};
    float unknown_6{};
    float unknown_7{};
    float unknown_8{};
    float unknown_9{};
    float unknown_10{};
    float unknown_11{};
    float unknown_12{};
    int unknown_13{};
    int unknown_14{};
    int unknown_15{};
    float unknown_16{};
    float unknown_17{};
    int unknown_18{};
    int unknown_19{};
    int unknown_20{};
    int unknown_21{};
    int unknown_22{};
    int unknown_23{};
    int unknown_24{};
    int unknown_25{};
    float unknown_26{};
    float unknown_27{};
};

struct LayerDeflector {
    std::string name;
    int bounce{};
    int hit{};
    int thickness{};
    bool visible{};
    Value2 position{};
    Value1 active{};
    Value1 angle{};
    std::vector<Value2> point;
};

struct LayerBlocker {
    std::string name;
    Value2 position{};
    Value1 active{};
    Value1 angle{};
    std::vector<Value2> point;
    int unknown_1{};
    int unknown_2{};
    int unknown_3{};
    int unknown_4{};
    int unknown_5{};
};

struct LayerForce {
    std::string name;
    bool visible{};
    Value2 position{};
    Value1 active{};
    Value1 strength{};
    Value1 width{};
    Value1 height{};
    Value1 angle{};
    Value1 direction{};
    Value1 unknown_1{};
};

struct Layer {
    std::string name;
    std::vector<LayerEmitter> emitter;
    std::vector<LayerDeflector> deflector;
    std::vector<LayerBlocker> blocker;
    std::vector<LayerForce> force;
    Value2 offset{};
    Value1 angle{};
    std::string unknown_1;
    std::vector<std::string> unknown_2;
    std::vector<int> unknown_3;
    std::vector<int> unknown_4;
    std::vector<int> unknown_5;
};

struct Effect {
    std::string note;
    Size2 size{};
    int frame_rate{};
    std::array<int, 2> frame_range{};
    Color background_color{};
    std::vector<uint8_t> startup_state;
    std::vector<Texture> texture;
    std::vector<Emitter> emitter;
    std::vector<Layer> layer;
    int unknown_1{};
    int unknown_2{};
    int unknown_3{};
    int unknown_4{};
    int unknown_5{};
    int unknown_6{};
    int unknown_7{};
    int unknown_8{};
    int unknown_9{};
    int unknown_10{};
    std::string unknown_11;
    int unknown_12{};
    int unknown_13{};
    int unknown_14{};
};

namespace Json {

using JVal = ::json::Value;
using JMVal = ::json::MutValue;
using JMDoc = ::json::MutDocument;

template<class C, class M>
struct Field { std::string_view name; M C::* ptr; };

template<class C, class M>
Field(std::string_view, M C::*) -> Field<C, M>;

template<class T> constexpr auto fields();

template<class T> struct IsObject : std::false_type {};
template<class T> inline constexpr bool IsObjectV = IsObject<T>::value;

template<class T> struct IsVector : std::false_type {};
template<class T, class A> struct IsVector<std::vector<T, A>> : std::true_type {};
template<class T> inline constexpr bool IsVectorV = IsVector<std::remove_cvref_t<T>>::value;

template<class T> struct IsArray : std::false_type {};
template<class T, size_t N> struct IsArray<std::array<T, N>> : std::true_type {};
template<class T> inline constexpr bool IsArrayV = IsArray<std::remove_cvref_t<T>>::value;

template<> struct IsObject<Vec2> : std::true_type {};
template<> struct IsObject<Size2> : std::true_type {};
template<> struct IsObject<Color> : std::true_type {};
template<> struct IsObject<Value1Point> : std::true_type {};
template<> struct IsObject<Value1> : std::true_type {};
template<> struct IsObject<Value2Point> : std::true_type {};
template<> struct IsObject<Value2> : std::true_type {};
template<> struct IsObject<ColorPoint> : std::true_type {};
template<> struct IsObject<AlphaPoint> : std::true_type {};
template<> struct IsObject<Texture> : std::true_type {};
template<> struct IsObject<EmitterParticle> : std::true_type {};
template<> struct IsObject<Emitter> : std::true_type {};
template<> struct IsObject<LayerEmitter> : std::true_type {};
template<> struct IsObject<LayerDeflector> : std::true_type {};
template<> struct IsObject<LayerBlocker> : std::true_type {};
template<> struct IsObject<LayerForce> : std::true_type {};
template<> struct IsObject<Layer> : std::true_type {};
template<> struct IsObject<Effect> : std::true_type {};

template<class T>
JMVal toJson(JMDoc& doc, const T& v);

template<class T>
void fromJson(JVal val, T& out);

template<class T>
JMVal toArr(JMDoc& doc, const T& v) {
    auto a = doc.mut_arr();
    for (auto& e : v) a.arr_append(toJson(doc, e));
    return a;
}

template<class T>
void fromArr(JVal val, T& out) {
    if constexpr (requires { out.resize(size_t{}); }) {
        auto n = val.arr_size();
        out.resize(n);
        for (size_t i = 0; i < n; ++i) fromJson(val.arr_get(i), out[i]);
    } else {
        for (size_t i = 0; i < out.size(); ++i) fromJson(val.arr_get(i), out[i]);
    }
}

template<class T>
JMVal toObj(JMDoc& doc, const T& v) {
    auto o = doc.mut_obj();
    std::apply([&](auto... f) {
        (o.obj_add(doc.mut_str(f.name), toJson(doc, v.*(f.ptr))), ...);
    }, fields<T>());
    return o;
}

template<class T>
void fromObj(JVal val, T& out) {
    std::apply([&](auto... f) {
        (fromJson(val.obj_get(f.name), out.*(f.ptr)), ...);
    }, fields<T>());
}

template<class T>
JMVal toJson(JMDoc& doc, const T& v) {
    using U = std::remove_cvref_t<T>;
    if constexpr (std::same_as<U, bool>) return doc.mut_bool(v);
    else if constexpr (std::same_as<U, std::string>) return doc.mut_str(v);
    else if constexpr (std::floating_point<U>) return doc.mut_real(v);
    else if constexpr (std::signed_integral<U>) return doc.mut_int(v);
    else if constexpr (std::unsigned_integral<U>) return doc.mut_uint(v);
    else if constexpr (IsVectorV<U> || IsArrayV<U>) return toArr(doc, v);
    else if constexpr (IsObjectV<U>) return toObj(doc, v);
    else static_assert(sizeof(U) == 0, "unsupported particle effect json type");
}

template<class T>
void fromJson(JVal val, T& out) {
    using U = std::remove_cvref_t<T>;
    if constexpr (std::same_as<U, bool>) out = val.get_bool();
    else if constexpr (std::same_as<U, std::string>) out = val.get_str_view();
    else if constexpr (std::floating_point<U>) out = static_cast<U>(val.get_num());
    else if constexpr (std::signed_integral<U>) out = static_cast<U>(val.is_uint() ? static_cast<int64_t>(val.get_uint()) : val.get_sint());
    else if constexpr (std::unsigned_integral<U>) out = static_cast<U>(val.is_uint() ? val.get_uint() : static_cast<uint64_t>(val.get_sint()));
    else if constexpr (IsVectorV<U> || IsArrayV<U>) fromArr(val, out);
    else if constexpr (IsObjectV<U>) fromObj(val, out);
    else static_assert(sizeof(U) == 0, "unsupported particle effect json type");
}

#define F(name, ptr) Field{name, ptr}

template<> constexpr auto fields<Vec2>() {
    return std::tuple{F("x", &Vec2::x), F("y", &Vec2::y)};
}

template<> constexpr auto fields<Size2>() {
    return std::tuple{F("width", &Size2::width), F("height", &Size2::height)};
}

template<> constexpr auto fields<Color>() {
    return std::tuple{F("red", &Color::red), F("green", &Color::green), F("blue", &Color::blue)};
}

template<> constexpr auto fields<Value1Point>() {
    return std::tuple{F("time", &Value1Point::time), F("value", &Value1Point::value), F("control_value", &Value1Point::control_value)};
}

template<> constexpr auto fields<Value1>() {
    return std::tuple{F("control", &Value1::control), F("point", &Value1::point)};
}

template<> constexpr auto fields<Value2Point>() {
    return std::tuple{F("time", &Value2Point::time), F("value", &Value2Point::value), F("control_value", &Value2Point::control_value)};
}

template<> constexpr auto fields<Value2>() {
    return std::tuple{F("unknown_1", &Value2::unknown_1), F("unknown_2", &Value2::unknown_2), F("control", &Value2::control), F("point", &Value2::point)};
}

template<> constexpr auto fields<ColorPoint>() {
    return std::tuple{F("time", &ColorPoint::time), F("value", &ColorPoint::value)};
}

template<> constexpr auto fields<AlphaPoint>() {
    return std::tuple{F("time", &AlphaPoint::time), F("value", &AlphaPoint::value)};
}

template<> constexpr auto fields<Texture>() {
    return std::tuple{F("name", &Texture::name), F("path", &Texture::path), F("cell", &Texture::cell), F("row", &Texture::row), F("padded", &Texture::padded)};
}

template<> constexpr auto fields<EmitterParticle>() {
    return std::tuple{
        F("name", &EmitterParticle::name), F("texture", &EmitterParticle::texture),
        F("instance", &EmitterParticle::instance), F("single", &EmitterParticle::single),
        F("flip_horizontal", &EmitterParticle::flip_horizontal), F("flip_vertical", &EmitterParticle::flip_vertical),
        F("lock_aspect", &EmitterParticle::lock_aspect), F("animation_speed", &EmitterParticle::animation_speed),
        F("animation_start_on_random_frame", &EmitterParticle::animation_start_on_random_frame),
        F("use_emitter_angle_and_range", &EmitterParticle::use_emitter_angle_and_range),
        F("attach_to_emitter", &EmitterParticle::attach_to_emitter), F("attach_value", &EmitterParticle::attach_value),
        F("reference_point_offset", &EmitterParticle::reference_point_offset),
        F("angle_value", &EmitterParticle::angle_value), F("angle_range", &EmitterParticle::angle_range),
        F("angle_offset", &EmitterParticle::angle_offset), F("angle_random_align", &EmitterParticle::angle_random_align),
        F("angle_align_offset", &EmitterParticle::angle_align_offset), F("angle_align_to_motion", &EmitterParticle::angle_align_to_motion),
        F("angle_keep_aligned_to_motion", &EmitterParticle::angle_keep_aligned_to_motion),
        F("repeat_color", &EmitterParticle::repeat_color), F("random_gradient_color", &EmitterParticle::random_gradient_color),
        F("number_of_each_color", &EmitterParticle::number_of_each_color), F("use_key_color_only", &EmitterParticle::use_key_color_only),
        F("use_next_color_key", &EmitterParticle::use_next_color_key), F("get_color_from_layer", &EmitterParticle::get_color_from_layer),
        F("update_color_from_layer", &EmitterParticle::update_color_from_layer), F("color", &EmitterParticle::color),
        F("repeat_alpha", &EmitterParticle::repeat_alpha), F("preserve_color", &EmitterParticle::preserve_color),
        F("link_transparency_to_color", &EmitterParticle::link_transparency_to_color),
        F("get_transparency_from_layer", &EmitterParticle::get_transparency_from_layer),
        F("update_transparency_from_layer", &EmitterParticle::update_transparency_from_layer),
        F("alpha", &EmitterParticle::alpha), F("value", &EmitterParticle::value),
        F("unknown_1", &EmitterParticle::unknown_1), F("unknown_2", &EmitterParticle::unknown_2),
        F("unknown_3", &EmitterParticle::unknown_3), F("unknown_4", &EmitterParticle::unknown_4),
        F("unknown_5", &EmitterParticle::unknown_5), F("unknown_6", &EmitterParticle::unknown_6),
        F("unknown_7", &EmitterParticle::unknown_7), F("unknown_8", &EmitterParticle::unknown_8),
        F("unknown_9", &EmitterParticle::unknown_9), F("unknown_10", &EmitterParticle::unknown_10),
        F("unknown_11", &EmitterParticle::unknown_11), F("unknown_12", &EmitterParticle::unknown_12),
        F("unknown_13", &EmitterParticle::unknown_13), F("unknown_14", &EmitterParticle::unknown_14),
        F("unknown_15", &EmitterParticle::unknown_15), F("unknown_16", &EmitterParticle::unknown_16),
        F("unknown_17", &EmitterParticle::unknown_17), F("unknown_18", &EmitterParticle::unknown_18),
        F("unknown_19", &EmitterParticle::unknown_19), F("unknown_20", &EmitterParticle::unknown_20),
        F("unknown_21", &EmitterParticle::unknown_21)
    };
}

template<> constexpr auto fields<Emitter>() {
    return std::tuple{
        F("name", &Emitter::name), F("keep_in_order", &Emitter::keep_in_order),
        F("oldest_in_front", &Emitter::oldest_in_front), F("value", &Emitter::value),
        F("particle", &Emitter::particle), F("unknown_1", &Emitter::unknown_1),
        F("unknown_2", &Emitter::unknown_2), F("unknown_3", &Emitter::unknown_3),
        F("unknown_4", &Emitter::unknown_4), F("unknown_5", &Emitter::unknown_5)
    };
}

template<> constexpr auto fields<LayerEmitter>() {
    return std::tuple{
        F("name", &LayerEmitter::name), F("type", &LayerEmitter::type), F("geom", &LayerEmitter::geom),
        F("geom_4_if_2", &LayerEmitter::geom_4_if_2), F("is_super", &LayerEmitter::is_super),
        F("preload_frame", &LayerEmitter::preload_frame), F("emit_in", &LayerEmitter::emit_in),
        F("emit_out", &LayerEmitter::emit_out), F("emit_at_point", &LayerEmitter::emit_at_point),
        F("tint_color", &LayerEmitter::tint_color), F("mask", &LayerEmitter::mask),
        F("mask_name", &LayerEmitter::mask_name), F("mask_path", &LayerEmitter::mask_path),
        F("invert_mask", &LayerEmitter::invert_mask), F("position", &LayerEmitter::position),
        F("point", &LayerEmitter::point), F("value", &LayerEmitter::value), F("free", &LayerEmitter::free),
        F("unknown_1", &LayerEmitter::unknown_1), F("unknown_2", &LayerEmitter::unknown_2),
        F("unknown_3", &LayerEmitter::unknown_3), F("unknown_4", &LayerEmitter::unknown_4),
        F("unknown_5", &LayerEmitter::unknown_5), F("unknown_6", &LayerEmitter::unknown_6),
        F("unknown_7", &LayerEmitter::unknown_7), F("unknown_8", &LayerEmitter::unknown_8),
        F("unknown_9", &LayerEmitter::unknown_9), F("unknown_10", &LayerEmitter::unknown_10),
        F("unknown_11", &LayerEmitter::unknown_11), F("unknown_12", &LayerEmitter::unknown_12),
        F("unknown_13", &LayerEmitter::unknown_13), F("unknown_14", &LayerEmitter::unknown_14),
        F("unknown_15", &LayerEmitter::unknown_15), F("unknown_16", &LayerEmitter::unknown_16),
        F("unknown_17", &LayerEmitter::unknown_17), F("unknown_18", &LayerEmitter::unknown_18),
        F("unknown_19", &LayerEmitter::unknown_19), F("unknown_20", &LayerEmitter::unknown_20),
        F("unknown_21", &LayerEmitter::unknown_21), F("unknown_22", &LayerEmitter::unknown_22),
        F("unknown_23", &LayerEmitter::unknown_23), F("unknown_24", &LayerEmitter::unknown_24),
        F("unknown_25", &LayerEmitter::unknown_25), F("unknown_26", &LayerEmitter::unknown_26),
        F("unknown_27", &LayerEmitter::unknown_27)
    };
}

template<> constexpr auto fields<LayerDeflector>() {
    return std::tuple{
        F("name", &LayerDeflector::name), F("bounce", &LayerDeflector::bounce), F("hit", &LayerDeflector::hit),
        F("thickness", &LayerDeflector::thickness), F("visible", &LayerDeflector::visible),
        F("position", &LayerDeflector::position), F("active", &LayerDeflector::active),
        F("angle", &LayerDeflector::angle), F("point", &LayerDeflector::point)
    };
}

template<> constexpr auto fields<LayerBlocker>() {
    return std::tuple{
        F("name", &LayerBlocker::name), F("position", &LayerBlocker::position),
        F("active", &LayerBlocker::active), F("angle", &LayerBlocker::angle),
        F("point", &LayerBlocker::point), F("unknown_1", &LayerBlocker::unknown_1),
        F("unknown_2", &LayerBlocker::unknown_2), F("unknown_3", &LayerBlocker::unknown_3),
        F("unknown_4", &LayerBlocker::unknown_4), F("unknown_5", &LayerBlocker::unknown_5)
    };
}

template<> constexpr auto fields<LayerForce>() {
    return std::tuple{
        F("name", &LayerForce::name), F("visible", &LayerForce::visible), F("position", &LayerForce::position),
        F("active", &LayerForce::active), F("strength", &LayerForce::strength), F("width", &LayerForce::width),
        F("height", &LayerForce::height), F("angle", &LayerForce::angle), F("direction", &LayerForce::direction),
        F("unknown_1", &LayerForce::unknown_1)
    };
}

template<> constexpr auto fields<Layer>() {
    return std::tuple{
        F("name", &Layer::name), F("emitter", &Layer::emitter), F("deflector", &Layer::deflector),
        F("blocker", &Layer::blocker), F("force", &Layer::force), F("offset", &Layer::offset),
        F("angle", &Layer::angle), F("unknown_1", &Layer::unknown_1), F("unknown_2", &Layer::unknown_2),
        F("unknown_3", &Layer::unknown_3), F("unknown_4", &Layer::unknown_4), F("unknown_5", &Layer::unknown_5)
    };
}

template<> constexpr auto fields<Effect>() {
    return std::tuple{
        F("note", &Effect::note), F("size", &Effect::size), F("frame_rate", &Effect::frame_rate),
        F("frame_range", &Effect::frame_range), F("background_color", &Effect::background_color),
        F("startup_state", &Effect::startup_state), F("texture", &Effect::texture),
        F("emitter", &Effect::emitter), F("layer", &Effect::layer),
        F("unknown_1", &Effect::unknown_1), F("unknown_2", &Effect::unknown_2),
        F("unknown_3", &Effect::unknown_3), F("unknown_4", &Effect::unknown_4),
        F("unknown_5", &Effect::unknown_5), F("unknown_6", &Effect::unknown_6),
        F("unknown_7", &Effect::unknown_7), F("unknown_8", &Effect::unknown_8),
        F("unknown_9", &Effect::unknown_9), F("unknown_10", &Effect::unknown_10),
        F("unknown_11", &Effect::unknown_11), F("unknown_12", &Effect::unknown_12),
        F("unknown_13", &Effect::unknown_13), F("unknown_14", &Effect::unknown_14)
    };
}

#undef F

inline std::string toJsonText(const Effect& effect) {
    JMDoc doc;
    doc.set_root(toJson(doc, effect));
    return doc.write(::json::WriteFlag::Pretty);
}

inline Effect fromJsonText(std::string_view text) {
    auto doc = ::json::Document::parse(text,
        ::json::ReadFlag::AllowComments |
        ::json::ReadFlag::AllowTrailingCommas |
        ::json::ReadFlag::AllowInfAndNan);
    if (!doc) throw std::runtime_error("invalid particle effect json");
    Effect effect;
    fromJson(doc.root(), effect);
    return effect;
}

}

}
