using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Bnk.Codec;
using LiarUtil.Core.Bnk.Model;

namespace LiarUtil.Core.Bnk;

public static class BnkCodec
{
    private const string DefinitionFileName = "definition.json";
    private const string MediaFolderName = "embedded_media";

    public static void Unpack(string inputPath, string outputFolder, BankVersion version)
    {
        var media = new List<EmbeddedMediaItem>();
        var bank = SoundBankCodec.Read(File.ReadAllBytes(inputPath), version, media);
        Directory.CreateDirectory(outputFolder);
        if (media.Count > 0)
        {
            var mediaFolder = Path.Combine(outputFolder, MediaFolderName);
            Directory.CreateDirectory(mediaFolder);
            foreach (var item in media)
            {
                File.WriteAllBytes(Path.Combine(mediaFolder, $"{item.Id}.wem"), item.Data);
            }
        }
        File.WriteAllText(Path.Combine(outputFolder, DefinitionFileName), JsonSerializer.Serialize(bank, Json.Options));
    }

    public static void Pack(string inputFolder, string outputPath, BankVersion version)
    {
        var definitionPath = Path.Combine(inputFolder, DefinitionFileName);
        var bank = JsonSerializer.Deserialize<SoundBank>(File.ReadAllText(definitionPath), Json.Options);
        if (bank is null)
        {
            throw new BnkException(string.Format(LiarUtil.Core.Strings.CannotRead0, definitionPath));
        }
        var media = new List<EmbeddedMediaItem>();
        var mediaFolder = Path.Combine(inputFolder, MediaFolderName);
        foreach (var id in bank.EmbeddedMedia)
        {
            var file = Path.Combine(mediaFolder, $"{id}.wem");
            if (File.Exists(file))
            {
                media.Add(new EmbeddedMediaItem(id, File.ReadAllBytes(file)));
            }
        }
        var data = SoundBankCodec.Write(bank, version, media);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllBytes(outputPath, data);
    }
}

internal static class Json
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
        options.Converters.Add(new HierarchyJsonConverter());
        options.Converters.Add(new EventActionPropertyJsonConverter());
        options.Converters.Add(new AudioAssociationSettingJsonConverter());
        return options;
    }
}
