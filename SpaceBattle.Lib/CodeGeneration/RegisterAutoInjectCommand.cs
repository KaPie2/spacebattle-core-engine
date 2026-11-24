using App;

namespace SpaceBattle.Lib;

public class RegisterAutoInjectCommand : ICommand
{
    private readonly string _commandName;
    private readonly Type _commandType;

    public RegisterAutoInjectCommand(string commandName, Type commandType)
    {
        _commandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        _commandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
    }

    public void Execute()
    {
        var factory = AutoInjectedCommandFactory.Build(_commandType);
        Ioc.Resolve<App.ICommand>("IoC.Register", _commandName, factory).Execute();
    }
}
