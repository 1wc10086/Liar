module;
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <span>
#include <stdexcept>
#include <string>
#include <string_view>
#include <tuple>
#include <type_traits>
#include <vector>
export module tool.popcap.particleeffect.utils;
import utility.binary.unified_binary_stream;
import tool.popcap.particleeffect.core;
import tool.popcap.particleeffect.definition;

export namespace PopCap::ParticleEffect {

inline constexpr size_t pidx(size_t one_based) noexcept {
    return one_based - 1;
}

inline bool feq(float a, float b) noexcept {
    return a == b;
}

class Reader {
public:
    explicit Reader(std::span<const uint8_t> data) : s_(data) {}

    [[nodiscard]] uint8_t u8() { return s_.readUInt8(); }
    [[nodiscard]] int16_t s16() { return s_.readInt16(); }
    [[nodiscard]] uint16_t u16() { return s_.readUInt16(); }
    [[nodiscard]] int32_t s32() { return s_.readInt32(); }
    [[nodiscard]] uint32_t u32() { return s_.readUInt32(); }
    [[nodiscard]] float f32() { return s_.readFloat32(); }
    [[nodiscard]] bool b8() { return s_.readBool(); }

    [[nodiscard]] std::string str8() { return s_.readStringByUInt8Head(); }

    [[nodiscard]] std::vector<uint8_t> bytes32() {
        auto n = u32();
        return s_.readBytes(n);
    }

    void magic() {
        for (auto b : kPpfMagic)
            if (u8() != b) throw std::runtime_error("invalid ppf magic");
        if (u32() != kPpfVersion) throw std::runtime_error("unsupported ppf version");
    }

    [[nodiscard]] bool error() const noexcept { return s_.hasErrorOccurred(); }

private:
    UnifiedBinaryStream s_;
};

class Writer {
public:
    Writer() : s_(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little) {}

    void u8(int v) { s_.writeUInt8(static_cast<uint8_t>(v)); }
    void s16(int v) { s_.writeInt16(static_cast<int16_t>(v)); }
    void u16(size_t v) { s_.writeUInt16(static_cast<uint16_t>(v)); }
    void s32(int v) { s_.writeInt32(static_cast<int32_t>(v)); }
    void u32(int v) { s_.writeUInt32(static_cast<uint32_t>(v)); }
    void f32(float v) { s_.writeFloat32(v); }
    void b8(bool v) { s_.writeBool(v); }

    void str8(std::string_view v) {
        if (v.size() > 255) throw std::runtime_error("string too long for uint8 length");
        s_.writeStringByUInt8Head(v);
    }

    void bytes32(std::span<const uint8_t> v) {
        s_.writeUInt32(static_cast<uint32_t>(v.size()));
        if (!v.empty()) s_.writeBytes(v);
    }

    void magic() {
        s_.writeBytes(kPpfMagic.data(), kPpfMagic.size());
        s_.writeUInt32(kPpfVersion);
    }

    [[nodiscard]] std::vector<uint8_t> take() const { return s_.toByteArray(); }

