using System.Globalization;
using LiarUtil.Core;

namespace LiarUtil.Cli;

internal static class Program
{
    private static async Task Main()
    {
        Strings.Culture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        await new CliApplication().RunAsync();
    }
}
