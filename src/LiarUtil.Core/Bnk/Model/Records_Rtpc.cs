namespace LiarUtil.Core.Bnk.Model;

internal sealed class RealTimeParameterControlSettingItem
{
    public long Type = 0L;
    public Parameter Parameter = new();
    public long U2 = 0L;
    public CoordinateMode Mode = default;
    public List<CoordinatePoint> Point = [];
    public PropertyCategory U1 = default;
}

internal sealed class RealTimeParameterControlSetting
{
    public List<RealTimeParameterControlSettingItem> Item = [];
}

internal sealed class StateSettingAttribute
{
    public long Type = 0L;
    public PropertyCategory Category = default;
    public long U1 = 0L;
}

internal sealed class StateSettingApplyItem
{
    public long Target = 0L;
    public long Setting = 0L;
}

internal sealed class StateSettingItem
{
    public long Group = 0L;
    public TimePoint ChangeOccurAt = default;
    public List<StateSettingApplyItem> Apply = [];
}

internal sealed class StateSetting
{
    public List<StateSettingItem> Item = [];
    public List<StateSettingAttribute> Attribute = [];
}
