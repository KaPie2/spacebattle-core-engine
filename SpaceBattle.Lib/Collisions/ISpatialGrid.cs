namespace SpaceBattle.Lib;

public interface ISpatialGrid
{
    int CellSize { get; }
    GridPoint GetCell(NVector? position);
    IEnumerable<GridPoint> GetRayCells(NVector? start, NVector? velocity);
    void Add(IMovingObject? obj);
    void Remove(IMovingObject? obj);
    IReadOnlyCollection<IMovingObject> GetCandidates(IMovingObject? obj);
}
