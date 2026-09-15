using LiarUtil.Core.Ppf.Models;

namespace LiarUtil.Core.Ppf.Decoding;

internal sealed partial class PpfDecoder
{
    private PpfLayer ReadLayer()
    {
        var name = _reader.ReadStringByByteHead();
        var emitters = ReadList(ReadLayerEmitter);
        var deflectors = ReadList(ReadLayerDeflector);
        var blockers = ReadList(ReadLayerBlocker);
        var offset = ReadValue2();
        var angle = ReadValue1();
        var unknown1 = _reader.ReadStringByByteHead();
        var unknown3 = _reader.ReadBytes(32);
        var unknown2 = ReadStringList();
        var unknown4 = _reader.ReadBytes(36);
        var forces = ReadList(ReadLayerForce);
        var unknown5 = _reader.ReadBytes(28);
        return new PpfLayer
        {
            Name = name,
            Emitters = emitters,
            Deflectors = deflectors,
            Blockers = blockers,
            Forces = forces,
            Offset = offset,
            Angle = angle,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            Unknown3 = unknown3,
            Unknown4 = unknown4,
            Unknown5 = unknown5,
        };
    }

    private PpfLayerEmitter ReadLayerEmitter()
    {
        var unknown1 = _reader.ReadSingle();
        var unknown2 = _reader.ReadSingle();
        var unknown3 = _reader.ReadSingle();
        var unknown4 = _reader.ReadSingle();
        var unknown5 = _reader.ReadSingle();
        var unknown6 = _reader.ReadSingle();
        var unknown7 = _reader.ReadSingle();
        var unknown8 = _reader.ReadSingle();
        var unknown9 = _reader.ReadSingle();
        var unknown10 = _reader.ReadSingle();
        var unknown11 = _reader.ReadSingle();
        var unknown12 = _reader.ReadSingle();
        var unknown13 = _reader.ReadInt32();
        var unknown14 = _reader.ReadInt32();
        var preloadFrame = _reader.ReadInt32();
        var unknown15 = _reader.ReadInt32();
        var name = _reader.ReadStringByByteHead();
        var geom = _reader.ReadInt32();
        var unknown16 = _reader.ReadSingle();
        var unknown17 = _reader.ReadSingle();
        var geom4If2 = _reader.ReadBoolean();
        var emitIn = _reader.ReadBoolean();
        var emitOut = _reader.ReadBoolean();
        var tintColor = ReadColor();
        var unknown18 = _reader.ReadInt32();
        var emitAtPoint = new PpfIntPoint { X = _reader.ReadInt32() };
        var type = _reader.ReadInt32();
        var position = ReadValue2();
        var points = ReadList(ReadValue2Simple);
        var values = new PpfLayerEmitterValues
        {
            Life = ReadValue1(),
            Number = ReadValue1(),
            SizeX = ReadValue1(),
            Velocity = ReadValue1(),
            Weight = ReadValue1(),
            Spin = ReadValue1(),
            MotionRand = ReadValue1(),
            Bounce = ReadValue1(),
            Zoom = ReadValue1(),
            Visibility = ReadValue1(),
            TintStrength = ReadValue1(),
            EmissionAngle = ReadValue1(),
            EmissionRange = ReadValue1(),
            Active = ReadValue1(),
            Angle = ReadValue1(),
            XRadius = ReadValue1(),
            YRadius = ReadValue1(),
        };
        emitAtPoint.Y = _reader.ReadInt32();
        var unknown19 = _reader.ReadInt32();
        values.SizeY = ReadValue1();
        var unknown20 = _reader.ReadInt32();
        values.Unknown18 = ReadValue1();
        var maskPath = ReadStringList();
        var mask = _reader.ReadBoolean();
        var maskName = _reader.ReadStringByByteHead();
        var unknown21 = _reader.ReadInt32();
        var unknown22 = _reader.ReadInt32();
        var invertMask = _reader.ReadBoolean();
        var unknown23 = _reader.ReadInt32();
        var unknown24 = _reader.ReadInt32();
        var isSuper = _reader.ReadBoolean();
        var free = ReadList(() => _reader.ReadInt16());
        var unknown25 = _reader.ReadInt32();
        var unknown26 = _reader.ReadSingle();
        var unknown27 = _reader.ReadSingle();
        return new PpfLayerEmitter
        {
            Name = name,
            Type = type,
            Geom = geom,
            Geom4If2 = geom4If2,
            IsSuper = isSuper,
            PreloadFrame = preloadFrame,
            EmitIn = emitIn,
            EmitOut = emitOut,
            EmitAtPoint = emitAtPoint,
            TintColor = tintColor,
            Mask = mask,
            MaskName = maskName,
            MaskPath = maskPath,
            InvertMask = invertMask,
            Position = position,
            Points = points,
            Values = values,
            Free = free,
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
            Unknown22 = unknown22,
            Unknown23 = unknown23,
            Unknown24 = unknown24,
            Unknown25 = unknown25,
            Unknown26 = unknown26,
            Unknown27 = unknown27,
        };
    }

