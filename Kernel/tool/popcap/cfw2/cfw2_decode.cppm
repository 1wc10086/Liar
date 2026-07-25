module;
#include <cstdint>
#include <span>
#include <string>
#include <string_view>
#include <utility>
export module tool.popcap.cfw2.decode;
import utility.binary.unified_binary_stream;
import utility.json;
import tool.popcap.cfw2.core;
import tool.popcap.cfw2.definition;

export namespace CFW2 {

[[nodiscard]] inline std::string decode(std::span<const uint8_t> data, bool preserveHeader = false) {
    if (data.size() < HeaderSize) return {};

    try {
        UnifiedBinaryStream stream(data, UnifiedBinaryStream::Endian::Little);
        stream.backupHeader(HeaderSize);
        stream.setPosition(HeaderSize);

        json::MutDocument document;
        if (!document || stream.hasErrorOccurred()) return {};
        auto root = document.mut_obj();
        document.set_root(root);
        const auto addInt = [&](json::MutValue object, std::string_view key, int64_t value) {
            document.obj_add_int(object, key, value);
        };
        const auto addString = [&](json::MutValue object, std::string_view key, std::string value) {
            object.obj_add(document.mut_str(key), document.mut_strdup(value));
        };

        if (preserveHeader) {
            const auto header = stream.getHeaderBackup();
            auto bytes = document.mut_arr();
            for (const auto byte : header) bytes.arr_append(document.mut_uint(byte));
            root.obj_add(document.mut_str(Field::Unknown), bytes);
        }

        addInt(root, Field::Ascent, stream.readInt32());
        addInt(root, Field::AscentPadding, stream.readInt32());
        addInt(root, Field::Height, stream.readInt32());
        addInt(root, Field::LineSepacingOffset, stream.readInt32());
        document.obj_add_bool(root, Field::Initialized, stream.readBool());
        addInt(root, Field::DefaultPointSize, stream.readInt32());

        auto characters = document.mut_arr();
        for (auto count = stream.readUInt32(); count-- && !stream.hasErrorOccurred();) {
            auto character = document.mut_obj();
            addInt(character, Field::Index, stream.readChar());
            addInt(character, Field::Value, stream.readChar());
            characters.arr_append(character);
        }
        root.obj_add(document.mut_str(Field::Character), characters);

        auto layers = document.mut_arr();
        for (auto count = stream.readUInt32(); count-- && !stream.hasErrorOccurred();) {
            auto layer = document.mut_obj();
            addString(layer, Field::Name, stream.readString());

            const auto readStrings = [&] {
                auto values = document.mut_arr();
                for (auto n = stream.readUInt32(); n-- && !stream.hasErrorOccurred();)
                    values.arr_append(document.mut_strdup(stream.readString()));
                return values;
            };
            layer.obj_add(document.mut_str(Field::TagRequire), readStrings());
            layer.obj_add(document.mut_str(Field::TagExclude), readStrings());

            auto kernings = document.mut_arr();
            for (auto n = stream.readUInt32(); n-- && !stream.hasErrorOccurred();) {
                auto kerning = document.mut_obj();
                addInt(kerning, Field::Offset, stream.readUInt16());
                addInt(kerning, Field::Index, stream.readChar());
                kernings.arr_append(kerning);
            }
            layer.obj_add(document.mut_str(Field::Kerning), kernings);

            auto layerCharacters = document.mut_arr();
            for (auto n = stream.readUInt32(); n-- && !stream.hasErrorOccurred();) {
                auto character = document.mut_obj();
                addInt(character, Field::Index, stream.readChar());
                for (const auto key : {Field::ImageRectX, Field::ImageRectY, Field::ImageRectWidth, Field::ImageRectHeight, Field::ImageOffsetX, Field::ImageOffsetY})
                    addInt(character, key, stream.readInt32());
                addInt(character, Field::KerningFirst, stream.readUInt16());
                addInt(character, Field::KerningCount, stream.readUInt16());
                addInt(character, Field::Width, stream.readInt32());
                addInt(character, Field::Order, stream.readInt32());
                layerCharacters.arr_append(character);
            }
            layer.obj_add(document.mut_str(Field::Character), layerCharacters);

            for (const auto key : {Field::MultiplyRed, Field::MultiplyGreen, Field::MultiplyBlue, Field::MultiplyAlpha, Field::AddRed, Field::AddGreen, Field::AddBlue, Field::AddAlpha})
                addInt(layer, key, stream.readInt32());
            addString(layer, Field::ImageFile, stream.readString());
            for (const auto key : {Field::DrawMode, Field::OffsetX, Field::OffsetY, Field::Spacing, Field::MinimumPointSize, Field::MaximumPointSize, Field::PointSize, Field::Ascent, Field::AscentPadding, Field::Height, Field::DefaultHeight, Field::LineSpacingOffset, Field::BaseOrder})
                addInt(layer, key, stream.readInt32());
            layers.arr_append(layer);
        }
        root.obj_add(document.mut_str(Field::Layer), layers);

        addString(root, Field::SourceFile, stream.readString());
        addString(root, Field::ErrorHeader, stream.readString());
        addInt(root, Field::PointSize, stream.readInt32());

        auto tags = document.mut_arr();
        for (auto count = stream.readUInt32(); count-- && !stream.hasErrorOccurred();)
            tags.arr_append(document.mut_strdup(stream.readString()));
        root.obj_add(document.mut_str(Field::Tag), tags);
        document.obj_add_real(root, Field::Scale, stream.readDouble());
        document.obj_add_bool(root, Field::ForceScaledImageWhite, stream.readBool());
        document.obj_add_bool(root, Field::ActivateAllLayer, stream.readBool());
        return stream.hasErrorOccurred() ? std::string{} : document.write(json::WriteFlag::Pretty);
    } catch (...) {
        return {};
    }
}

}
