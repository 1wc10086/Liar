using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf;

internal sealed class PpfEmitterLayout
{
    private readonly PpfEmitter[] _emitters;
    private readonly int[] _indexMap;

    public PpfEmitterLayout(PpfEffect effect)
    {
        var used = new bool[effect.Emitters.Count];
        foreach (var layer in effect.Layers)
        {
            foreach (var emitter in layer.Emitters)
            {
                MarkUsed(used, emitter.Type, LiarUtil.Core.Strings.LayerEmitter);
                foreach (var index in emitter.Free)
                {
                    MarkUsed(used, index, LiarUtil.Core.Strings.FreeEmitter);
                }
            }
        }
        var emitters = new List<PpfEmitter>();
        _indexMap = new int[used.Length];
        for (var index = 0; index < used.Length; index++)
        {
            if (!used[index])
            {
                _indexMap[index] = -1;
                continue;
            }
            _indexMap[index] = emitters.Count;
            emitters.Add(effect.Emitters[index]);
        }
        _emitters = [.. emitters];
    }

    public PpfEmitter Resolve(int emitterIndex)
    {
        if ((uint)emitterIndex >= (uint)_indexMap.Length || _indexMap[emitterIndex] < 0)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFEmitterIndexInvalid0, emitterIndex));
        }
        return _emitters[_indexMap[emitterIndex]];
    }

    public static PpfEmitterParticle[] GetDefinitions(PpfEmitter emitter)
    {
        var definitions = emitter.Particles.ToArray();
        if (emitter.OldestInFront)
        {
            Array.Reverse(definitions);
        }
        return definitions;
    }

    private static void MarkUsed(bool[] used, int index, string source)
    {
        if ((uint)index >= (uint)used.Length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPF0IndexOutOfRange1, source, index));
        }
        used[index] = true;
    }
}
