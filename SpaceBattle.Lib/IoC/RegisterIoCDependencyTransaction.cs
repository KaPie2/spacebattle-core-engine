using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyTransaction : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Transaction.Manager",
            (object[] args) => new TransactionManager()
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Transactions.Manager",
            (object[] args) => new TransactionManager()
        ).Execute();
    }
}
