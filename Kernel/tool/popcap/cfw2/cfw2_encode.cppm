module;
#include <cstdint>
#include <string>
#include <string_view>
#include <vector>
export module tool.popcap.cfw2.encode;
import utility.binary.unified_binary_stream;
import utility.json;
import tool.popcap.cfw2.core;
import tool.popcap.cfw2.definition;

export namespace CFW2 {

[[nodiscard]] inline std::vector<uint8_t> encode(std::string_view source) {
    try {
        auto document = json::Document::parse(source);
        if (!document) return {};
        const auto root = document.root();
        if (!root || !root.is_obj() || !root.obj_get(Field::Ascent) || !root.obj_get(Field::Height)) return {};

        UnifiedBinaryStream stream(UnifiedBinaryStream::Mode::Write, UnifiedBinaryStream::Endian::Little);
        if (const auto header = root.obj_get(Field::Unknown); header && header.is_arr() && header.arr_size() == HeaderSize) {
            std::vector<uint8_t> bytes;
            bytes.reserve(HeaderSize);
            for (const auto value : header.array()) bytes.push_back(static_cast<uint8_t>(value.get_sint()));
            stream.writeBytes(bytes);
        } else {
            stream.reserveHeader(HeaderSize);
        }

        const auto integer = [](json::Value object, std::string_view key) {
            return static_cast<int32_t>(object.obj_get(key).get_sint());
        };
        const auto writeStrings = [&](json::Value values) {
            stream.writeUInt32(static_cast<uint32_t>(values.arr_size()));
            for (const auto value : values.array()) stream.writeString(value.get_str_view());
        };

        for (const auto key : {Field::Ascent, Field::AscentPadding, Field::Height, Field::LineSepacingOffset})
            stream.writeInt32(integer(root, key));
        stream.writeBool(root.obj_get(Field::Initialized).get_bool());
        stream.writeInt32(integer(root, Field::DefaultPointSize));

        const auto characters = root.obj_get(Field::Character);
        stream.writeUInt32(static_cast<uint32_t>(characters.arr_size()));
        for (const auto character : characters.array()) {
            stream.writeChar(static_cast<uint16_t>(integer(character, Field::Index)));
            stream.writeChar(static_cast<uint16_t>(integer(character, Field::Value)));
        }

        const auto layers = root.obj_get(Field::Layer);
        stream.writeUInt32(static_cast<uint32_t>(layers.arr_size()));
        for (const auto layer : layers.array()) {
            stream.writeString(layer.obj_get(Field::Name).get_str_view());
            writeStrings(layer.obj_get(Field::TagRequire));
            writeStrings(layer.obj_get(Field::TagExclude));

            const auto kernings = layer.obj_get(Field::Kerning);
            stream.writeUInt32(static_cast<uint32_t>(kernings.arr_size()));
            for (const auto kerning : kernings.array()) {
                stream.writeUInt16(static_cast<uint16_t>(integer(kerning, Field::Offset)));
                stream.writeChar(static_cast<uint16_t>(integer(kerning, Field::Index)));
            }

            const auto layerCharacters = layer.obj_get(Field::Character);
            stream.writeUInt32(static_cast<uint32_t>(layerCharacters.arr_size()));
            for (const auto character : layerCharacters.array()) {
                stream.writeChar(static_cast<uint16_t>(integer(character, Field::Index)));
                for (const auto key : {Field::ImageRectX, Field::ImageRectY, Field::ImageRectWidth, Field::ImageRectHeight, Field::ImageOffsetX, Field::ImageOffsetY})
                    stream.writeInt32(integer(character, key));
                stream.writeUInt16(static_cast<uint16_t>(integer(character, Field::KerningFirst)));
                stream.writeUInt16(static_cast<uint16_t>(integer(character, Field::KerningCount)));
                stream.writeInt32(integer(character, Field::Width));
                stream.writeInt32(integer(character, Field::Order));
            }

            for (const auto key : {Field::MultiplyRed, Field::MultiplyGreen, Field::MultiplyBlue, Field::MultiplyAlpha, Field::AddRed, Field::AddGreen, Field::AddBlue, Field::AddAlpha})
                stream.writeInt32(integer(layer, key));
            stream.writeString(layer.obj_get(Field::ImageFile).get_str_view());
            for (const auto key : {Field::DrawMode, Field::OffsetX, Field::OffsetY, Field::Spacing, Field::MinimumPointSize, Field::MaximumPointSize, Field::PointSize, Field::Ascent, Field::AscentPadding, Field::Height, Field::DefaultHeight, Field::LineSpacingOffset, Field::BaseOrder})
                stream.writeInt32(integer(layer, key));
        }

        stream.writeString(root.obj_get(Field::SourceFile).get_str_view());
        stream.writeString(root.obj_get(Field::ErrorHeader).get_str_view());
        stream.writeInt32(integer(root, Field::PointSize));
        writeStrings(root.obj_get(Field::Tag));
        stream.writeDouble(root.obj_get(Field::Scale).get_real());
        stream.writeBool(root.obj_get(Field::ForceScaledImageWhite).get_bool());
        stream.writeBool(root.obj_get(Field::ActivateAllLayer).get_bool());
        return stream.hasErrorOccurred() ? std::vector<uint8_t>{} : stream.toByteArray();
    } catch (...) {
        return {};
    }
}

}
