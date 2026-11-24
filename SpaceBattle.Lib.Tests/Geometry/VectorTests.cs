namespace SpaceBattle.Lib.Tests;

public class VectorTests
{
    [Fact]
    public void TwoVectorAddOperationPositive()
    {
        var vector1 = new NVector(1, -1, 2);
        var vector2 = new NVector(-1, 1, -2);

        var res = vector1 + vector2;

        Assert.Equal(vector1.Size, res.Size);
        Assert.Equal(new NVector(0, 0, 0), res);
    }

    [Fact]
    public void AddingIncompatibleVectorsThrowsArgumentException()
    {
        var vector1 = new NVector(1, 2, 3);
        var vector2 = new NVector(-1, 2);

        Assert.Throws<ArgumentException>(() => vector1 + vector2);
        Assert.Throws<ArgumentException>(() => vector2 + vector1);
    }

    [Fact]
    public void TwoVectorsWithSameCoordsAreEqual()
    {
        var vector1 = new NVector(1, 2, 3);
        var vector2 = new NVector(1, 2, 3);

        bool res = vector1.Equals(vector2);

        Assert.True(res);
    }

    [Fact]
    public void TwoVectorsWithDifferentCoordsAreNotEqual()
    {
        var vector1 = new NVector(1, 2, 3);
        var vector2 = new NVector(1, 2, 2);

        bool res = vector1.Equals(vector2);

        Assert.False(res);
    }

    [Fact]
    public void VectorsOfDifferentSizesAreNotEqual()
    {
        var vector1 = new NVector(1, 2, 3);
        var vector2 = new NVector(1, 2);

        bool res = vector1.Equals(vector2);

        Assert.False(res);
    }

    [Fact]
    public void EqualityOperatorReturnsTrueIfVectorsAreEqual()
    {
        var vector1 = new NVector(1, 2, 3);
        var vector2 = new NVector(1, 2, 3);

        Assert.True(vector1 == vector2);
    }

    [Fact]
    public void EqualityOperatorReturnsFalseIfVectorsAreNotEqual()
    {
        var vector1 = new NVector(1, 2, 2);
        var vector2 = new NVector(1, 2, 3);

        Assert.True(vector1 != vector2);
    }

    [Fact]
    public void HashCodeTest()
    {
        var vector = new NVector(1, 2, 3);

        var exception = Record.Exception(() => vector.GetHashCode());

        Assert.Null(exception);
    }
}
