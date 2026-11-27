using System;

namespace SpaceBattle.Lib;


public class RegisterExceptionRouteCommand : ICommand, App.ICommand
{
    private readonly ExceptionHandlerRouter _router;
    private readonly Type _commandType;
    private readonly Type _exceptionType;
    private readonly Func<ICommand, Exception, ICommand> _handlerFactory;

    public RegisterExceptionRouteCommand(
        ExceptionHandlerRouter router,
        Type commandType,
        Type exceptionType,
        Func<ICommand, Exception, ICommand> handlerFactory)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _commandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
        _exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
        _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
    }

    public void Execute()
    {
        _router.Register(_commandType, _exceptionType, _handlerFactory);
    }
}