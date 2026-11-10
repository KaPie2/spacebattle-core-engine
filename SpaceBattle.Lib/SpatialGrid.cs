namespace SpaceBattle.Lib;

public class SpatialGrid : ISpatialGrid
{
    public const int DefaultCellSize = 50;
    private const int MinVectorDimensions = 2;
    private const int NeighborOffsetMin = -1;
    private const int NeighborOffsetMax = 1;

    public int CellSize { get; }
    private readonly Dictionary<GridPoint, HashSet<IMovingObject>> _grid = new();
    private readonly Dictionary<IMovingObject, HashSet<GridPoint>> _objectCells = new(ReferenceEqualityComparer.Instance);

    public SpatialGrid(int cellSize = DefaultCellSize)
    {
        if (cellSize <= 0)
        {
            throw new ArgumentException("Cell size must be positive", nameof(cellSize));
        }

        CellSize = cellSize;
    }

    public GridPoint GetCell(NVector? position)
    {
        if (position is null)
        {
            throw new ArgumentNullException(nameof(position));
        }
        if (position.Size < MinVectorDimensions)
        {
            throw new ArgumentException("Position must have at least 2 dimensions", nameof(position));
        }

        int cellX = (int)Math.Floor((double)position[0] / CellSize);
        int cellY = (int)Math.Floor((double)position[1] / CellSize);
        return new GridPoint(cellX, cellY);
    }

    public IEnumerable<GridPoint> GetRayCells(NVector? start, NVector? velocity)
    {
        if (start is null)
        {
            throw new ArgumentNullException(nameof(start));
        }
        if (velocity is null)
        {
            throw new ArgumentNullException(nameof(velocity));
        }
        if (start.Size < MinVectorDimensions || velocity.Size < MinVectorDimensions)
        {
            throw new ArgumentException("Vectors must have at least 2 dimensions");
        }

        var x0 = start[0];
        var y0 = start[1];
        var x1 = x0 + velocity[0];
        var y1 = y0 + velocity[1];

        var startCellX = (int)Math.Floor((double)x0 / CellSize);
        var startCellY = (int)Math.Floor((double)y0 / CellSize);
        var endCellX = (int)Math.Floor((double)x1 / CellSize);
        var endCellY = (int)Math.Floor((double)y1 / CellSize);

        var cells = new HashSet<GridPoint>
        {
            new(startCellX, startCellY),
            new(endCellX, endCellY)
        };

        int dx = Math.Abs(endCellX - startCellX);
        int dy = Math.Abs(endCellY - startCellY);
        int sx = startCellX < endCellX ? 1 : -1;
        int sy = startCellY < endCellY ? 1 : -1;
        int err = dx - dy;

        int curX = startCellX;
        int curY = startCellY;

        while (curX != endCellX || curY != endCellY)
        {
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                curX += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                curY += sy;
            }
            cells.Add(new GridPoint(curX, curY));
        }

        return cells;
    }

    public void Add(IMovingObject? obj)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        Remove(obj);

        var cells = GetRayCells(obj.Position, obj.Velocity).ToHashSet();
        _objectCells[obj] = cells;

        foreach (var cell in cells)
        {
            if (!_grid.TryGetValue(cell, out var list))
            {
                list = new HashSet<IMovingObject>(ReferenceEqualityComparer.Instance);
                _grid[cell] = list;
            }
            list.Add(obj);
        }
    }

    public void Remove(IMovingObject? obj)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if (_objectCells.TryGetValue(obj, out var cells))
        {
            foreach (var cell in cells)
            {
                if (_grid.TryGetValue(cell, out var list))
                {
                    list.Remove(obj);
                    if (list.Count == 0)
                    {
                        _grid.Remove(cell);
                    }
                }
            }
            _objectCells.Remove(obj);
        }
    }

    public IReadOnlyCollection<IMovingObject> GetCandidates(IMovingObject? obj)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var candidates = new HashSet<IMovingObject>(ReferenceEqualityComparer.Instance);
        var cells = GetRayCells(obj.Position, obj.Velocity);

        foreach (var cell in cells)
        {
            for (int dx = NeighborOffsetMin; dx <= NeighborOffsetMax; dx++)
            {
                for (int dy = NeighborOffsetMin; dy <= NeighborOffsetMax; dy++)
                {
                    var neighbor = new GridPoint(cell.X + dx, cell.Y + dy);
                    if (_grid.TryGetValue(neighbor, out var list))
                    {
                        foreach (var other in list)
                        {
                            if (!ReferenceEquals(other, obj))
                            {
                                candidates.Add(other);
                            }
                        }
                    }
                }
            }
        }

        return candidates;
    }
}
