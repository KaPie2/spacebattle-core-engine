using System.Collections.Concurrent;
using App;
using App.Scopes;

namespace SpaceBattle.Lib.Tests;

public class GameThreadIntegrationTests
{
    public GameThreadIntegrationTests()
    {
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void GameThread_EndToEnd_ExecutesGeneratedAdapterAndAutoInjectedMoveCommand()
    {
        // arrange (регистрация стратегии скорости, адаптера и команды движения)
        var currentScope = Ioc.Resolve<object>("IoC.Scope.Current");
        var queue = new BlockingCollection<ICommand>();

        // регистрация стратегии скорости до адаптера
        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Velocity.Get", (object[] args) =>
        {
            return new NVector(2, -5);
        }).Execute();

        // регистрация фабрики адаптера для imovingobject
        new RegisterAdapterCommand<IMovingObject>().Execute();

        // регистрация команды автоматического внедрения зависимостей для movecommand
        new RegisterAutoInjectCommand("Commands.Move", typeof(MoveCommand)).Execute();

        // регистрация зависимостей чтения и записи позиции объекта в скоупе
        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Position.Get", (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];
            return dict["Position"];
        }).Execute();

        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Position.Set", (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];
            var newPos = (NVector)args[1];
            return new ActionCommand(() => dict["Position"] = newPos);
        }).Execute();

        var gameObject = new Dictionary<string, object>
        {
            ["Position"] = new NVector(10, 20)
        };

        var moveCompletedEvent = new ManualResetEvent(false);
        Exception? caughtEx = null;

        Ioc.Resolve<App.ICommand>("IoC.Register", "Exception.Handler", (object[] args) =>
        {
            caughtEx = (Exception)args[1];
            return new ActionCommand(() => { });
        }).Execute();

        // act (создание команды через ioc, постановка в очередь потока и запуск)
        var thread = new GameThread(
            queue,
            (command, exception) => Ioc.Resolve<ICommand>("Exception.Handler", command, exception),
            currentScope);
        var moveCmd = Ioc.Resolve<ICommand>("Commands.Move", gameObject);

        queue.Add(moveCmd);
        queue.Add(new ActionCommand(() => moveCompletedEvent.Set()));

        thread.Start();

        // assert (ожидание выполнения команды движения и проверка новых координат)
        bool completed = moveCompletedEvent.WaitOne(TimeSpan.FromSeconds(3));
        thread.StopImmediately();
        thread.Join();

        if (caughtEx != null)
        {
            Assert.Fail($"MoveCommand threw exception: {caughtEx.GetType().Name} - {caughtEx.Message}\n{caughtEx.StackTrace}");
        }

        Assert.True(completed, "Команда движения не была выполнена игровым потоком.");
        var pos = (NVector)gameObject["Position"];
        Assert.Equal(new NVector(12, 15), pos);
    }
}
