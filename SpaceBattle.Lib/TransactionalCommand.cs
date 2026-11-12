namespace SpaceBattle.Lib;

public class TransactionalCommand : ICommand
{
    private readonly ICommand _command;
    private readonly ITransactionManager _transactionManager;

    public TransactionalCommand(ICommand command, ITransactionManager transactionManager)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
        _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
    }

    public void Execute()
    {
        _transactionManager.BeginTransaction();
        try
        {
            _command.Execute();
            _transactionManager.Commit();
        }
        catch
        {
            _transactionManager.Rollback();
            throw;
        }
    }
}
