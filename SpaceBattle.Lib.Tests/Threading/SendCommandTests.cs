using Moq;

namespace SpaceBattle.Lib.Tests;

public class SendCommandTests
{
    [Fact]
    public void SSendCommandTransfersCommandToReceiver()
    {
        var mockCommand = new Mock<ICommand>();
        var mockReceiver = new Mock<ICommandReceiver>();
        var sendCommand = new SendCommand(mockCommand.Object, mockReceiver.Object);

        sendCommand.Execute();

        mockReceiver.Verify(r => r.Receive(mockCommand.Object), Times.Once);
    }

    [Fact]
    public void SendCommandThrowsExceptionWhenReceiveFails()
    {
        var mockCommand = new Mock<ICommand>();
        var mockReceiver = new Mock<ICommandReceiver>();
        mockReceiver.Setup(r => r.Receive(It.IsAny<ICommand>()))
                    .Throws(new InvalidOperationException());
        var sendCommand = new SendCommand(mockCommand.Object, mockReceiver.Object);

        Assert.Throws<InvalidOperationException>(() => sendCommand.Execute());
    }
}
