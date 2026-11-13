using System;
using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyExceptionHandler : ICommand
{
    public void Execute()
    {
        var router = new ExceptionHandlerRouter();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Exception.Handler",
            (Func<object[], object>)(args =>
            {
                var command = (ICommand)args[0];
                var exception = (Exception)args[1];
                return router.Handle(command, exception);
            })
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Exception.Handler.Register",
            (Func<object[], object>)(args =>
            {
                var commandType = (Type)args[0];
                var exceptionType = (Type)args[1];
                var handlerFactory = (Func<ICommand, Exception, ICommand>)args[2];
                return new RegisterExceptionRouteCommand(router, commandType, exceptionType, handlerFactory);
            })
        ).Execute();
    }
}