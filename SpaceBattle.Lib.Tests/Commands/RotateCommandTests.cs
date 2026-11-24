using System;
using System.Reflection;
using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class RotateCommandTests
{
    [Fact]
    public void RotateCommandWorksCorrect()
    {
        var rotatingObject = new Mock<IRotatingObject>();
        rotatingObject.Setup(o => o.Angle).Returns(new Angle(new int[] { 1, 8 }));
        rotatingObject.Setup(o => o.AngleVelocity).Returns(new Angle(new int[] { 1, 8 }));

        var cmd = new RotateCommand(rotatingObject.Object);
        cmd.Execute();

        rotatingObject.VerifySet(o => o.Angle = new Angle(new int[] { 2, 8 }));
    }

    [Fact]
    public void ThrowsExceptionIfAngleIsNull()
    {
        var rotatingObject = new Mock<IRotatingObject>();
        rotatingObject.Setup(o => o.Angle).Returns((Angle)null);
        rotatingObject.Setup(o => o.AngleVelocity).Returns(new Angle(new int[] { 1, 8 }));

        var cmd = new RotateCommand(rotatingObject.Object);

        Assert.Throws<ArgumentNullException>(() => cmd.Execute());
    }

    [Fact]
    public void ThrowsExceptionIfAngleVelocityIsNull()
    {
        var rotatingObject = new Mock<IRotatingObject>();
        rotatingObject.Setup(o => o.Angle).Returns(new Angle(new int[] { 1, 8 }));
        rotatingObject.Setup(o => o.AngleVelocity).Returns((Angle)null);

        var cmd = new RotateCommand(rotatingObject.Object);

        Assert.Throws<ArgumentNullException>(() => cmd.Execute());
    }

    [Fact]
    public void ThrowsExceptionWhenUnableToChangeAngle()
    {
        var vector = new Mock<IRotatingObject>();

        vector.Setup(x => x.Angle).Returns(new Angle(new int[] { 1, 8 }));
        vector.Setup(x => x.AngleVelocity).Returns(new Angle(new int[] { 4, 8 }));

        vector.SetupSet(x => x.Angle = It.IsAny<Angle>()).Throws<InvalidOperationException>();

        var moveCommand = new RotateCommand(vector.Object);

        Assert.Throws<InvalidOperationException>(() => moveCommand.Execute());
    }
}