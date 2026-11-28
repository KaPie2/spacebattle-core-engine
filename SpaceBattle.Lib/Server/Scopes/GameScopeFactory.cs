using App;
using App.Scopes;

namespace SpaceBattle.Lib.Server;

public interface IGameScopeFactory
{
    object Create();
    void Execute(object gameScope, ICommand command);
}

public sealed class GameScopeFactory : IGameScopeFactory
{
    private readonly Action _registerDependencies;

    public GameScopeFactory(Action registerDependencies)
    {
        _registerDependencies = registerDependencies ?? throw new ArgumentNullException(nameof(registerDependencies));
    }

    public object Create()
    {
        new InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        var previousScope = Ioc.Resolve<object>("IoC.Scope.Current");

        try
        {
            SetCurrent(scope);
            _registerDependencies();
            return scope;
        }
        finally
        {
            SetCurrent(previousScope);
        }
    }

    public void Execute(object gameScope, ICommand command)
    {
        ArgumentNullException.ThrowIfNull(gameScope);
        ArgumentNullException.ThrowIfNull(command);

        var previousScope = Ioc.Resolve<object>("IoC.Scope.Current");
        try
        {
            SetCurrent(gameScope);
            command.Execute();
        }
        finally
        {
            SetCurrent(previousScope);
        }
    }

    private static void SetCurrent(object scope)
    {
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();
    }
}
