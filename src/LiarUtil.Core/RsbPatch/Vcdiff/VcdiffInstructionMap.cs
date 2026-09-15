namespace LiarUtil.Core.RsbPatch;

internal static class VcdiffInstructionMap
{
    private const int InstructionModeCount = 12;

    private static readonly int[][] FirstOpcodes = BuildFirst();

    private static readonly Dictionary<int, int[]?[]> SecondOpcodes = BuildSecond();

    public static int LookupFirst(byte instruction, byte size, byte mode)
    {
        var index = instruction == VcdiffCodeTable.Copy ? instruction + mode : instruction;
        var row = FirstOpcodes[index];
        return size < row.Length ? row[size] : VcdiffCodeTable.NoOpcode;
    }

    public static int LookupSecond(byte firstOpcode, byte instruction, byte size, byte mode)
    {
        if (!SecondOpcodes.TryGetValue(firstOpcode, out var rows))
        {
            return VcdiffCodeTable.NoOpcode;
        }

        var index = instruction == VcdiffCodeTable.Copy ? instruction + mode : instruction;
        var row = rows[index];
        if (row is null || size >= row.Length)
        {
            return VcdiffCodeTable.NoOpcode;
        }

        return row[size];
    }

    private static int[][] BuildFirst()
    {
        var maxSize = 0;
        var inst1 = VcdiffCodeTable.Inst1;
        var inst2 = VcdiffCodeTable.Inst2;
        var size1 = VcdiffCodeTable.Size1;
        var mode1 = VcdiffCodeTable.Mode1;
        foreach (var size in size1)
        {
            maxSize = Math.Max(maxSize, size);
        }

        var rows = new int[InstructionModeCount][];
        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = new int[maxSize + 1];
            Array.Fill(rows[index], VcdiffCodeTable.NoOpcode);
        }

        for (var opcode = 0; opcode < VcdiffCodeTable.Size; opcode++)
        {
            if (inst2[opcode] == VcdiffCodeTable.Noop)
            {
                AddFirst(rows, inst1[opcode], size1[opcode], mode1[opcode], (byte)opcode);
            }
            else if (inst1[opcode] == VcdiffCodeTable.Noop)
            {
                AddFirst(rows, inst1[opcode], size1[opcode], mode1[opcode], (byte)opcode);
            }
        }

        return rows;
    }

    private static void AddFirst(int[][] rows, byte instruction, byte size, byte mode, byte opcode)
    {
        var index = instruction == VcdiffCodeTable.Copy ? instruction + mode : instruction;
        if (rows[index][size] == VcdiffCodeTable.NoOpcode)
        {
            rows[index][size] = opcode;
        }
    }

    private static Dictionary<int, int[]?[]> BuildSecond()
    {
        var result = new Dictionary<int, int[]?[]>();
        var inst1 = VcdiffCodeTable.Inst1;
        var inst2 = VcdiffCodeTable.Inst2;
        var size2 = VcdiffCodeTable.Size2;
        var mode2 = VcdiffCodeTable.Mode2;
        var maxSize = 0;
        foreach (var size in size2)
        {
            maxSize = Math.Max(maxSize, size);
        }

        for (var opcode = 0; opcode < VcdiffCodeTable.Size; opcode++)
        {
            if (inst1[opcode] == VcdiffCodeTable.Noop || inst2[opcode] == VcdiffCodeTable.Noop)
            {
                continue;
            }

            var first = LookupFirst(inst1[opcode], VcdiffCodeTable.Size1[opcode], VcdiffCodeTable.Mode1[opcode]);
            if (first == VcdiffCodeTable.NoOpcode)
            {
                continue;
            }

            if (!result.TryGetValue(first, out var rows))
            {
                rows = new int[InstructionModeCount][];
                result[first] = rows;
            }

            var index = inst2[opcode] + mode2[opcode];
            var row = rows[index] ??= CreateFilled(maxSize + 1);
            if (row[size2[opcode]] == VcdiffCodeTable.NoOpcode)
            {
                row[size2[opcode]] = opcode;
            }
        }

        return result;
    }

    private static int[] CreateFilled(int length)
    {
        var row = new int[length];
        Array.Fill(row, VcdiffCodeTable.NoOpcode);
        return row;
    }
}
