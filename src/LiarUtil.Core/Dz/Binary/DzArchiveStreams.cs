using LiarUtil.Core.Dz.Definitions;
using LiarUtil.Core.Dz.Models;

namespace LiarUtil.Core.Dz.Binary;

internal sealed class DzArchiveStreams : IDisposable
{
    private readonly Stream[] _streams;
    private readonly List<Stream> _owned = [];

    public DzArchiveStreams(Stream mainStream, string inputPath, DzArchive archive)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? ".";
        _streams = new Stream[archive.ArchiveCount < 1 ? 1 : archive.ArchiveCount];
        for (var i = 0; i < _streams.Length; i++)
        {
            var name = i < archive.ArchiveNames.Length ? archive.ArchiveNames[i] : null;
            if (i == 0 || string.IsNullOrEmpty(name))
            {
                _streams[i] = mainStream;
                continue;
            }

            var stream = File.OpenRead(DzPath.ToLocal(root, name!));
            _owned.Add(stream);
            _streams[i] = stream;
        }
    }

    public Stream this[ushort archiveIndex] => archiveIndex < _streams.Length && _streams[archiveIndex] is not null
        ? _streams[archiveIndex]
        : throw new DzException(string.Format(LiarUtil.Core.Strings.ArchiveIndexOutOfRange0, archiveIndex));

    public void Dispose()
    {
        foreach (var stream in _owned)
        {
            stream.Dispose();
        }
    }
}
