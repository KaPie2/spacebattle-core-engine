using System;
using System.Reflection;
using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class AngleTests
{
    [Fact]
    public void AddingAnglesReturnsCorrectAnswer()
    {
        var angle1 = new Angle(new int[] { 5, 8 });
        var angle2 = new Angle(new int[] { 7, 8 });

        Angle res = angle1 + angle2;
        Assert.Equal(new int[] { 4, 8 }, new int[] { res.Degrees, res.Denominator });
    }

    [Fact]
    public void EqualsReturnsTrueIfAnglesAreEqual()
    {
        var angle1 = new Angle(new int[] { 15, 8 });
        var angle2 = new Angle(new int[] { 23, 8 });

        Assert.True(angle1.Equals(angle2));
    }

    [Fact]
    public void EqualsReturnsFalseIfAnglesNotEqual()
    {
        var angle1 = new Angle(new int[] { 1, 8 });
        var angle2 = new Angle(new int[] { 2, 8 });

        Assert.False(angle1.Equals(angle2));
    }

    [Fact]
    public void EqualsOperatorReturnsTrueIfAnglesNotEqual()
    {
        var angle1 = new Angle(new int[] { 1, 8 });
        var angle2 = new Angle(new int[] { 2, 8 });

        Assert.True(angle1 != angle2);
    }

    [Fact]
    public void EqualsOperatorReturnsTrueIfAnglesEqual()
    {
        var angle1 = new Angle(new int[] { 15, 8 });
        var angle2 = new Angle(new int[] { 23, 8 });

        Assert.True(angle1 == angle2);
    }
}