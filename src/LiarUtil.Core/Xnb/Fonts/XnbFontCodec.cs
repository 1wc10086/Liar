using System.Text.Json;

namespace LiarUtil.Core.Xnb.Fonts;

public static class XnbFontCodec
{
    public static SpriteFontData Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var content = XnbContainer.Read(data);
        if (!content.PrimaryReader.IsSpriteFont)
        {
            throw new XnbException(string.Format(LiarUtil.Core.Strings.XNBPrimaryObjectNotFont0, content.PrimaryReader.Name));
        }

        return SpriteFontReader.Read(content.Data, content.Offset, content.Length);
    }

    public static byte[] Encode(SpriteFontData font)
    {
        ArgumentNullException.ThrowIfNull(font);
        return SpriteFontWriter.Write(font);
    }

    public static string DecodeToJson(byte[] data) =>
        JsonSerializer.Serialize(Decode(data), SpriteFontJsonContext.Default.SpriteFontData);

    public static byte[] EncodeFromJson(string json) => Encode(
        JsonSerializer.Deserialize(json, SpriteFontJsonContext.Default.SpriteFontData)
        ?? throw new XnbException(LiarUtil.Core.Strings.XNBFontJSONContentEmpty));
}