    [[nodiscard]] bool error() const noexcept { return s_.hasErrorOccurred(); }

private:
    UnifiedBinaryStream s_;
};

template<class T, class F>
void readList(Reader& r, std::vector<T>& out, F&& f, size_t size, size_t actualSize) {
    out.clear();
    out.resize(actualSize);
    for (size_t i = 0; i < size; ++i) f(r, out[i]);
}

template<class T, class F>
void readList(Reader& r, std::vector<T>& out, F&& f, size_t size) {
    readList(r, out, std::forward<F>(f), size, size);
}

template<class T, class F>
void readList16(Reader& r, std::vector<T>& out, F&& f) {
    readList(r, out, std::forward<F>(f), r.u16());
}

template<class T, class F>
void writeList(Writer& w, const std::vector<T>& in, F&& f, size_t size) {
    if (in.size() < size) throw std::runtime_error("list size is too small");
    for (size_t i = 0; i < size; ++i) f(w, in[i]);
}

template<class T, class F>
void writeList16(Writer& w, const std::vector<T>& in, F&& f) {
    if (in.size() > 65535) throw std::runtime_error("list too large");
    w.u16(in.size());
    writeList(w, in, std::forward<F>(f), in.size());
}

void readValue1(Reader& r, Value1& v) {
    auto countWithFlag = r.u8();
    auto count = static_cast<size_t>(countWithFlag & 0b111);
    if (count == 0b111) count = static_cast<size_t>(r.s16());

    auto flag = countWithFlag >> 3;
    auto get = [&](int bit) { return ((flag >> (bit - 1)) & 1) != 0; };

    v.control = count > 1 && get(1);

    auto hasInitialTime = get(2);
    auto hasInitialValue = get(3) || get(4);
    auto initialValue = 0.0f;

    if (!get(3) && get(4)) initialValue = 1.0f;
    else if (get(3) && get(4)) initialValue = 2.0f;

    if (get(5)) throw std::runtime_error("invalid value1 flag");

    auto first = true;
    readList(r, v.point, [&](Reader& rr, Value1Point& p) {
        if (!first || !hasInitialTime) p.time = rr.f32();
        if (!first || !hasInitialValue) p.value = rr.f32();
        if (v.control) {
            p.control_value[0].x = rr.f32();
            p.control_value[0].y = rr.f32();
            p.control_value[1].x = rr.f32();
            p.control_value[1].y = rr.f32();
        }
        first = false;
    }, count);

    if (count > 0) {
        if (hasInitialTime) v.point[0].time = 0.0f;
        if (hasInitialValue) v.point[0].value = initialValue;
    }
}

void writeValue1(Writer& w, const Value1& v) {
    auto count = v.point.size();
    uint8_t flag = 0;

    auto hasInitialTime = false;
    auto hasInitialValue = false;
    auto initialValue = 0.0f;

    if (!v.point.empty()) {
        if (feq(v.point.front().time, 0.0f)) hasInitialTime = true;
        if (feq(v.point.front().value, 0.0f) || feq(v.point.front().value, 1.0f) || feq(v.point.front().value, 2.0f)) {
            hasInitialValue = true;
            initialValue = v.point.front().value;
        }
    }

    auto set = [&](int bit, bool val) {
        if (val) flag |= static_cast<uint8_t>(1u << (bit - 1));
    };

    set(1, count > 1 && v.control);
    set(2, hasInitialTime);
    if (hasInitialValue) {
        if (feq(initialValue, 0.0f)) { set(3, true); set(4, false); }
        else if (feq(initialValue, 1.0f)) { set(3, false); set(4, true); }
        else if (feq(initialValue, 2.0f)) { set(3, true); set(4, true); }
    }

    auto countWithFlag = static_cast<uint8_t>((flag << 3) | static_cast<uint8_t>(std::min<size_t>(count, 0b111)));
    w.u8(countWithFlag);
    if (count >= 0b111) w.s16(static_cast<int>(count));

    auto first = true;
    writeList(w, v.point, [&](Writer& ww, const Value1Point& p) {
        if (!first || !hasInitialTime) ww.f32(p.time);
        if (!first || !hasInitialValue) ww.f32(p.value);
        if (v.control) {
            ww.f32(p.control_value[0].x);
            ww.f32(p.control_value[0].y);
            ww.f32(p.control_value[1].x);
            ww.f32(p.control_value[1].y);
        }
        first = false;
    }, count);
}

void readValue2(Reader& r, Value2& v) {
    auto count = r.u16();
    v.control = count > 1 ? r.b8() : false;
    readList(r, v.point, [&](Reader& rr, Value2Point& p) {
        p.time = rr.s32();
        p.value.x = rr.f32();
        p.value.y = rr.f32();
        if (v.control) {
            p.control_value[0].x = rr.f32();
            p.control_value[0].y = rr.f32();
            p.control_value[1].x = rr.f32();
            p.control_value[1].y = rr.f32();
        }
    }, count);
}

void writeValue2(Writer& w, const Value2& v) {
    auto count = v.point.size();
    w.u16(count);
    if (count > 1) w.b8(v.control);
    writeList(w, v.point, [&](Writer& ww, const Value2Point& p) {
        ww.s32(p.time);
        ww.f32(p.value.x);
        ww.f32(p.value.y);
        if (v.control) {
            ww.f32(p.control_value[0].x);
            ww.f32(p.control_value[0].y);
            ww.f32(p.control_value[1].x);
            ww.f32(p.control_value[1].y);
        }
    }, count);
}

void readValue2Simple(Reader& r, Value2& v) {
    v.unknown_1 = r.f32();
    v.unknown_2 = r.f32();
    readList16(r, v.point, [](Reader& rr, Value2Point& p) {
        p.time = rr.s32();
        p.value.x = rr.f32();
        p.value.y = rr.f32();
    });
}

void writeValue2Simple(Writer& w, const Value2& v) {
    w.f32(v.unknown_1);
    w.f32(v.unknown_2);
    writeList16(w, v.point, [](Writer& ww, const Value2Point& p) {
        ww.s32(p.time);
        ww.f32(p.value.x);
        ww.f32(p.value.y);
    });
}

void readTexture(Reader& r, Texture& v) {
    v.name = r.str8();
    v.cell = r.s16();
    v.row = r.s16();
    v.padded = r.b8();
    v.path = r.str8();
}

void writeTexture(Writer& w, const Texture& v) {
    w.str8(v.name);
    w.s16(v.cell);
    w.s16(v.row);
    w.b8(v.padded);
    w.str8(v.path);
}

void readEmitterParticle(Reader& r, EmitterParticle& v) {
    v.unknown_1 = r.s32();
    v.unknown_2 = r.s32();
    v.unknown_3 = r.s32();
    v.unknown_4 = r.f32();
    v.unknown_5 = r.s32();
    v.unknown_6 = r.s32();
    v.unknown_7 = r.s32();
    v.unknown_8 = r.s32();
    v.unknown_9 = r.s32();
    v.unknown_10 = r.s32();
    v.unknown_11 = r.s32();
    v.unknown_12 = r.s32();
    v.unknown_13 = r.s32();
    v.unknown_14 = r.s32();
    v.unknown_15 = r.s32();
    v.unknown_16 = r.s32();
    v.instance = r.b8();
    v.single = r.b8();
    v.preserve_color = r.b8();
    v.attach_to_emitter = r.b8();
    v.attach_value = r.f32();
    v.flip_horizontal = r.b8();
    v.flip_vertical = r.b8();
    v.animation_start_on_random_frame = r.b8();
    v.repeat_color = r.s32();
    v.repeat_alpha = r.s32();
    v.link_transparency_to_color = r.b8();
    v.name = r.str8();
    v.angle_align_to_motion = r.b8();
    v.angle_random_align = r.b8();
    v.angle_keep_aligned_to_motion = r.b8();
    v.angle_value = r.s32();
    v.angle_align_offset = r.s32();
    v.animation_speed = r.s32();
    v.random_gradient_color = r.b8();
    v.unknown_17 = r.s32();
    v.texture = r.s32();

    readList16(r, v.color, [](Reader& rr, ColorPoint& p) {
        p.value.red = rr.u8();
        p.value.green = rr.u8();
        p.value.blue = rr.u8();
        p.time = rr.f32();
    });

    readList16(r, v.alpha, [](Reader& rr, AlphaPoint& p) {
        p.value = rr.u8();
        p.time = rr.f32();
    });

    readList(r, v.value, [](Reader& rr, Value1& x) { readValue1(rr, x); }, 23, 28);

    v.reference_point_offset.x = r.f32();
    v.reference_point_offset.y = r.f32();
    v.unknown_18 = r.s32();
    v.unknown_19 = r.s32();
    v.lock_aspect = r.b8();

    readValue1(r, v.value[pidx(26)]);
    readValue1(r, v.value[pidx(27)]);
    readValue1(r, v.value[pidx(28)]);

    v.angle_range = r.s32();
    v.angle_offset = r.s32();
    v.get_color_from_layer = r.b8();
    v.update_color_from_layer = r.b8();
    v.use_emitter_angle_and_range = r.b8();

    readValue1(r, v.value[pidx(24)]);
    readValue1(r, v.value[pidx(25)]);

    v.unknown_20 = r.s32();
    readValue1(r, v.unknown_21);
    v.use_key_color_only = r.b8();
    v.update_transparency_from_layer = r.b8();
    v.use_next_color_key = r.b8();
    v.number_of_each_color = r.s32();
    v.get_transparency_from_layer = r.b8();
}

void writeEmitterParticle(Writer& w, const EmitterParticle& v) {
    if (v.value.size() < 28) throw std::runtime_error("emitter particle value size too small");

    w.s32(v.unknown_1);
    w.s32(v.unknown_2);
    w.s32(v.unknown_3);
    w.f32(v.unknown_4);
    w.s32(v.unknown_5);
    w.s32(v.unknown_6);
    w.s32(v.unknown_7);
    w.s32(v.unknown_8);
    w.s32(v.unknown_9);
    w.s32(v.unknown_10);
    w.s32(v.unknown_11);
    w.s32(v.unknown_12);
    w.s32(v.unknown_13);
    w.s32(v.unknown_14);
    w.s32(v.unknown_15);
    w.s32(v.unknown_16);
    w.b8(v.instance);
    w.b8(v.single);
    w.b8(v.preserve_color);
    w.b8(v.attach_to_emitter);
    w.f32(v.attach_value);
    w.b8(v.flip_horizontal);
    w.b8(v.flip_vertical);
    w.b8(v.animation_start_on_random_frame);
    w.s32(v.repeat_color);
    w.s32(v.repeat_alpha);
    w.b8(v.link_transparency_to_color);
    w.str8(v.name);
    w.b8(v.angle_align_to_motion);
    w.b8(v.angle_random_align);
    w.b8(v.angle_keep_aligned_to_motion);
    w.s32(v.angle_value);
    w.s32(v.angle_align_offset);
    w.s32(v.animation_speed);
    w.b8(v.random_gradient_color);
    w.s32(v.unknown_17);
    w.s32(v.texture);

    writeList16(w, v.color, [](Writer& ww, const ColorPoint& p) {
        ww.u8(p.value.red);
        ww.u8(p.value.green);
        ww.u8(p.value.blue);
        ww.f32(p.time);
    });

    writeList16(w, v.alpha, [](Writer& ww, const AlphaPoint& p) {
        ww.u8(p.value);
        ww.f32(p.time);
    });

    writeList(w, v.value, [](Writer& ww, const Value1& x) { writeValue1(ww, x); }, 23);

    w.f32(v.reference_point_offset.x);
    w.f32(v.reference_point_offset.y);
    w.s32(v.unknown_18);
    w.s32(v.unknown_19);
    w.b8(v.lock_aspect);

    writeValue1(w, v.value[pidx(26)]);
    writeValue1(w, v.value[pidx(27)]);
    writeValue1(w, v.value[pidx(28)]);

    w.s32(v.angle_range);
    w.s32(v.angle_offset);
    w.b8(v.get_color_from_layer);
    w.b8(v.update_color_from_layer);
    w.b8(v.use_emitter_angle_and_range);

    writeValue1(w, v.value[pidx(24)]);
    writeValue1(w, v.value[pidx(25)]);

    w.s32(v.unknown_20);
    writeValue1(w, v.unknown_21);
    w.b8(v.use_key_color_only);
    w.b8(v.update_transparency_from_layer);
    w.b8(v.use_next_color_key);
    w.s32(v.number_of_each_color);
    w.b8(v.get_transparency_from_layer);
}

void readEmitter(Reader& r, Emitter& v) {
    v.unknown_1 = r.s32();
    v.name = r.str8();
    v.keep_in_order = r.b8();
    v.unknown_2 = r.s32();
    v.oldest_in_front = r.b8();

    readList16(r, v.particle, [](Reader& rr, EmitterParticle& p) { readEmitterParticle(rr, p); });

    v.unknown_3 = r.s32();
    readList(r, v.value, [](Reader& rr, Value1& x) { readValue1(rr, x); }, 42);
    v.unknown_4 = r.s32();
    v.unknown_5 = r.s32();
}

void writeEmitter(Writer& w, const Emitter& v) {
    if (v.value.size() < 42) throw std::runtime_error("emitter value size too small");

    w.s32(v.unknown_1);
    w.str8(v.name);
    w.b8(v.keep_in_order);
    w.s32(v.unknown_2);
    w.b8(v.oldest_in_front);

    writeList16(w, v.particle, [](Writer& ww, const EmitterParticle& p) { writeEmitterParticle(ww, p); });

    w.s32(v.unknown_3);
    writeList(w, v.value, [](Writer& ww, const Value1& x) { writeValue1(ww, x); }, 42);
    w.s32(v.unknown_4);
    w.s32(v.unknown_5);
}

void readLayerEmitter(Reader& r, LayerEmitter& v) {
    v.unknown_1 = r.f32();
    v.unknown_2 = r.f32();
    v.unknown_3 = r.f32();
    v.unknown_4 = r.f32();
    v.unknown_5 = r.f32();
    v.unknown_6 = r.f32();
    v.unknown_7 = r.f32();
    v.unknown_8 = r.f32();
    v.unknown_9 = r.f32();
    v.unknown_10 = r.f32();
    v.unknown_11 = r.f32();
    v.unknown_12 = r.f32();
    v.unknown_13 = r.s32();
    v.unknown_14 = r.s32();
    v.preload_frame = r.s32();
    v.unknown_15 = r.s32();
    v.name = r.str8();
    v.geom = r.s32();
    v.unknown_16 = r.f32();
    v.unknown_17 = r.f32();
    v.geom_4_if_2 = r.b8();
    v.emit_in = r.b8();
    v.emit_out = r.b8();
    v.tint_color.red = static_cast<int>(r.u32());
    v.tint_color.green = static_cast<int>(r.u32());
    v.tint_color.blue = static_cast<int>(r.u32());
    v.unknown_18 = r.s32();
    v.emit_at_point[0] = r.s32();
    v.type = r.s32();

    readValue2(r, v.position);
    readList16(r, v.point, [](Reader& rr, Value2& x) { readValue2Simple(rr, x); });
    readList(r, v.value, [](Reader& rr, Value1& x) { readValue1(rr, x); }, 17, 19);

    v.emit_at_point[1] = r.s32();
    v.unknown_19 = r.s32();
    readValue1(r, v.value[pidx(18)]);
    v.unknown_20 = r.s32();
    readValue1(r, v.value[pidx(19)]);

    readList16(r, v.mask_path, [](Reader& rr, std::string& s) { s = rr.str8(); });

    v.mask = r.b8();
    v.mask_name = r.str8();
    v.unknown_21 = r.s32();
    v.unknown_22 = r.s32();
    v.invert_mask = r.b8();
    v.unknown_23 = r.s32();
    v.unknown_24 = r.s32();
    v.is_super = r.b8();

    readList16(r, v.free, [](Reader& rr, int& x) { x = rr.s16(); });

    v.unknown_25 = r.s32();
    v.unknown_26 = r.f32();
    v.unknown_27 = r.f32();
}

void writeLayerEmitter(Writer& w, const LayerEmitter& v) {
    if (v.value.size() < 19) throw std::runtime_error("layer emitter value size too small");

    w.f32(v.unknown_1);
    w.f32(v.unknown_2);
    w.f32(v.unknown_3);
    w.f32(v.unknown_4);
    w.f32(v.unknown_5);
    w.f32(v.unknown_6);
    w.f32(v.unknown_7);
    w.f32(v.unknown_8);
    w.f32(v.unknown_9);
    w.f32(v.unknown_10);
    w.f32(v.unknown_11);
    w.f32(v.unknown_12);
    w.s32(v.unknown_13);
    w.s32(v.unknown_14);
    w.s32(v.preload_frame);
    w.s32(v.unknown_15);
    w.str8(v.name);
    w.s32(v.geom);
    w.f32(v.unknown_16);
    w.f32(v.unknown_17);
    w.b8(v.geom_4_if_2);
    w.b8(v.emit_in);
    w.b8(v.emit_out);
    w.u32(v.tint_color.red);
    w.u32(v.tint_color.green);
    w.u32(v.tint_color.blue);
    w.s32(v.unknown_18);
    w.s32(v.emit_at_point[0]);
    w.s32(v.type);

    writeValue2(w, v.position);
    writeList16(w, v.point, [](Writer& ww, const Value2& x) { writeValue2Simple(ww, x); });
    writeList(w, v.value, [](Writer& ww, const Value1& x) { writeValue1(ww, x); }, 17);

    w.s32(v.emit_at_point[1]);
    w.s32(v.unknown_19);
    writeValue1(w, v.value[pidx(18)]);
    w.s32(v.unknown_20);
    writeValue1(w, v.value[pidx(19)]);

    writeList16(w, v.mask_path, [](Writer& ww, const std::string& s) { ww.str8(s); });

    w.b8(v.mask);
    w.str8(v.mask_name);
    w.s32(v.unknown_21);
    w.s32(v.unknown_22);
    w.b8(v.invert_mask);
    w.s32(v.unknown_23);
    w.s32(v.unknown_24);
    w.b8(v.is_super);

    writeList16(w, v.free, [](Writer& ww, int x) { ww.s16(x); });

    w.s32(v.unknown_25);
    w.f32(v.unknown_26);
    w.f32(v.unknown_27);
}

void readLayer(Reader& r, Layer& v) {
    v.name = r.str8();

    readList16(r, v.emitter, [](Reader& rr, LayerEmitter& x) { readLayerEmitter(rr, x); });

    readList16(r, v.deflector, [](Reader& rr, LayerDeflector& x) {
        x.name = rr.str8();
        x.bounce = rr.s32();
        x.hit = rr.s32();
        x.thickness = rr.s32();
        x.visible = rr.b8();
        readValue2(rr, x.position);
        readList16(rr, x.point, [](Reader& rrr, Value2& v2) { readValue2Simple(rrr, v2); });
        readValue1(rr, x.active);
        readValue1(rr, x.angle);
    });

    readList16(r, v.blocker, [](Reader& rr, LayerBlocker& x) {
        x.name = rr.str8();
        x.unknown_1 = rr.s32();
        x.unknown_2 = rr.s32();
        x.unknown_3 = rr.s32();
        x.unknown_4 = rr.s32();
        x.unknown_5 = rr.s32();
        readValue2(rr, x.position);
        readList16(rr, x.point, [](Reader& rrr, Value2& v2) { readValue2Simple(rrr, v2); });
        readValue1(rr, x.active);
        readValue1(rr, x.angle);
    });

    readValue2(r, v.offset);
    readValue1(r, v.angle);
    v.unknown_1 = r.str8();

    readList(r, v.unknown_3, [](Reader& rr, int& x) { x = rr.u8(); }, 32);
    readList16(r, v.unknown_2, [](Reader& rr, std::string& s) { s = rr.str8(); });
    readList(r, v.unknown_4, [](Reader& rr, int& x) { x = rr.u8(); }, 36);

    readList16(r, v.force, [](Reader& rr, LayerForce& x) {
        x.name = rr.str8();
        x.visible = rr.b8();
        readValue2(rr, x.position);
        readValue1(rr, x.active);
        readValue1(rr, x.unknown_1);
        readValue1(rr, x.strength);
        readValue1(rr, x.width);
        readValue1(rr, x.height);
        readValue1(rr, x.angle);
        readValue1(rr, x.direction);
    });

    readList(r, v.unknown_5, [](Reader& rr, int& x) { x = rr.u8(); }, 28);
}

void writeLayer(Writer& w, const Layer& v) {
    if (v.unknown_3.size() < 32) throw std::runtime_error("layer unknown_3 size too small");
    if (v.unknown_4.size() < 36) throw std::runtime_error("layer unknown_4 size too small");
    if (v.unknown_5.size() < 28) throw std::runtime_error("layer unknown_5 size too small");

    w.str8(v.name);

    writeList16(w, v.emitter, [](Writer& ww, const LayerEmitter& x) { writeLayerEmitter(ww, x); });

    writeList16(w, v.deflector, [](Writer& ww, const LayerDeflector& x) {
        ww.str8(x.name);
        ww.s32(x.bounce);
        ww.s32(x.hit);
        ww.s32(x.thickness);
        ww.b8(x.visible);
        writeValue2(ww, x.position);
        writeList16(ww, x.point, [](Writer& www, const Value2& v2) { writeValue2Simple(www, v2); });
        writeValue1(ww, x.active);
        writeValue1(ww, x.angle);
    });

    writeList16(w, v.blocker, [](Writer& ww, const LayerBlocker& x) {
        ww.str8(x.name);
        ww.s32(x.unknown_1);
        ww.s32(x.unknown_2);
        ww.s32(x.unknown_3);
        ww.s32(x.unknown_4);
        ww.s32(x.unknown_5);
        writeValue2(ww, x.position);
        writeList16(ww, x.point, [](Writer& www, const Value2& v2) { writeValue2Simple(www, v2); });
        writeValue1(ww, x.active);
        writeValue1(ww, x.angle);
    });

    writeValue2(w, v.offset);
    writeValue1(w, v.angle);
    w.str8(v.unknown_1);

    writeList(w, v.unknown_3, [](Writer& ww, int x) { ww.u8(x); }, 32);
    writeList16(w, v.unknown_2, [](Writer& ww, const std::string& s) { ww.str8(s); });
    writeList(w, v.unknown_4, [](Writer& ww, int x) { ww.u8(x); }, 36);

    writeList16(w, v.force, [](Writer& ww, const LayerForce& x) {
        ww.str8(x.name);
        ww.b8(x.visible);
        writeValue2(ww, x.position);
        writeValue1(ww, x.active);
        writeValue1(ww, x.unknown_1);
        writeValue1(ww, x.strength);
        writeValue1(ww, x.width);
        writeValue1(ww, x.height);
        writeValue1(ww, x.angle);
        writeValue1(ww, x.direction);
    });

    writeList(w, v.unknown_5, [](Writer& ww, int x) { ww.u8(x); }, 28);
}

void readEffect(Reader& r, Effect& v) {
    v.note = r.str8();

    readList16(r, v.texture, [](Reader& rr, Texture& x) { readTexture(rr, x); });
    readList16(r, v.emitter, [](Reader& rr, Emitter& x) { readEmitter(rr, x); });
    readList16(r, v.layer, [](Reader& rr, Layer& x) { readLayer(rr, x); });

    v.background_color.red = static_cast<int>(r.u32());
    v.background_color.green = static_cast<int>(r.u32());
    v.background_color.blue = static_cast<int>(r.u32());
    v.unknown_1 = r.s32();
    v.unknown_2 = r.s32();
    v.frame_rate = r.s16();
    v.unknown_3 = r.s16();
    v.unknown_4 = r.s16();
    v.unknown_5 = r.s16();
    v.size.width = r.s32();
    v.size.height = r.s32();
    v.unknown_6 = r.s32();
    v.unknown_7 = r.s32();
    v.unknown_8 = r.s32();
    v.unknown_9 = r.s32();
    v.unknown_10 = r.s32();
    v.frame_range[0] = r.s32();
    v.frame_range[1] = r.s32();
    v.unknown_11 = r.str8();
    v.unknown_12 = r.u8();
    v.unknown_13 = r.s16();
    v.unknown_14 = r.s16();
    v.startup_state = r.bytes32();
}

void writeEffect(Writer& w, const Effect& v) {
    w.str8(v.note);

    writeList16(w, v.texture, [](Writer& ww, const Texture& x) { writeTexture(ww, x); });
    writeList16(w, v.emitter, [](Writer& ww, const Emitter& x) { writeEmitter(ww, x); });
    writeList16(w, v.layer, [](Writer& ww, const Layer& x) { writeLayer(ww, x); });

    w.u32(v.background_color.red);
    w.u32(v.background_color.green);
    w.u32(v.background_color.blue);
    w.s32(v.unknown_1);
    w.s32(v.unknown_2);
    w.s16(v.frame_rate);
    w.s16(v.unknown_3);
    w.s16(v.unknown_4);
    w.s16(v.unknown_5);
    w.s32(v.size.width);
    w.s32(v.size.height);
    w.s32(v.unknown_6);
    w.s32(v.unknown_7);
    w.s32(v.unknown_8);
    w.s32(v.unknown_9);
    w.s32(v.unknown_10);
    w.s32(v.frame_range[0]);
    w.s32(v.frame_range[1]);
    w.str8(v.unknown_11);
    w.u8(v.unknown_12);
    w.s16(v.unknown_13);
    w.s16(v.unknown_14);
    w.bytes32(v.startup_state);
}

Effect decodeEffect(std::span<const uint8_t> data) {
    Reader r(data);
    r.magic();
    Effect e;
    readEffect(r, e);
    if (r.error()) throw std::runtime_error("ppf decode stream error");
    return e;
}

std::vector<uint8_t> encodeEffect(const Effect& e) {
    Writer w;
    w.magic();
    writeEffect(w, e);
    if (w.error()) throw std::runtime_error("ppf encode stream error");
    return w.take();
}

}
