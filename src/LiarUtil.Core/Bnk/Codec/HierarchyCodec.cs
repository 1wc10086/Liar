using LiarUtil.Core.Bnk.Model;

namespace LiarUtil.Core.Bnk.Codec;

internal static class HierarchyCodec
{
    public static void List(BankContext c, List<Hierarchy> list)
    {
        if (c.Reading)
        {
            var count = (int)c.Reader.ReadUInt32();
            list.Clear();
            for (var i = 0; i < count; i++)
            {
                list.Add(ReadItem(c));
            }
        }
        else
        {
            c.Writer.WriteUInt32((uint)list.Count);
            foreach (var item in list)
            {
                WriteItem(c, item);
            }
        }
    }

    private static Hierarchy ReadItem(BankContext c)
    {
        var raw = c.Reader.ReadUInt8();
        var size = (int)c.Reader.ReadUInt32();
        var body = c.Slice(size);
        var type = HierarchyTypeWire.Instance.FromRaw(c.Version, raw);
        var hierarchy = new Hierarchy { Type = type };
        switch (type)
        {
            case HierarchyType.StatefulPropertySetting:
            {
                var value = new StatefulPropertySetting();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.EventAction:
            {
                var value = new EventAction();
                EventActionCodec.Read(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.Event:
            {
                var value = new Event();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.DialogueEvent:
            {
                var value = new DialogueEvent();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.Attenuation:
            {
                var value = new Attenuation();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.LowFrequencyOscillatorModulator:
            {
                var value = new LowFrequencyOscillatorModulator();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.EnvelopeModulator:
            {
                var value = new EnvelopeModulator();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.TimeModulator:
            {
                var value = new TimeModulator();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.Effect:
            {
                var value = new Effect();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.Source:
            {
                var value = new Source();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.AudioDevice:
            {
                var value = new AudioDevice();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.AudioBus:
            {
                var value = new AudioBus();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.AuxiliaryAudioBus:
            {
                var value = new AuxiliaryAudioBus();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.Sound:
            {
                var value = new Sound();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.SoundPlaylistContainer:
            {
                var value = new SoundPlaylistContainer();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.SoundSwitchContainer:
            {
                var value = new SoundSwitchContainer();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.SoundBlendContainer:
            {
                var value = new SoundBlendContainer();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.ActorMixer:
            {
                var value = new ActorMixer();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.MusicTrack:
            {
                var value = new MusicTrack();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.MusicSegment:
            {
                var value = new MusicSegment();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.MusicPlaylistContainer:
            {
                var value = new MusicPlaylistContainer();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            case HierarchyType.MusicSwitchContainer:
            {
                var value = new MusicSwitchContainer();
                BankCodecImpl.Section(body, value);
                hierarchy.Item = value;
                break;
            }
            default:
            {
                var rest = body.Reader.Rest();
                hierarchy.Item = new UnknownHierarchy { Type = raw, Data = rest.ToArray() };
                break;
            }
        }
        return hierarchy;
    }

    private static void WriteItem(BankContext c, Hierarchy hierarchy)
    {
        var raw = HierarchyTypeWire.Instance.ToRaw(c.Version, hierarchy.Type);
        c.Writer.WriteUInt8((byte)raw);
        var sub = BankWire.Writer();
        var body = new BankContext(sub, c.Version);
        switch (hierarchy.Item)
        {
            case StatefulPropertySetting value:
                BankCodecImpl.Section(body, value);
                break;
            case EventAction value:
                EventActionCodec.Write(body, value);
                break;
            case Event value:
                BankCodecImpl.Section(body, value);
                break;
            case DialogueEvent value:
                BankCodecImpl.Section(body, value);
                break;
            case Attenuation value:
                BankCodecImpl.Section(body, value);
                break;
            case LowFrequencyOscillatorModulator value:
                BankCodecImpl.Section(body, value);
                break;
            case EnvelopeModulator value:
                BankCodecImpl.Section(body, value);
                break;
            case TimeModulator value:
                BankCodecImpl.Section(body, value);
                break;
            case Effect value:
                BankCodecImpl.Section(body, value);
                break;
            case AudioDevice value:
                BankCodecImpl.Section(body, value);
                break;
            case AudioBus value:
                BankCodecImpl.Section(body, value);
                break;
            case Sound value:
                BankCodecImpl.Section(body, value);
                break;
            case SoundPlaylistContainer value:
                BankCodecImpl.Section(body, value);
                break;
            case SoundSwitchContainer value:
                BankCodecImpl.Section(body, value);
                break;
            case SoundBlendContainer value:
                BankCodecImpl.Section(body, value);
                break;
            case ActorMixer value:
                BankCodecImpl.Section(body, value);
                break;
            case MusicTrack value:
                BankCodecImpl.Section(body, value);
                break;
            case MusicSegment value:
                BankCodecImpl.Section(body, value);
                break;
            case MusicPlaylistContainer value:
                BankCodecImpl.Section(body, value);
                break;
            case MusicSwitchContainer value:
                BankCodecImpl.Section(body, value);
                break;
            case UnknownHierarchy value:
                body.Writer.WriteBytes(value.Data);
                break;
        }
        c.Writer.WriteUInt32((uint)sub.Length);
        c.Writer.WriteBytes(sub.ToArray());
    }
}
