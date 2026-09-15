namespace LiarUtil.Core.Pam.Xfl;

internal readonly record struct PamXflColor(double Red, double Green, double Blue, double Alpha)
{
    public bool Matches(PamXflColor other) =>
        Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
}
