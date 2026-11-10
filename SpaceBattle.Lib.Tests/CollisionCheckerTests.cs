using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class CollisionCheckerTests
{
    [Fact]
    public void Constructor_NegativeDistance_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CollisionChecker(-1.0));
    }

    [Fact]
    public void HaveCollided_NullObjects_ThrowsArgumentNullException()
    {
        var checker = new CollisionChecker();
        var obj = new Mock<IMovingObject>();

        Assert.Throws<ArgumentNullException>(() => checker.HaveCollided(null!, obj.Object));
        Assert.Throws<ArgumentNullException>(() => checker.HaveCollided(obj.Object, null!));
    }

    [Fact]
    public void HaveCollided_NullProperties_ThrowsArgumentNullException()
    {
        var checker = new CollisionChecker();

        var objWithNullPos = new Mock<IMovingObject>();
        objWithNullPos.SetupGet(o => o.Position).Returns((NVector)null!);
        objWithNullPos.SetupGet(o => o.Velocity).Returns(new NVector(1, 1));

        var validObj = new Mock<IMovingObject>();
        validObj.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        validObj.SetupGet(o => o.Velocity).Returns(new NVector(1, 1));

        Assert.Throws<ArgumentNullException>(() => checker.HaveCollided(objWithNullPos.Object, validObj.Object));

        var objWithNullVel = new Mock<IMovingObject>();
        objWithNullVel.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        objWithNullVel.SetupGet(o => o.Velocity).Returns((NVector)null!);

        Assert.Throws<ArgumentNullException>(() => checker.HaveCollided(objWithNullVel.Object, validObj.Object));

        Assert.Throws<ArgumentNullException>(() => checker.HaveCollided(validObj.Object, objWithNullPos.Object));
        Assert.Throws<ArgumentNullException>(() => checker.HaveCollided(validObj.Object, objWithNullVel.Object));
    }

    [Fact]
    public void HaveCollided_VectorDimensionsLessThanTwo_ThrowsArgumentException()
    {
        var checker = new CollisionChecker();

        var obj1D = new Mock<IMovingObject>();
        obj1D.SetupGet(o => o.Position).Returns(new NVector(1));
        obj1D.SetupGet(o => o.Velocity).Returns(new NVector(1));

        var obj2D = new Mock<IMovingObject>();
        obj2D.SetupGet(o => o.Position).Returns(new NVector(1, 2));
        obj2D.SetupGet(o => o.Velocity).Returns(new NVector(1, 2));

        Assert.Throws<ArgumentException>(() => checker.HaveCollided(obj1D.Object, obj2D.Object));
        Assert.Throws<ArgumentException>(() => checker.HaveCollided(obj2D.Object, obj1D.Object));
    }

    [Fact]
    public void HaveCollided_MovingTowardsEachOther_CollideAtMidpoint_ReturnsTrue()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(2, 2));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(2, 0));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(-2, 2));

        var checker = new CollisionChecker(1.0);

        Assert.True(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_CrossingTrajectoriesDifferentTimes_ReturnsFalse()
    {
        // Trajectories cross geometrically, but obj1 arrives early, obj2 arrives late
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(2, 0)); // At t=0.5: (1, 0)

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(1, -50));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(0, 2)); // At t=0.5: (1, -49)

        var checker = new CollisionChecker(1.0);

        Assert.False(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_MovingAwayFromEachOther_ReturnsFalse()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(10, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(5, 0));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(-5, 0));

        var checker = new CollisionChecker(1.0);

        Assert.False(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_StationaryObjectsWithinDistance_ReturnsTrue()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(1, 0));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        var checker = new CollisionChecker(1.0);

        Assert.True(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_StationaryObjectsOutsideDistance_ReturnsFalse()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(10, 0));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(0, 0));

        var checker = new CollisionChecker(1.0);

        Assert.False(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_ParallelTrajectoriesCloseDistance_ReturnsTrue()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(5, 0));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(0, 1));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(5, 0));

        var checker = new CollisionChecker(1.5);

        Assert.True(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_ParallelTrajectoriesFarDistance_ReturnsFalse()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(5, 0));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(0, 10));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(5, 0));

        var checker = new CollisionChecker(1.0);

        Assert.False(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_CollisionAtTickStart_ReturnsTrue()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(10, 0));

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(0, 10));

        var checker = new CollisionChecker(1.0);

        Assert.True(checker.HaveCollided(obj1.Object, obj2.Object));
    }

    [Fact]
    public void HaveCollided_CollisionAtTickEnd_ReturnsTrue()
    {
        var obj1 = new Mock<IMovingObject>();
        obj1.SetupGet(o => o.Position).Returns(new NVector(0, 0));
        obj1.SetupGet(o => o.Velocity).Returns(new NVector(10, 0)); // Ends at (10, 0)

        var obj2 = new Mock<IMovingObject>();
        obj2.SetupGet(o => o.Position).Returns(new NVector(10, 10));
        obj2.SetupGet(o => o.Velocity).Returns(new NVector(0, -10)); // Ends at (10, 0)

        var checker = new CollisionChecker(1.0);

        Assert.True(checker.HaveCollided(obj1.Object, obj2.Object));
    }
}
