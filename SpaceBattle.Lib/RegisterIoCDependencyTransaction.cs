using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyTransaction : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Transactions.Manager",
            (object[] args) => new TransactionManager()
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.Transactional",
            (object[] args) =>
            {
                var command = (ICommand)args[0];
                var manager = args.Length > 1
                    ? (ITransactionManager)args[1]
                    : Ioc.Resolve<ITransactionManager>("Transactions.Manager");

                return new TransactionalCommand(command, manager);
            }
        ).Execute();
    }
}
