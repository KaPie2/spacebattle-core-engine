using App;

namespace SpaceBattle.Lib;

public class CreateMacroCommandStrategy
{
    private readonly string _commandSpec;
    public CreateMacroCommandStrategy(string commandSpec)
    {
        _commandSpec = commandSpec;
    }
    public ICommand Resolve(object[] args)
    {
        var commandNames = Ioc.Resolve<string[]>($"Specs.{_commandSpec}");
        var commands = commandNames.Select(name => (ICommand)Ioc.Resolve<object>(name, args)).ToArray();

        return new MacroCommand(commands);
    }
}
