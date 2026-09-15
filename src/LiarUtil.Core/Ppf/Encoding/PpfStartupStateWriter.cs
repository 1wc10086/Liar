using LiarUtil.Core.Core.Binary;
using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Encoding;

internal sealed class PpfStartupStateWriter
{
    private readonly PpfEffect _effect;
    private readonly PpfEmitterLayout _layout;
    private readonly BufferWriter _writer = new();

    public PpfStartupStateWriter(PpfEffect effect)
    {
        _effect = effect;
        _layout = new PpfEmitterLayout(effect);
    }

    public byte[] Write(PpfStartupState state)
    {
        _writer.WriteUInt32(0);
        _writer.WriteUInt16(state.Version);
        _writer.WriteSingle(state.Frame);
        if (state.Version == 0)
        {
            _writer.WriteBoolean(state.EmitAfterTimeline);
            WriteTransform(state.EmitterTransform);
            WriteTransform(state.DrawTransform);
        }
        if (state.Frame > 0f)
        {
            RequireCount(state.Layers.Count, _effect.Layers.Count, LiarUtil.Core.Strings.LayerState);
            for (var layerIndex = 0; layerIndex < _effect.Layers.Count; layerIndex++)
            {
                var layer = _effect.Layers[layerIndex];
                var layerState = state.Layers[layerIndex];
                RequireCount(layerState.Emitters.Count, layer.Emitters.Count, LiarUtil.Core.Strings.LayerEmitterState);
                var hasDeflector = layer.Deflectors.Count > 0;
                for (var emitterIndex = 0; emitterIndex < layer.Emitters.Count; emitterIndex++)
                {
                    WriteEmitterState(layer.Emitters[emitterIndex], layerState.Emitters[emitterIndex], hasDeflector);
                }
            }
        }
        _writer.WriteUInt32At(0, (uint)(_writer.Length - sizeof(uint)));
        return _writer.ToArray();
    }

