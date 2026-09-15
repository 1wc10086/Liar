namespace LiarUtil.Core.Pak.Models;

internal sealed record PakPayload(byte[] Data, bool PcEncrypted, bool TvVersion, bool XmemCompressed);
