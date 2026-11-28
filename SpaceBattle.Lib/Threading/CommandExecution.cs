namespace SpaceBattle.Lib;

internal static class CommandExecution
{
    internal static void Execute(ICommand command, Func<ICommand, Exception, ICommand> exceptionHandler)
    {
        try
        {
            command.Execute();
        }
        catch (Exception exception)
        {
            exceptionHandler(command, exception).Execute();
        }
    }
}
