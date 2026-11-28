using SpaceBattle.Lib;
using System.Text.Json;
using Moq;
using SpaceBattle.Lib.Server;
//using SpaceBattle.Lib.Threading;

namespace SpaceBattle.Lib.Tests.Server;

public sealed class CommandSubmissionTests
{
    private static CommandRequestDto Request(string gameId = "game-1", long generation = 1) =>
        new(gameId, "Move", JsonDocument.Parse("{}").RootElement, generation, "cmd-1");

    [Fact]
    public void ValidatorRejectsMissingGame()
    {
        var result = new CommandRequestValidator().Validate(Request(""));
        Assert.Equal("GameId is required.", result);
    }

    [Fact]
    public void SubmitSendsFactoryCommandToSelectedGame()
    {
        var command = new Mock<ICommand>().Object;
        var receiver = new Mock<ICommandReceiver>();
        var factory = new Mock<IGameCommandFactory>();
        var registry = new Mock<IGameReceiverRegistry>();
        factory.Setup(x => x.Create(It.IsAny<CommandRequestDto>())).Returns(command);
        registry.Setup(x => x.TryGet("game-1", out It.Ref<ICommandReceiver>.IsAny)).Returns((string _, out ICommandReceiver value) => { value = receiver.Object; return true; });

        var response = new CommandSubmissionService(new CommandRequestValidator(), factory.Object, registry.Object).Submit(Request());

        Assert.True(response.Accepted);
        receiver.Verify(x => x.Receive(command), Times.Once);
    }

    [Fact]
    public void SubmitRejectsUnknownGame()
    {
        var registry = new Mock<IGameReceiverRegistry>();
        registry.Setup(x => x.TryGet(It.IsAny<string>(), out It.Ref<ICommandReceiver>.IsAny)).Returns(false);
        var service = new CommandSubmissionService(new CommandRequestValidator(), Mock.Of<IGameCommandFactory>(), registry.Object);

        var response = service.Submit(Request());

        Assert.False(response.Accepted);
        Assert.Equal("Unknown game.", response.Reason);
    }
}



