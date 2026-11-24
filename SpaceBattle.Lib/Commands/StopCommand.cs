namespace SpaceBattle.Lib;

public class StopCommand : ICommand
{
    private readonly ICommandInjectable _injectableCommand;

    public StopCommand(ICommandInjectable injectableCommand)
    {
        _injectableCommand = injectableCommand;
    }

    public void Execute()
    {
        _injectableCommand.Inject(new EmptyCommand());
    }
}
