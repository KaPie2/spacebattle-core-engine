using System;
using Moq;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class MoveCommandTests
{
    [Fact]
    public void MoveCommand_MovesObjectCorrectly()
    {
        var movingObjectMock = new Mock<IMovingObject>();
        movingObjectMock.SetupGet(m => m.Position).Returns(new NVector(12, 5));
        movingObjectMock.SetupGet(m => m.Velocity).Returns(new NVector(-4, 1));

        var moveCommand = new MoveCommand(movingObjectMock.Object);

        moveCommand.Execute();

        movingObjectMock.VerifySet(m => m.Position = new NVector(8, 6), Times.Once);
    }

    [Fact]
    public void MoveCommand_ThrowsException_WhenPositionIsUnreadable()
    {
        var movingObjectMock = new Mock<IMovingObject>();
        movingObjectMock.SetupGet(m => m.Position).Throws<Exception>();

        var moveCommand = new MoveCommand(movingObjectMock.Object);

        Assert.Throws<Exception>(() => moveCommand.Execute());
    }

    [Fact]
    public void MoveCommand_ThrowsException_WhenVelocityIsUnreadable()
    {
        var movingObjectMock = new Mock<IMovingObject>();
        movingObjectMock.SetupGet(m => m.Position).Returns(new NVector(12, 5));
        movingObjectMock.SetupGet(m => m.Velocity).Throws<Exception>();

        var moveCommand = new MoveCommand(movingObjectMock.Object);

        Assert.Throws<Exception>(() => moveCommand.Execute());
    }

    [Fact]
    public void MoveCommand_ThrowsException_WhenPositionIsUnwritable()
    {
        var movingObjectMock = new Mock<IMovingObject>();
        movingObjectMock.SetupGet(m => m.Position).Returns(new NVector(12, 5));
        movingObjectMock.SetupGet(m => m.Velocity).Returns(new NVector(-4, 1));

        movingObjectMock.SetupSet(m => m.Position = It.IsAny<NVector>()).Throws<Exception>();

        var moveCommand = new MoveCommand(movingObjectMock.Object);

        Assert.Throws<Exception>(() => moveCommand.Execute());
    }
}
