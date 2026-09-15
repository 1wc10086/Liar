using Avalonia.Media;

namespace LiarUtil.Gui;

internal static class FontDefaults
{
    public static readonly FontManagerOptions Options = new()
    {
        FontFallbacks =
        [
            new FontFallback
            {
                FontFamily = new FontFamily("avares://LiarUtil.Gui/Assets/Fonts/wqy-microhei.ttc#WenQuanYi Micro Hei"),
            },
        ],
    };
}
