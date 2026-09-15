namespace LiarUtil.Core.Atlas.Packing;

internal sealed class MaxRectsBinPack
{
    public enum FreeRectChoiceHeuristic
    {
        RectBestShortSideFit,
        RectBestLongSideFit,
        RectBestAreaFit,
        RectBottomLeftRule,
        RectContactPointRule,
    }

    private readonly int _binWidth;
    private readonly int _binHeight;
    private readonly bool _allowRotations;
    private readonly List<Rect> _usedRectangles = [];
    private readonly List<Rect> _freeRectangles = [];

    public MaxRectsBinPack(int width, int height, bool rotations)
    {
        _binWidth = width;
        _binHeight = height;
        _allowRotations = rotations;
        _freeRectangles.Add(new Rect(0, 0, width, height));
    }

    public Rect Insert(int width, int height, FreeRectChoiceHeuristic method)
    {
        var node = FindPosition(width + 1, height + 1, method);
        if (node.Height == 0)
        {
            return node;
        }

        var count = _freeRectangles.Count;
        for (var i = 0; i < count; ++i)
        {
            if (SplitFreeNode(_freeRectangles[i], node))
            {
                _freeRectangles.RemoveAt(i);
                --i;
                --count;
            }
        }

        PruneFreeList();
        _usedRectangles.Add(node);
        return node;
    }

