namespace LiarUtil.Core.Bnk;

public readonly record struct BankVersion(uint Number)
{
    public static readonly IReadOnlyList<uint> Supported =
    [
        72, 88, 112, 113, 118, 120, 125, 128, 132, 134, 135, 140, 145, 150,
    ];

    public bool IsSupported => Supported.Contains(Number);

    public bool AtLeast(uint min) => Number >= min;

    public bool In(uint min, uint max) => Number >= min && (max == 0 || Number < max);

    public override string ToString() => Number.ToString();
}
