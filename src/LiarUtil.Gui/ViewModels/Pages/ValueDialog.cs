using System.Globalization;

namespace LiarUtil.Gui.ViewModels.Pages;

public sealed class ValueDialog
{
    private readonly Func<string, bool> _validate;
    private readonly Action<string> _apply;

    private ValueDialog(string title, string watermark, string value, Func<string, bool> validate, Action<string> apply)
    {
        Title = title;
        Watermark = watermark;
        Value = value;
        _validate = validate;
        _apply = apply;
    }

    public string Title { get; }

    public string Watermark { get; }

    public string Value { get; }

    public static ValueDialog ForText(string title, string watermark, string value, Action<string> apply) =>
        new(title, watermark, value, input => input.Trim().Length > 0, input => apply(input.Trim()));

    public static ValueDialog ForNumber(string title, string watermark, string value, Action<double> apply) =>
        new(title, watermark, value, input => TryParse(input, out _),
            input => apply(Parse(input)));

    public bool Validate(string input) => _validate(input);

    public void Apply(string input) => _apply(input);

    private static bool TryParse(string input, out double value) =>
        double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0;

    private static double Parse(string input) => TryParse(input, out var value) ? value : 0;
}
