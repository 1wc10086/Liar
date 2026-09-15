using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Decoding;

internal sealed partial class PpfDecoder
{
    private PpfTexture ReadTexture()
    {
        var name = _reader.ReadStringByByteHead();
        var cell = _reader.ReadInt16();
        var row = _reader.ReadInt16();
        var padded = _reader.ReadBoolean();
        var path = _reader.ReadStringByByteHead();
        return new PpfTexture { Name = name, Cell = cell, Row = row, Padded = padded, Path = path };
    }

    private PpfEmitter ReadEmitter()
    {
        var unknown1 = _reader.ReadInt32();
        var name = _reader.ReadStringByByteHead();
        var keepInOrder = _reader.ReadBoolean();
        var unknown2 = _reader.ReadInt32();
        var oldestInFront = _reader.ReadBoolean();
        var particles = ReadList(ReadEmitterParticle);
        var unknown3 = _reader.ReadInt32();
        var values = ReadEmitterValues();
        var unknown4 = _reader.ReadInt32();
        var unknown5 = _reader.ReadInt32();
        return new PpfEmitter
        {
            Name = name,
            KeepInOrder = keepInOrder,
            OldestInFront = oldestInFront,
            Particles = particles,
            Values = values,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            Unknown3 = unknown3,
            Unknown4 = unknown4,
            Unknown5 = unknown5,
        };
    }

