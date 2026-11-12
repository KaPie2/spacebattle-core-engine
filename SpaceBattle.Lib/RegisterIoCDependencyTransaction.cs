using System.Runtime.CompilerServices;
using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyTransaction : ICommand
{
    public void Execute()
    {
        var scopedManagers = new ConditionalWeakTable<object, ITransactionManager>();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Transactions.Manager",
            (object[] args) =>
            {
                var currentScope = Ioc.Resolve<object>("IoC.Scope.Current");
                return scopedManagers.GetValue(currentScope, _ => new TransactionManager());
            }
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Objects.Transactional",
            (object[] args) =>
            {
                var target = (IUObject)args[0];
                var manager = Ioc.Resolve<ITransactionManager>("Transactions.Manager");
                return manager.CreateTransactionalObject(target);
            }
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
