namespace LiarUtil.Core.RsbPatch;

internal sealed class VcdiffBlockHash
{
    private const int MaxProbes = 16;

    private const int MaxMatchesToCheck = 64;

    private readonly byte[] _source;

    private readonly int[] _hashTable;

    private readonly int[] _nextBlockTable;

    private readonly int[] _lastBlockTable;

    private readonly int _blockSize;

    private readonly int _startingOffset;

    private readonly int _hashTableMask;

    private int _lastBlockAdded = -1;

    public VcdiffBlockHash(byte[] source, int startingOffset, int blockSize, bool populate)
    {
        _source = source;
        _blockSize = blockSize;
        _startingOffset = startingOffset;
        BlocksCount = source.Length / blockSize;

        long minimum = (source.Length / sizeof(int)) + 1L;
        long tableSize = 1L;
        while (tableSize < minimum)
        {
            tableSize <<= 1;
        }

        _hashTable = new int[tableSize];
        _hashTableMask = (int)(tableSize - 1);
        _nextBlockTable = new int[BlocksCount];
        _lastBlockTable = new int[BlocksCount];
        Array.Fill(_hashTable, -1);
        Array.Fill(_nextBlockTable, -1);
        Array.Fill(_lastBlockTable, -1);

        if (populate)
        {
            AddAllBlocksThrough(source.Length);
        }
    }

    public int BlocksCount { get; }

    public void AddAllBlocksThrough(int endIndex)
    {
        if (endIndex > _source.Length || _source.Length < _blockSize)
        {
            return;
        }

        if (endIndex <= _lastBlockAdded * _blockSize)
        {
            return;
        }

        var limit = Math.Min(endIndex, _source.Length - _blockSize + 1);
        for (var position = (_lastBlockAdded + 1) * _blockSize; position < limit; position += _blockSize)
        {
            AddBlock(VcdiffRollingHash.Compute(_source, position, _blockSize));
        }
    }

    public void AddOneIndexHash(int index, uint hash)
    {
        if (index == (_lastBlockAdded + 1) * _blockSize)
        {
            AddBlock(hash);
        }
    }

    public void FindBestMatch(uint hash, int candidateStart, int targetStart, int targetSize, byte[] target, ref VcdiffMatch match)
    {
        var counter = 0;
        var blockNumber = SkipNonMatching(_hashTable[(int)(hash & (uint)_hashTableMask)], candidateStart, target);
        while (blockNumber >= 0 && ++counter <= MaxMatchesToCheck)
        {
            var sourceOffset = blockNumber * _blockSize;
            var sourceEnd = sourceOffset + _blockSize;
            var targetOffset = candidateStart - targetStart;
            var targetEnd = targetOffset + _blockSize;
            var matchSize = _blockSize;

            var limitLeft = Math.Min(sourceOffset, targetOffset);
            var left = 0;
            while (left < limitLeft && _source[sourceOffset - left - 1] == target[targetStart + targetOffset - left - 1])
            {
                left++;
            }

            sourceOffset -= left;
            targetOffset -= left;
            matchSize += left;

            var limitRight = Math.Min(_source.Length - sourceEnd, targetSize - targetEnd);
            var right = 0;
            while (right < limitRight && _source[sourceEnd + right] == target[targetStart + targetEnd + right])
            {
                right++;
            }

            matchSize += right;
            match.ReplaceIfBetter(matchSize, sourceOffset + _startingOffset, targetOffset);
            blockNumber = SkipNonMatching(_nextBlockTable[blockNumber], candidateStart, target);
        }
    }

    private void AddBlock(uint hash)
    {
        var blockNumber = _lastBlockAdded + 1;
        if (blockNumber >= BlocksCount)
        {
            return;
        }

        var index = (int)(hash & (uint)_hashTableMask);
        var first = _hashTable[index];
        if (first < 0)
        {
            _hashTable[index] = blockNumber;
            _lastBlockTable[blockNumber] = blockNumber;
        }
        else
        {
            var last = _lastBlockTable[first];
            _nextBlockTable[last] = blockNumber;
            _lastBlockTable[first] = blockNumber;
        }

        _lastBlockAdded = blockNumber;
    }

    private int SkipNonMatching(int blockNumber, int targetOffset, byte[] target)
    {
        var probes = 0;
        while (blockNumber >= 0 && !BlockContentsMatch(blockNumber, targetOffset, target))
        {
            if (++probes > MaxProbes)
            {
                return -1;
            }

            blockNumber = _nextBlockTable[blockNumber];
        }

        return blockNumber;
    }

    private bool BlockContentsMatch(int blockNumber, int targetOffset, byte[] target)
    {
        var sourceOffset = blockNumber * _blockSize;
        if (sourceOffset > _source.Length - _blockSize || targetOffset > target.Length - _blockSize)
        {
            return false;
        }

        return _source.AsSpan(sourceOffset, _blockSize).SequenceEqual(target.AsSpan(targetOffset, _blockSize));
    }
}

internal struct VcdiffMatch
{
    public int Size;

    public int SourceOffset;

    public int TargetOffset;

    public void ReplaceIfBetter(int size, int sourceOffset, int targetOffset)
    {
        if (size > Size)
        {
            Size = size;
            SourceOffset = sourceOffset;
            TargetOffset = targetOffset;
        }
    }
}
