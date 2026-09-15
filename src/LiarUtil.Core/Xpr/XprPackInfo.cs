namespace LiarUtil.Core.Xpr;

public sealed class XprDefinitionFile
{
    public string? Source { get; set; } = "LibWindPop.Packs.Xpr.XprPackInfo";

    public string? Author { get; set; } = "YingFengTingYu";

    public int Version { get; set; }

    public XprPackInfo Content { get; set; } = new();
}

public sealed class XprPackInfo
{
    public uint XprDataOffset { get; set; }

    public bool XprDataFileAlign { get; set; }

    public List<XprRecordFile> RecordFiles { get; set; } = [];
}

public sealed class XprRecordFile
{
    public string? Type { get; set; }

    public string Path { get; set; } = "";
}
