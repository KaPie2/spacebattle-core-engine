using System;
using App;

namespace SpaceBattle.Lib;
public class RepeatedCommand : ICommand
{
    private readonly ICommand _innerCommand;

    public ICommand InnerCommand => _innerCommand;

    public RepeatedCommand(ICommand innerCommand)
    {
        _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
    }

    public void Execute()
    {
        _innerCommand.Execute();
    }
}

public class RepeatOnceCommand : ICommand
{
    private readonly ICommand _targetCommand;
    private readonly ICommandReceiver _receiver;

    public ICommand TargetCommand => _targetCommand;
    public ICommandReceiver Receiver => _receiver;

    public RepeatOnceCommand(ICommand targetCommand, ICommandReceiver receiver)
    {
        _targetCommand = targetCommand ?? throw new ArgumentNullException(nameof(targetCommand));
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
    }

    public void Execute()
    {
        var wrappedCommand = new RepeatedCommand(_targetCommand);
        Ioc.Resolve<ICommand>("Commands.Send", wrappedCommand, _receiver).Execute();
    }
}