    private PpfLayerDeflector ReadLayerDeflector()
    {
        var name = _reader.ReadStringByByteHead();
        var bounce = _reader.ReadInt32();
        var hit = _reader.ReadInt32();
        var thickness = _reader.ReadInt32();
        var visible = _reader.ReadBoolean();
        var position = ReadValue2();
        var points = ReadList(ReadValue2Simple);
        var active = ReadValue1();
        var angle = ReadValue1();
        return new PpfLayerDeflector
        {
            Name = name,
            Bounce = bounce,
            Hit = hit,
            Thickness = thickness,
            Visible = visible,
            Position = position,
            Points = points,
            Active = active,
            Angle = angle,
        };
    }

    private PpfLayerBlocker ReadLayerBlocker()
    {
        var name = _reader.ReadStringByByteHead();
        var unknown1 = _reader.ReadInt32();
        var unknown2 = _reader.ReadInt32();
        var unknown3 = _reader.ReadInt32();
        var unknown4 = _reader.ReadInt32();
        var unknown5 = _reader.ReadInt32();
        var position = ReadValue2();
        var points = ReadList(ReadValue2Simple);
        var active = ReadValue1();
        var angle = ReadValue1();
        return new PpfLayerBlocker
        {
            Name = name,
            Position = position,
            Active = active,
            Angle = angle,
            Points = points,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            Unknown3 = unknown3,
            Unknown4 = unknown4,
            Unknown5 = unknown5,
        };
    }

    private PpfLayerForce ReadLayerForce()
    {
        var name = _reader.ReadStringByByteHead();
        var visible = _reader.ReadBoolean();
        var position = ReadValue2();
        var active = ReadValue1();
        var unknown1 = ReadValue1();
        var strength = ReadValue1();
        var width = ReadValue1();
        var height = ReadValue1();
        var angle = ReadValue1();
        var direction = ReadValue1();
        return new PpfLayerForce
        {
            Name = name,
            Visible = visible,
            Position = position,
            Active = active,
            Strength = strength,
            Width = width,
            Height = height,
            Angle = angle,
            Direction = direction,
            Unknown1 = unknown1,
        };
    }

    private PpfColor ReadColor() => new()
    {
        Red = _reader.ReadUInt32(),
        Green = _reader.ReadUInt32(),
        Blue = _reader.ReadUInt32(),
    };

    private PpfRgb ReadRgb() => new()
    {
        Red = _reader.ReadByte(),
        Green = _reader.ReadByte(),
        Blue = _reader.ReadByte(),
    };

    private PpfSize ReadSize() => new()
    {
        Width = _reader.ReadInt32(),
        Height = _reader.ReadInt32(),
    };

    private PpfFrameRange ReadFrameRange() => new()
    {
        Begin = _reader.ReadInt32(),
        End = _reader.ReadInt32(),
    };
}