    private void WriteEmitterState(PpfLayerEmitter layerEmitter, PpfStartupEmitterState state, bool hasDeflector)
    {
        _writer.WriteBoolean(state.HasTransform);
        if (state.HasTransform)
        {
            WriteTransform(state.Transform);
        }
        _writer.WriteBoolean(state.WasActive);
        _writer.WriteBoolean(state.WithinLifeFrame);
        var emitter = _layout.Resolve(layerEmitter.Type);
        var definitions = PpfEmitterLayout.GetDefinitions(emitter);
        RequireCount(state.ParticleDefStates.Count, definitions.Length, LiarUtil.Core.Strings.ParticleDefinitionState);
        foreach (var item in state.ParticleDefStates)
        {
            WriteParticleDefState(item);
        }
        RequireCount(state.FreeParticleDefStates.Count, layerEmitter.Free.Count, LiarUtil.Core.Strings.FreeParticleDefinitionState);
        foreach (var item in state.FreeParticleDefStates)
        {
            WriteParticleDefState(item);
        }
        WriteCount(state.FreeEmitters.Count);
        foreach (var freeState in state.FreeEmitters)
        {
            if ((uint)freeState.FreeListIndex >= (uint)layerEmitter.Free.Count)
            {
                throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFFreeEmitterIndexInvalid0, freeState.FreeListIndex));
            }
            var freeEmitter = _layout.Resolve(layerEmitter.Free[freeState.FreeListIndex]);
            var freeDefinitions = PpfEmitterLayout.GetDefinitions(freeEmitter);
            _writer.WriteInt16(freeState.FreeListIndex);
            WriteParticleState(freeState.Particle, null, hasDeflector);
            RequireCount(freeState.ParticleDefStates.Count, freeDefinitions.Length, LiarUtil.Core.Strings.FreeEmitterParticleDefinitionState);
            foreach (var item in freeState.ParticleDefStates)
            {
                WriteParticleDefState(item);
            }
            WriteCount(freeState.Particles.Count);
            foreach (var particle in freeState.Particles)
            {
                var definition = ResolveDefinition(freeDefinitions, particle.ParticleDefIndex);
                _writer.WriteInt16(particle.ParticleDefIndex);
                WriteParticleState(particle, definition, hasDeflector);
            }
        }
        WriteCount(state.Particles.Count);
        foreach (var particle in state.Particles)
        {
            var definition = ResolveDefinition(definitions, particle.ParticleDefIndex);
            _writer.WriteInt16(particle.ParticleDefIndex);
            WriteParticleState(particle, definition, hasDeflector);
        }
    }

    private void WriteParticleState(PpfStartupParticleState state, PpfEmitterParticle? definition, bool hasDeflector)
    {
        _writer.WriteSingle(state.Ticks);
        _writer.WriteSingle(state.Life);
        _writer.WriteSingle(state.LifePercent);
        _writer.WriteSingle(state.Zoom);
        WriteDoublePoint(state.Position);
        WriteDoublePoint(state.Velocity);
        WriteDoublePoint(state.EmittedPosition);
        if (definition is { AttachToEmitter: true })
        {
            WriteDoublePoint(state.OriginalPosition);
            _writer.WriteSingle(state.OriginalEmitterAngle);
        }
        _writer.WriteSingle(state.ImageAngle);
        _writer.WriteInt16(state.VariationFlags);
        for (var index = 0; index < 9; index++)
        {
            if ((state.VariationFlags & (1 << index)) != 0)
            {
                _writer.WriteSingle(RequireVariation(state, index));
            }
        }
        _writer.WriteSingle(state.SourceSizeXMultiplier);
        _writer.WriteSingle(state.SourceSizeYMultiplier);
        if (definition is { RandomGradientColor: true })
        {
            _writer.WriteSingle(state.GradientRandom);
        }
        if (definition is { AnimationStartOnRandomFrame: true })
        {
            _writer.WriteInt16(state.AnimationFrameRandom);
        }
        if (hasDeflector)
        {
            _writer.WriteSingle(state.ThicknessHitVariation);
        }
    }

    private void WriteParticleDefState(PpfStartupParticleDefState state)
    {
        _writer.WriteSingle(state.NumberAccumulator);
        _writer.WriteSingle(state.CurrentNumberVariation);
        _writer.WriteInt32(state.ParticlesEmitted);
        _writer.WriteInt32(state.Ticks);
    }

    private void WriteTransform(PpfTransform value)
    {
        _writer.WriteSingle(value.M00);
        _writer.WriteSingle(value.M01);
        _writer.WriteSingle(value.M02);
        _writer.WriteSingle(value.M10);
        _writer.WriteSingle(value.M11);
        _writer.WriteSingle(value.M12);
        _writer.WriteSingle(value.M20);
        _writer.WriteSingle(value.M21);
        _writer.WriteSingle(value.M22);
    }

    private void WriteDoublePoint(PpfDoublePoint value)
    {
        _writer.WriteDouble(value.X);
        _writer.WriteDouble(value.Y);
    }

    private void WriteCount(int count)
    {
        if (count < 0)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFStartupStateCountInvalid0, count));
        }
        _writer.WriteUInt32((uint)count);
    }

    private static PpfEmitterParticle ResolveDefinition(PpfEmitterParticle[] definitions, short definitionIndex)
    {
        if ((uint)definitionIndex >= (uint)definitions.Length)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFParticleDefinitionIndexInvalid0, definitionIndex));
        }
        return definitions[definitionIndex];
    }

    private static float RequireVariation(PpfStartupParticleState state, int index)
    {
        if (index >= state.Variations.Count)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPFParticleVariationCountInsufficient0, state.Variations.Count));
        }
        return state.Variations[index];
    }

    private static void RequireCount(int actual, int expected, string name)
    {
        if (actual != expected)
        {
            throw new InvalidDataException(string.Format(LiarUtil.Core.Strings.PPF0CountMust12, name, expected, actual));
        }
    }
}
