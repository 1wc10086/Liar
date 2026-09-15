using System.Text.Json;
using System.Text.Json.Serialization;
using LiarUtil.Core.Bnk.Model;

namespace LiarUtil.Core.Bnk.Codec;

internal static class VariantTypes
{
    public static Type? Hierarchy(HierarchyType type)
    {
        return type switch
        {
            HierarchyType.Unknown => typeof(UnknownHierarchy),
            HierarchyType.StatefulPropertySetting => typeof(StatefulPropertySetting),
            HierarchyType.EventAction => typeof(EventAction),
            HierarchyType.Event => typeof(Event),
            HierarchyType.DialogueEvent => typeof(DialogueEvent),
            HierarchyType.Attenuation => typeof(Attenuation),
            HierarchyType.LowFrequencyOscillatorModulator => typeof(LowFrequencyOscillatorModulator),
            HierarchyType.EnvelopeModulator => typeof(EnvelopeModulator),
            HierarchyType.TimeModulator => typeof(TimeModulator),
            HierarchyType.Effect => typeof(Effect),
            HierarchyType.Source => typeof(Source),
            HierarchyType.AudioDevice => typeof(AudioDevice),
            HierarchyType.AudioBus => typeof(AudioBus),
            HierarchyType.AuxiliaryAudioBus => typeof(AuxiliaryAudioBus),
            HierarchyType.Sound => typeof(Sound),
            HierarchyType.SoundPlaylistContainer => typeof(SoundPlaylistContainer),
            HierarchyType.SoundSwitchContainer => typeof(SoundSwitchContainer),
            HierarchyType.SoundBlendContainer => typeof(SoundBlendContainer),
            HierarchyType.ActorMixer => typeof(ActorMixer),
            HierarchyType.MusicTrack => typeof(MusicTrack),
            HierarchyType.MusicSegment => typeof(MusicSegment),
            HierarchyType.MusicPlaylistContainer => typeof(MusicPlaylistContainer),
            HierarchyType.MusicSwitchContainer => typeof(MusicSwitchContainer),
            _ => null,
        };
    }

    public static Type? EventActionProperty(EventActionPropertyType type)
    {
        return type switch
        {
            EventActionPropertyType.PlayAudio => typeof(EventActionPropertyPlayAudio),
            EventActionPropertyType.StopAudio => typeof(EventActionPropertyStopAudio),
            EventActionPropertyType.PauseAudio => typeof(EventActionPropertyPauseAudio),
            EventActionPropertyType.ResumeAudio => typeof(EventActionPropertyResumeAudio),
            EventActionPropertyType.BreakAudio => typeof(EventActionPropertyBreakAudio),
            EventActionPropertyType.SeekAudio => typeof(EventActionPropertySeekAudio),
            EventActionPropertyType.PostEvent => typeof(EventActionPropertyPostEvent),
            EventActionPropertyType.SetBusVolume => typeof(EventActionPropertySetBusVolume),
            EventActionPropertyType.SetVoiceVolume => typeof(EventActionPropertySetVoiceVolume),
            EventActionPropertyType.SetVoicePitch => typeof(EventActionPropertySetVolumePitch),
            EventActionPropertyType.SetVoiceLowPassFilter => typeof(EventActionPropertySetVolumeLowPassFilter),
            EventActionPropertyType.SetVoiceHighPassFilter => typeof(EventActionPropertySetVolumeHighPassFilter),
            EventActionPropertyType.SetMute => typeof(EventActionPropertySetMute),
            EventActionPropertyType.SetGameParameter => typeof(EventActionPropertySetGameParameter),
            EventActionPropertyType.SetStateAvailability => typeof(EventActionPropertySetStateAvailability),
            EventActionPropertyType.ActivateState => typeof(EventActionPropertyActivateState),
            EventActionPropertyType.ActivateSwitch => typeof(EventActionPropertyActivateSwitch),
            EventActionPropertyType.ActivateTrigger => typeof(EventActionPropertyActivateTrigger),
            EventActionPropertyType.SetBypassEffect => typeof(EventActionPropertySetBypassEffect),
            EventActionPropertyType.ReleaseEnvelope => typeof(EventActionPropertyReleaseEnvelope),
            EventActionPropertyType.ResetPlaylist => typeof(EventActionPropertyResetPlaylist),
            _ => null,
        };
    }
}

internal sealed class HierarchyJsonConverter : JsonConverter<Hierarchy>
{
    public override Hierarchy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = (HierarchyType)root.GetProperty("Type").GetInt32();
        var result = new Hierarchy { Type = type };
        if (root.TryGetProperty("Item", out var item) && item.ValueKind == JsonValueKind.Object)
        {
            var clr = VariantTypes.Hierarchy(type);
            if (clr is not null)
            {
                result.Item = item.Deserialize(clr, options);
            }
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Hierarchy value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(Hierarchy.Type), (int)value.Type);
        writer.WritePropertyName(nameof(Hierarchy.Item));
        if (value.Item is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, value.Item, value.Item.GetType(), options);
        }
        writer.WriteEndObject();
    }
}

internal sealed class EventActionPropertyJsonConverter : JsonConverter<EventActionPropertyItem>
{
    public override EventActionPropertyItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = (EventActionPropertyType)root.GetProperty("Type").GetInt32();
        var result = new EventActionPropertyItem { Type = type };
        if (root.TryGetProperty("Item", out var item) && item.ValueKind == JsonValueKind.Object)
        {
            var clr = VariantTypes.EventActionProperty(type);
            if (clr is not null)
            {
                result.Item = item.Deserialize(clr, options);
            }
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, EventActionPropertyItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(EventActionPropertyItem.Type), (int)value.Type);
        writer.WritePropertyName(nameof(EventActionPropertyItem.Item));
        if (value.Item is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, value.Item, value.Item.GetType(), options);
        }
        writer.WriteEndObject();
    }
}

internal sealed class AudioAssociationSettingJsonConverter : JsonConverter<AudioAssociationSetting>
{
    private static readonly JsonSerializerOptions Fallback = new() { IncludeFields = true };

    public override AudioAssociationSetting Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Skip();
            return new AudioAssociationSetting();
        }
        return JsonSerializer.Deserialize<AudioAssociationSetting>(ref reader, Fallback) ?? new AudioAssociationSetting();
    }

    public override void Write(Utf8JsonWriter writer, AudioAssociationSetting value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, Fallback);
    }
}
