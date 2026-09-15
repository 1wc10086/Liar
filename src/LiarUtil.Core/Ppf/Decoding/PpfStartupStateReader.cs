using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Decoding;

internal sealed class PpfStartupStateReader
{
    private readonly PpfEffect _effect;
    private readonly PpfEmitterLayout _layout;
    private BufferReader _reader = new(Array.Empty<byte>());

    public PpfStartupStateReader(PpfEffect effect)
    {
        _effect = effect;
        _layout = new PpfEmitterLayout(effect);
    }

    public PpfStartupState Read(byte[] payload)
    {
        _reader = new BufferReader(payload);
        var totalSize = _reader.ReadInt32();
        var state = new PpfStartupState
        {
            Version = _reader.ReadUInt16(),
            Frame = _reader.ReadSingle(),
        };
        if (state.Version == 0)
        {
            state.EmitAfterTimeline = _reader.ReadBoolean();
            state.EmitterTransform = ReadTransform();
            state.DrawTransform = ReadTransform();
        }
        if (state.Frame > 0f)
        {
            foreach (var layer in _effect.Layers)
            {
                var layerState = new PpfStartupLayerState();
                var hasDeflector = layer.Deflectors.Count > 0;
                foreach (var layerEmitter in layer.Emitters)
                {
                    layerState.Emitters.Add(ReadEmitterState(layerEmitter, hasDeflector));
                }
                state.Layers.Add(layerState);
            }
        }
        if (_reader.Remaining != 0 || totalSize != payload.Length - 4)
        {
            throw new InvalidDataException(LiarUtil.Core.Strings.PPFStartupStateLengthsAreInconsistent);
        }
        return state;
    }

    private PpfStartupEmitterState ReadEmitterState(PpfLayerEmitter layerEmitter, bool hasDeflector)
    {
        var state = new PpfStartupEmitterState { HasTransform = _reader.ReadBoolean() };
        if (state.HasTransform)
        {
            state.Transform = ReadTransform();
        }
        state.WasActive = _reader.ReadBoolean();
        state.WithinLifeFrame = _reader.ReadBoolean();
        var emitter = _layout.Resolve(layerEmitter.Type);
        var definitions = PpfEmitterLayout.GetDefinitions(emitter);
        for (var index = 0; index < definitions.Length; index++)
        {
            state.ParticleDefStates.Add(ReadParticleDefState());
        }
        for (var index = 0; index < layerEmitter.Free.Count; index++)
        {
            state.FreeParticleDefStates.Add(ReadParticleDefState());
        }
        var freeCount = ReadCount();
        for (var index = 0; index < freeCount; index++)
        {
            state.FreeEmitters.Add(ReadFreeEmitterState(layerEmitter, hasDeflector));
        }
        var particleCount = ReadCount();
        for (var index = 0; index < particleCount; index++)
        {
            state.Particles.Add(ReadParticle(definitions, _reader.ReadInt16(), hasDeflector));
        }
        return state;
    }

    private PpfStartupFreeEmitterState ReadFreeEmitterState(PpfLayerEmitter layerEmitter, bool hasDeflector)
    {
        var freeIndex = _reader.ReadInt16();
        if ((uint)freeIndex >= (uint)layerEmitter.Free.Count)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFFreeEmitterIndexInvalid0, freeIndex));
        }
        var freeEmitter = _layout.Resolve(layerEmitter.Free[freeIndex]);
        var definitions = PpfEmitterLayout.GetDefinitions(freeEmitter);
        var state = new PpfStartupFreeEmitterState
        {
            FreeListIndex = freeIndex,
            Particle = ReadParticle(null, 0, hasDeflector),
        };
        for (var index = 0; index < definitions.Length; index++)
        {
            state.ParticleDefStates.Add(ReadParticleDefState());
        }
        var particleCount = ReadCount();
        for (var index = 0; index < particleCount; index++)
        {
            state.Particles.Add(ReadParticle(definitions, _reader.ReadInt16(), hasDeflector));
        }
        return state;
    }

    private PpfStartupParticleState ReadParticle(PpfEmitterParticle[]? definitions, short definitionIndex, bool hasDeflector)
    {
        var definition = definitions is not null && (uint)definitionIndex < (uint)definitions.Length
            ? definitions[definitionIndex]
            : null;
        var state = new PpfStartupParticleState
        {
            ParticleDefIndex = definitionIndex,
            Ticks = _reader.ReadSingle(),
            Life = _reader.ReadSingle(),
            LifePercent = _reader.ReadSingle(),
            Zoom = _reader.ReadSingle(),
            Position = ReadDoublePoint(),
            Velocity = ReadDoublePoint(),
            EmittedPosition = ReadDoublePoint(),
        };
        if (definition is { AttachToEmitter: true })
        {
            state.OriginalPosition = ReadDoublePoint();
            state.OriginalEmitterAngle = _reader.ReadSingle();
        }
        state.ImageAngle = _reader.ReadSingle();
        state.VariationFlags = _reader.ReadInt16();
        var variations = new float[9];
        for (var index = 0; index < variations.Length; index++)
        {
            if ((state.VariationFlags & (1 << index)) != 0)
            {
                variations[index] = _reader.ReadSingle();
            }
        }
        state.Variations = [.. variations];
        state.SourceSizeXMultiplier = _reader.ReadSingle();
        state.SourceSizeYMultiplier = _reader.ReadSingle();
        if (definition is { RandomGradientColor: true })
        {
            state.GradientRandom = _reader.ReadSingle();
        }
        if (definition is { AnimationStartOnRandomFrame: true })
        {
            state.AnimationFrameRandom = _reader.ReadInt16();
        }
        if (hasDeflector)
        {
            state.ThicknessHitVariation = _reader.ReadSingle();
        }
        return state;
    }

    private PpfStartupParticleDefState ReadParticleDefState() => new()
    {
        NumberAccumulator = _reader.ReadSingle(),
        CurrentNumberVariation = _reader.ReadSingle(),
        ParticlesEmitted = _reader.ReadInt32(),
        Ticks = _reader.ReadInt32(),
    };

    private PpfTransform ReadTransform() => new()
    {
        M00 = _reader.ReadSingle(),
        M01 = _reader.ReadSingle(),
        M02 = _reader.ReadSingle(),
        M10 = _reader.ReadSingle(),
        M11 = _reader.ReadSingle(),
        M12 = _reader.ReadSingle(),
        M20 = _reader.ReadSingle(),
        M21 = _reader.ReadSingle(),
        M22 = _reader.ReadSingle(),
    };

    private PpfDoublePoint ReadDoublePoint() => new()
    {
        X = _reader.ReadDouble(),
        Y = _reader.ReadDouble(),
    };

    private int ReadCount()
    {
        var count = _reader.ReadUInt32();
        if (count > (uint)_reader.Remaining)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFStartupStateCountInvalid0, count));
        }
        return (int)count;
    }
}