    private Rect FindPosition(int width, int height, FreeRectChoiceHeuristic method)
    {
        var score1 = 0;
        var score2 = 0;
        var node = method switch
        {
            FreeRectChoiceHeuristic.RectBestShortSideFit => FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2),
            FreeRectChoiceHeuristic.RectBottomLeftRule => FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2),
            FreeRectChoiceHeuristic.RectContactPointRule => FindPositionForNewNodeContactPoint(width, height, ref score1),
            FreeRectChoiceHeuristic.RectBestLongSideFit => FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1),
            _ => FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2),
        };
        return node;
    }

    private Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestY, ref int bestX)
    {
        var bestNode = new Rect();
        bestY = int.MaxValue;
        foreach (var free in _freeRectangles)
        {
            if (free.Width >= width && free.Height >= height)
            {
                var topSideY = free.Y + height;
                if (topSideY < bestY || (topSideY == bestY && free.X < bestX))
                {
                    bestNode = new Rect(free.X, free.Y, width, height);
                    bestY = topSideY;
                    bestX = free.X;
                }
            }

            if (_allowRotations && free.Width >= height && free.Height >= width)
            {
                var topSideY = free.Y + width;
                if (topSideY < bestY || (topSideY == bestY && free.X < bestX))
                {
                    bestNode = new Rect(free.X, free.Y, height, width);
                    bestY = topSideY;
                    bestX = free.X;
                }
            }
        }

        return bestNode;
    }

    private Rect FindPositionForNewNodeBestShortSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
    {
        var bestNode = new Rect();
        bestShortSideFit = int.MaxValue;
        foreach (var free in _freeRectangles)
        {
            if (free.Width >= width && free.Height >= height)
            {
                var leftoverHoriz = Math.Abs(free.Width - width);
                var leftoverVert = Math.Abs(free.Height - height);
                var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                var longSideFit = Math.Max(leftoverHoriz, leftoverVert);
                if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                {
                    bestNode = new Rect(free.X, free.Y, width, height);
                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }

            if (_allowRotations && free.Width >= height && free.Height >= width)
            {
                var leftoverHoriz = Math.Abs(free.Width - height);
                var leftoverVert = Math.Abs(free.Height - width);
                var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                var longSideFit = Math.Max(leftoverHoriz, leftoverVert);
                if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                {
                    bestNode = new Rect(free.X, free.Y, height, width);
                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }
        }

        return bestNode;
    }

    private Rect FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
    {
        var bestNode = new Rect();
        bestLongSideFit = int.MaxValue;
        foreach (var free in _freeRectangles)
        {
            if (free.Width >= width && free.Height >= height)
            {
                var leftoverHoriz = Math.Abs(free.Width - width);
                var leftoverVert = Math.Abs(free.Height - height);
                var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                var longSideFit = Math.Max(leftoverHoriz, leftoverVert);
                if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                {
                    bestNode = new Rect(free.X, free.Y, width, height);
                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }

            if (_allowRotations && free.Width >= height && free.Height >= width)
            {
                var leftoverHoriz = Math.Abs(free.Width - height);
                var leftoverVert = Math.Abs(free.Height - width);
                var shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                var longSideFit = Math.Max(leftoverHoriz, leftoverVert);
                if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                {
                    bestNode = new Rect(free.X, free.Y, height, width);
                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }
        }

        return bestNode;
    }

    private Rect FindPositionForNewNodeBestAreaFit(int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
    {
        var bestNode = new Rect();
        bestAreaFit = int.MaxValue;
        foreach (var free in _freeRectangles)
        {
            var areaFit = (free.Width * free.Height) - (width * height);
            if (free.Width >= width && free.Height >= height)
            {
                var shortSideFit = Math.Min(Math.Abs(free.Width - width), Math.Abs(free.Height - height));
                if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                {
                    bestNode = new Rect(free.X, free.Y, width, height);
                    bestShortSideFit = shortSideFit;
                    bestAreaFit = areaFit;
                }
            }

            if (_allowRotations && free.Width >= height && free.Height >= width)
            {
                var shortSideFit = Math.Min(Math.Abs(free.Width - height), Math.Abs(free.Height - width));
                if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                {
                    bestNode = new Rect(free.X, free.Y, height, width);
                    bestShortSideFit = shortSideFit;
                    bestAreaFit = areaFit;
                }
            }
        }

        return bestNode;
    }

    private Rect FindPositionForNewNodeContactPoint(int width, int height, ref int bestContactScore)
    {
        var bestNode = new Rect();
        bestContactScore = -1;
        foreach (var free in _freeRectangles)
        {
            if (free.Width >= width && free.Height >= height)
            {
                var score = ContactPointScoreNode(free.X, free.Y, width, height);
                if (score > bestContactScore)
                {
                    bestNode = new Rect(free.X, free.Y, width, height);
                    bestContactScore = score;
                }
            }

            if (_allowRotations && free.Width >= height && free.Height >= width)
            {
                var score = ContactPointScoreNode(free.X, free.Y, height, width);
                if (score > bestContactScore)
                {
                    bestNode = new Rect(free.X, free.Y, height, width);
                    bestContactScore = score;
                }
            }
        }

        return bestNode;
    }

    private int CommonIntervalLength(int firstStart, int firstEnd, int secondStart, int secondEnd) =>
        firstEnd < secondStart || secondEnd < firstStart ? 0 : Math.Min(firstEnd, secondEnd) - Math.Max(firstStart, secondStart);

    private int ContactPointScoreNode(int x, int y, int width, int height)
    {
        var score = 0;
        if (x == 0 || x + width == _binWidth)
        {
            score += height;
        }

        if (y == 0 || y + height == _binHeight)
        {
            score += width;
        }

        foreach (var used in _usedRectangles)
        {
            if (used.X == x + width || used.X + used.Width == x)
            {
                score += CommonIntervalLength(used.Y, used.Y + used.Height, y, y + height);
            }

            if (used.Y == y + height || used.Y + used.Height == y)
            {
                score += CommonIntervalLength(used.X, used.X + used.Width, x, x + width);
            }
        }

        return score;
    }

    private bool SplitFreeNode(Rect freeNode, Rect usedNode)
    {
        if (usedNode.X >= freeNode.X + freeNode.Width || usedNode.X + usedNode.Width <= freeNode.X ||
            usedNode.Y >= freeNode.Y + freeNode.Height || usedNode.Y + usedNode.Height <= freeNode.Y)
        {
            return false;
        }

        if (usedNode.X < freeNode.X + freeNode.Width && usedNode.X + usedNode.Width > freeNode.X)
        {
            if (usedNode.Y > freeNode.Y && usedNode.Y < freeNode.Y + freeNode.Height)
            {
                _freeRectangles.Add(new Rect(freeNode.X, freeNode.Y, freeNode.Width, usedNode.Y - freeNode.Y));
            }

            if (usedNode.Y + usedNode.Height < freeNode.Y + freeNode.Height)
            {
                _freeRectangles.Add(new Rect(
                    freeNode.X,
                    usedNode.Y + usedNode.Height,
                    freeNode.Width,
                    freeNode.Y + freeNode.Height - usedNode.Y - usedNode.Height));
            }
        }

        if (usedNode.Y < freeNode.Y + freeNode.Height && usedNode.Y + usedNode.Height > freeNode.Y)
        {
            if (usedNode.X > freeNode.X && usedNode.X < freeNode.X + freeNode.Width)
            {
                _freeRectangles.Add(new Rect(freeNode.X, freeNode.Y, usedNode.X - freeNode.X, freeNode.Height));
            }

            if (usedNode.X + usedNode.Width < freeNode.X + freeNode.Width)
            {
                _freeRectangles.Add(new Rect(
                    usedNode.X + usedNode.Width,
                    freeNode.Y,
                    freeNode.X + freeNode.Width - usedNode.X - usedNode.Width,
                    freeNode.Height));
            }
        }

        return true;
    }

    private void PruneFreeList()
    {
        for (var i = 0; i < _freeRectangles.Count; ++i)
        {
            for (var j = i + 1; j < _freeRectangles.Count; ++j)
            {
                if (IsContainedIn(_freeRectangles[i], _freeRectangles[j]))
                {
                    _freeRectangles.RemoveAt(i);
                    --i;
                    break;
                }

                if (IsContainedIn(_freeRectangles[j], _freeRectangles[i]))
                {
                    _freeRectangles.RemoveAt(j);
                    --j;
                }
            }
        }
    }

    private static bool IsContainedIn(Rect a, Rect b) =>
        a.X >= b.X && a.Y >= b.Y && a.X + a.Width <= b.X + b.Width && a.Y + a.Height <= b.Y + b.Height;

    public readonly record struct Rect(int X, int Y, int Width, int Height);
}
