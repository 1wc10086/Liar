namespace LiarUtil.Core.Wma;

internal static class WmaTables
{
    internal static readonly uint[][] CoefCodes = [WmaCoefTablesA.Coef0Codes, WmaCoefTablesA.Coef1Codes, WmaCoefTablesB.Coef2Codes, WmaCoefTablesC.Coef3Codes, WmaCoefTablesD.Coef4Codes, WmaCoefTablesD.Coef5Codes];

    internal static readonly byte[][] CoefBits = [WmaCoefTablesA.Coef0Bits, WmaCoefTablesA.Coef1Bits, WmaCoefTablesB.Coef2Bits, WmaCoefTablesC.Coef3Bits, WmaCoefTablesD.Coef4Bits, WmaCoefTablesD.Coef5Bits];

    internal static readonly ushort[][] Levels = [WmaLevelTables.Levels0, WmaLevelTables.Levels1, WmaLevelTables.Levels2, WmaLevelTables.Levels3, WmaLevelTables.Levels4, WmaLevelTables.Levels5];
}
