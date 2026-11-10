using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class SpatialGridTests
{
    [Fact]
    public void Constructor_InvalidCellSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SpatialGrid(0));
        Assert.Throws<ArgumentException>(() => new SpatialGrid(-10));
    }

    [Fact]
    public void Constructor_DefaultCellSize_IsFifty()
    {
        var grid = new SpatialGrid();
        Assert.Equal(SpatialGrid.DefaultCellSize, grid.CellSize);
    }

    [Fact]
    public void GetCell_NullPosition_ThrowsArgumentNullException()
    {
        var grid = new SpatialGrid(50);
        Assert.Throws<ArgumentNullException>(() => grid.GetCell(null!));
    }

    [Fact]
    public void GetCell_PositionDimensionsLessThanTwo_ThrowsArgumentException()
    {
        var grid = new SpatialGrid(50);
        Assert.Throws<ArgumentException>(() => grid.GetCell(new NVector(10)));
    }

    [Fact]
    public void GetCell_PositiveAndNegativeCoords_CalculatesCorrectCell()
    {
        var grid = new SpatialGrid(10);

        Assert.Equal(new GridPoint(0, 0), grid.GetCell(new NVector(5, 5)));
        Assert.Equal(new GridPoint(1, 2), grid.GetCell(new NVector(15, 25)));
        Assert.Equal(new GridPoint(-1, -1), grid.GetCell(new NVector(-5, -5)));
        Assert.Equal(new GridPoint(-2, 0), grid.GetCell(new NVector(-15, 5)));
    }

    [Fact]
    public void GetRayCells_NullArguments_ThrowsArgumentNullException()
    {
        var grid = new SpatialGrid(10);
        var vec = new NVector(0, 0);

        Assert.Throws<ArgumentNullException>(() => grid.GetRayCells(null!, vec));
        Assert.Throws<ArgumentNullException>(() => grid.GetRayCells(vec, null!));
    }

    [Fact]
    public void GetRayCells_InvalidVectorDimensions_ThrowsArgumentException()
    {
        var grid = new SpatialGrid(10);
        var vec1D = new NVector(0);
        var vec2D = new NVector(0, 0);

        Assert.Throws<ArgumentException>(() => grid.GetRayCells(vec1D, vec2D));
        Assert.Throws<ArgumentException>(() => grid.GetRayCells(vec2D, vec1D));
    }

    [Fact]
    public void GetRayCells_StationaryObject_ReturnsSingleCell()
    {
        var grid = new SpatialGrid(10);
        var cells = grid.GetRayCells(new NVector(5, 5), new NVector(0, 0)).ToList();

        Assert.Single(cells);
        Assert.Equal(new GridPoint(0, 0), cells[0]);
    }

    [Fact]
    public void GetRayCells_CrossingMultipleCells_ReturnsAllTraversedCells()
    {
        var grid = new SpatialGrid(10);
        var cells = grid.GetRayCells(new NVector(5, 5), new NVector(20, 20)).ToList();

        Assert.Contains(new GridPoint(0, 0), cells);
        Assert.Contains(new GridPoint(1, 1), cells);
        Assert.Contains(new GridPoint(2, 2), cells);
    }

    [Fact]
    public void Add_NullObject_ThrowsArgumentNullException()
    {
        var grid = new SpatialGrid(10);
        Assert.Throws<ArgumentNullException>(() => grid.Add(null!));
    }

    [Fact]
    public void Remove_NullObject_ThrowsArgumentNullException()
    {
        var grid = new SpatialGrid(10);
        Assert.Throws<ArgumentNullException>(() => grid.Remove(null!));
    }

    [Fact]
    public void GetCandidates_NullObject_ThrowsArgumentNullException()
    {
        var grid = new SpatialGrid(10);
        Assert.Throws<ArgumentNullException>(() => grid.GetCandidates(null!));
    }

    [Fact]
    public void AddAndGetCandidates_FindsNearbyObject_ExcludesSelfAndFarObjects()
    {
        var grid = new SpatialGrid(10);

        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(5, 5));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        var objNear = new Mock<IMovingObject>();
        objNear.SetupGet(o => o.Position).Returns(new NVector(7, 7));
        objNear.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        var objFar = new Mock<IMovingObject>();
        objFar.SetupGet(o => o.Position).Returns(new NVector(100, 100));
        objFar.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        grid.Add(obj1.Object);
        grid.Add(objNear.Object);
        grid.Add(objFar.Object);

        var candidates = grid.GetCandidates(obj1.Object);

        Assert.Contains(objNear.Object, candidates);
        Assert.DoesNotContain(obj1.Object, candidates);
        Assert.DoesNotContain(objFar.Object, candidates);
    }

    [Fact]
    public void GetCandidates_BoundaryAcrossAdjacentCells_FoundAsCandidate()
    {
        var grid = new SpatialGrid(50);

        // Object 1 at (49, 25) -> Cell (0, 0)
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(49, 25));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        // Object 2 at (51, 25) -> Cell (1, 0)
        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(51, 25));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        grid.Add(obj1.Object);
        grid.Add(obj2.Object);

        var candidates1 = grid.GetCandidates(obj1.Object);
        var candidates2 = grid.GetCandidates(obj2.Object);

        // Both objects must see each other despite being in different cells across the border
        Assert.Contains(obj2.Object, candidates1);
        Assert.Contains(obj1.Object, candidates2);
    }

    [Fact]
    public void Add_ExistingObject_UpdatesCellsCleanly()
    {
        var grid = new SpatialGrid(10);

        var obj = new Mock<IMovingObject>();
        obj.SetupGet(o => o.Position).Returns(new NVector(5, 5));
        obj.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        grid.Add(obj.Object);

        // Move obj far away to (105, 105)
        obj.SetupGet(o => o.Position).Returns(new NVector(105, 105));
        grid.Add(obj.Object);

        var otherAtOldLocation = new Mock<IMovingObject>();
        otherAtOldLocation.SetupGet(o => o.Position).Returns(new NVector(6, 6));
        otherAtOldLocation.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));
        grid.Add(otherAtOldLocation.Object);

        var oldCandidates = grid.GetCandidates(otherAtOldLocation.Object);
        Assert.DoesNotContain(obj.Object, oldCandidates);
    }

    [Fact]
    public void Remove_RemovesObjectFromCandidates()
    {
        var grid = new SpatialGrid(10);

        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(5, 5));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(6, 6));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        grid.Add(obj1.Object);
        grid.Add(obj2.Object);

        grid.Remove(obj2.Object);

        var candidates = grid.GetCandidates(obj1.Object);
        Assert.Empty(candidates);

        grid.Remove(obj2.Object);
    }
}
