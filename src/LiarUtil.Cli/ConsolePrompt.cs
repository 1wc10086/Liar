namespace LiarUtil.Cli;

internal static class ConsolePrompt
{
    public static string Ask(string label, string? defaultValue = null)
    {
        while (true)
        {
            Console.Write(defaultValue is null ? $"{label}: " : $"{label} [{defaultValue}]: ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (input.Length > 0)
            {
                return input;
            }

            if (defaultValue is not null)
            {
                return defaultValue;
            }

            Console.WriteLine("Value is required.");
        }
    }

    public static int Choose(string label, IReadOnlyList<string> items, int defaultIndex = 0, bool allowBack = true)
    {
        Console.WriteLine(label);
        for (var index = 0; index < items.Count; index++)
        {
            Console.WriteLine($"{index + 1}. {items[index]}");
        }

        if (allowBack)
        {
            Console.WriteLine("0. Back");
        }

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (input.Length == 0 && defaultIndex >= 0 && defaultIndex < items.Count)
            {
                return defaultIndex;
            }

            if (int.TryParse(input, out var value))
            {
                if (allowBack && value == 0)
                {
                    return -1;
                }

                if (value >= 1 && value <= items.Count)
                {
                    return value - 1;
                }
            }

            Console.WriteLine("Invalid choice.");
        }
    }

    public static bool Confirm(string label, bool defaultValue)
    {
        var suffix = defaultValue ? "Y/n" : "y/N";
        while (true)
        {
            Console.Write($"{label} [{suffix}]: ");
            var input = (Console.ReadLine()?.Trim() ?? "").ToLowerInvariant();
            if (input.Length == 0)
            {
                return defaultValue;
            }

            if (input is "y" or "yes")
            {
                return true;
            }

            if (input is "n" or "no")
            {
                return false;
            }

            Console.WriteLine("Invalid choice.");
        }
    }

    public static int AskInt(string label, int defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (input.Length == 0)
            {
                return defaultValue;
            }

            if (int.TryParse(input, out var value))
            {
                return value;
            }

            Console.WriteLine("Invalid number.");
        }
    }

    public static double AskDouble(string label, double defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue:0.###}]: ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (input.Length == 0)
            {
                return defaultValue;
            }

            if (double.TryParse(input, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            Console.WriteLine("Invalid number.");
        }
    }
}
