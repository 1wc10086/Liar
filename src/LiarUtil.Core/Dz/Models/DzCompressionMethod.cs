using System.Text.Json.Serialization;
using LiarUtil.Core.Dz.Json;

namespace LiarUtil.Core.Dz.Models;

[Flags]
[JsonConverter(typeof(DzCompressionMethodJsonConverter))]
public enum DzCompressionMethod : ushort
{
    None = 0,
    CombineBuffer = 1,
    Dz = 4,
    Zlib = 8,
    Bzip2 = 16,
    Mp3 = 32,
    Jpeg = 64,
    Zero = 128,
    Store = 256,
    Lzma = 512,
    RandomAccess = 1024,
}
