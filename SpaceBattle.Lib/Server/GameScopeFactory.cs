using App;
using App.Scopes;

namespace SpaceBattle.Lib.Server;

public interface IGameScopeFactory
{
    object Create();
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
        var previous = Ioc.Resolve<object>("IoC.Scope.Current");
        try
        {
            Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();
            _registerDependencies();
            return scope;
        }
        catch
        {
            Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", previous).Execute();
            throw;
        }
        finally
        {
            Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", previous).Execute();
        }
    }
}