    private PpfEmitterParticle ReadEmitterParticle()
    {
        var unknown1 = _reader.ReadInt32();
        var unknown2 = _reader.ReadInt32();
        var unknown3 = _reader.ReadInt32();
        var unknown4 = _reader.ReadSingle();
        var unknown5 = _reader.ReadInt32();
        var unknown6 = _reader.ReadInt32();
        var unknown7 = _reader.ReadInt32();
        var unknown8 = _reader.ReadInt32();
        var unknown9 = _reader.ReadInt32();
        var unknown10 = _reader.ReadInt32();
        var unknown11 = _reader.ReadInt32();
        var unknown12 = _reader.ReadInt32();
        var unknown13 = _reader.ReadInt32();
        var unknown14 = _reader.ReadInt32();
        var unknown15 = _reader.ReadInt32();
        var unknown16 = _reader.ReadInt32();
        var instance = _reader.ReadBoolean();
        var single = _reader.ReadBoolean();
        var preserveColor = _reader.ReadBoolean();
        var attachToEmitter = _reader.ReadBoolean();
        var attachValue = _reader.ReadSingle();
        var flipHorizontal = _reader.ReadBoolean();
        var flipVertical = _reader.ReadBoolean();
        var animationStartOnRandomFrame = _reader.ReadBoolean();
        var repeatColor = _reader.ReadInt32();
        var repeatAlpha = _reader.ReadInt32();
        var linkTransparencyToColor = _reader.ReadBoolean();
        var name = _reader.ReadStringByByteHead();
        var angleAlignToMotion = _reader.ReadBoolean();
        var angleRandomAlign = _reader.ReadBoolean();
        var angleKeepAlignedToMotion = _reader.ReadBoolean();
        var angleValue = _reader.ReadInt32();
        var angleAlignOffset = _reader.ReadInt32();
        var animationSpeed = _reader.ReadInt32();
        var randomGradientColor = _reader.ReadBoolean();
        var unknown17 = _reader.ReadInt32();
        var texture = _reader.ReadInt32();
        var colors = ReadList(ReadColorPoint);
        var alphas = ReadList(ReadAlphaPoint);
        var values = new PpfParticleValues
        {
            Life = ReadValue1(),
            Number = ReadValue1(),
            SizeX = ReadValue1(),
            Velocity = ReadValue1(),
            Weight = ReadValue1(),
            Spin = ReadValue1(),
            MotionRand = ReadValue1(),
            Bounce = ReadValue1(),
            LifeVariation = ReadValue1(),
            NumberVariation = ReadValue1(),
            SizeXVariation = ReadValue1(),
            VelocityVariation = ReadValue1(),
            WeightVariation = ReadValue1(),
            SpinVariation = ReadValue1(),
            MotionRandVariation = ReadValue1(),
            BounceVariation = ReadValue1(),
            SizeXOverLife = ReadValue1(),
            VelocityOverLife = ReadValue1(),
            WeightOverLife = ReadValue1(),
            SpinOverLife = ReadValue1(),
            MotionRandOverLife = ReadValue1(),
            BounceOverLife = ReadValue1(),
            Visibility = ReadValue1(),
        };
        var referencePointOffset = ReadVector2();
        var unknown18 = _reader.ReadInt32();
        var unknown19 = _reader.ReadInt32();
        var lockAspect = _reader.ReadBoolean();
        values.SizeY = ReadValue1();
        values.SizeYVariation = ReadValue1();
        values.SizeYOverLife = ReadValue1();
        var angleRange = _reader.ReadInt32();
        var angleOffset = _reader.ReadInt32();
        var getColorFromLayer = _reader.ReadBoolean();
        var updateColorFromLayer = _reader.ReadBoolean();
        var useEmitterAngleAndRange = _reader.ReadBoolean();
        values.EmissionAngle = ReadValue1();
        values.EmissionRange = ReadValue1();
        var unknown20 = _reader.ReadInt32();
        var unknown21 = ReadValue1();
        var useKeyColorOnly = _reader.ReadBoolean();
        var updateTransparencyFromLayer = _reader.ReadBoolean();
        var useNextColorKey = _reader.ReadBoolean();
        var numberOfEachColor = _reader.ReadInt32();
        var getTransparencyFromLayer = _reader.ReadBoolean();
        return new PpfEmitterParticle
        {
            Name = name,
            Texture = texture,
            Instance = instance,
            Single = single,
            FlipHorizontal = flipHorizontal,
            FlipVertical = flipVertical,
            LockAspect = lockAspect,
            AnimationSpeed = animationSpeed,
            AnimationStartOnRandomFrame = animationStartOnRandomFrame,
            UseEmitterAngleAndRange = useEmitterAngleAndRange,
            AttachToEmitter = attachToEmitter,
            AttachValue = attachValue,
            ReferencePointOffset = referencePointOffset,
            AngleValue = angleValue,
            AngleRange = angleRange,
            AngleOffset = angleOffset,
            AngleRandomAlign = angleRandomAlign,
            AngleAlignOffset = angleAlignOffset,
            AngleAlignToMotion = angleAlignToMotion,
            AngleKeepAlignedToMotion = angleKeepAlignedToMotion,
            RepeatColor = repeatColor,
            RandomGradientColor = randomGradientColor,
            NumberOfEachColor = numberOfEachColor,
            UseKeyColorOnly = useKeyColorOnly,
            UseNextColorKey = useNextColorKey,
            GetColorFromLayer = getColorFromLayer,
            UpdateColorFromLayer = updateColorFromLayer,
            Colors = colors,
            RepeatAlpha = repeatAlpha,
            PreserveColor = preserveColor,
            LinkTransparencyToColor = linkTransparencyToColor,
            GetTransparencyFromLayer = getTransparencyFromLayer,
            UpdateTransparencyFromLayer = updateTransparencyFromLayer,
            Alphas = alphas,
            Values = values,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            Unknown3 = unknown3,
            Unknown4 = unknown4,
            Unknown5 = unknown5,
            Unknown6 = unknown6,
            Unknown7 = unknown7,
            Unknown8 = unknown8,
            Unknown9 = unknown9,
            Unknown10 = unknown10,
            Unknown11 = unknown11,
            Unknown12 = unknown12,
            Unknown13 = unknown13,
            Unknown14 = unknown14,
            Unknown15 = unknown15,
            Unknown16 = unknown16,
            Unknown17 = unknown17,
            Unknown18 = unknown18,
            Unknown19 = unknown19,
            Unknown20 = unknown20,
            Unknown21 = unknown21,
        };
    }
}
