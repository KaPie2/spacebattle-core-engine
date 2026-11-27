using System.Collections.Concurrent;
using Moq;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class ShipScenarioAgentTests
{
    [Fact]
    public void MoveShipRequest_InitializesProperties()
    {
        var req1 = new MoveShipRequest();
        Assert.Empty(req1.GameId);
        Assert.Empty(req1.ShipId);
        Assert.Equal("Ship.Move", req1.CommandType);

        var req2 = new MoveShipRequest("game-1", "ship-99", "Custom.Command");
        Assert.Equal("game-1", req2.GameId);
        Assert.Equal("ship-99", req2.ShipId);
        Assert.Equal("Custom.Command", req2.CommandType);

        Assert.Throws<ArgumentNullException>(() => new MoveShipRequest(null!, "ship-1"));
        Assert.Throws<ArgumentNullException>(() => new MoveShipRequest("game-1", null!));
        Assert.Throws<ArgumentNullException>(() => new MoveShipRequest("game-1", "ship-1", null!));
    }

    [Fact]
    public void ExecuteDirect_SufficientFuel_ReturnsSuccessWithEvents()
    {
        var agent = new ShipScenarioAgent();
        var ship = new Ship(
            "ship-1",
            new NVector(10, 20),
            new NVector(1, 2),
            new Angle(new int[] { 0, 360 }),
            50,
            10
        );
        var request = new MoveShipRequest("game-1", "ship-1");

        var result = agent.ExecuteDirect(ship, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(new NVector(11, 22), result.Position);
        Assert.Equal(40, result.FuelLevel);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains("успешно", result.Message);
    }

    [Fact]
    public void ExecuteDirect_InsufficientFuel_ReturnsFailureWithoutEvents()
    {
        var agent = new ShipScenarioAgent();
        var ship = new Ship(
            "ship-1",
            new NVector(10, 20),
            new NVector(1, 2),
            new Angle(new int[] { 0, 360 }),
            5,
            10
        );
        var request = new MoveShipRequest("game-1", "ship-1");

        var result = agent.ExecuteDirect(ship, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(new NVector(10, 20), result.Position);
        Assert.Equal(5, result.FuelLevel);
        Assert.Empty(result.Events);
        Assert.Contains("Недостаточно топлива", result.Message);
    }

    [Fact]
    public void ExecuteDirect_ThrowsOnNullArguments()
    {
        var agent = new ShipScenarioAgent();
        var ship = new Ship("ship-1", new NVector(0, 0), new NVector(1, 1), new Angle(new int[] { 0, 360 }), 10, 5);
        var request = new MoveShipRequest("game-1", "ship-1");

        Assert.Throws<ArgumentNullException>(() => agent.ExecuteDirect(null!, request));
        Assert.Throws<ArgumentNullException>(() => agent.ExecuteDirect(ship, null!));
    }

    [Fact]
    public void SendAndProcess_EnqueuesCommandToReceiver()
    {
        var agent = new ShipScenarioAgent();
        var receiverMock = new Mock<ICommandReceiver>();
        var ship = new Ship("ship-1", new NVector(0, 0), new NVector(1, 1), new Angle(new int[] { 0, 360 }), 10, 5);
        var request = new MoveShipRequest("game-1", "ship-1");

        ICommand? receivedCmd = null;
        receiverMock.Setup(r => r.Receive(It.IsAny<ICommand>()))
            .Callback<ICommand>(cmd => receivedCmd = cmd);

        var result = agent.SendAndProcess(receiverMock.Object, ship, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(receivedCmd);
        Assert.IsType<MoveShipCommand>(receivedCmd);
    }

    [Fact]
    public void SendAndProcess_WhenReceiverThrows_ReturnsFailure()
    {
        var agent = new ShipScenarioAgent();
        var receiverMock = new Mock<ICommandReceiver>();
        receiverMock.Setup(r => r.Receive(It.IsAny<ICommand>()))
            .Throws(new InvalidOperationException("Queue is full"));

        var ship = new Ship("ship-1", new NVector(0, 0), new NVector(1, 1), new Angle(new int[] { 0, 360 }), 10, 5);
        var request = new MoveShipRequest("game-1", "ship-1");

        var result = agent.SendAndProcess(receiverMock.Object, ship, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Queue is full", result.Message);
    }

    [Fact]
    public void SendAndProcess_ThrowsOnNullArguments()
    {
        var agent = new ShipScenarioAgent();
        var receiver = new Mock<ICommandReceiver>().Object;
        var ship = new Ship("ship-1", new NVector(0, 0), new NVector(1, 1), new Angle(new int[] { 0, 360 }), 10, 5);
        var request = new MoveShipRequest("game-1", "ship-1");

        Assert.Throws<ArgumentNullException>(() => agent.SendAndProcess(null!, ship, request));
        Assert.Throws<ArgumentNullException>(() => agent.SendAndProcess(receiver, null!, request));
        Assert.Throws<ArgumentNullException>(() => agent.SendAndProcess(receiver, ship, null!));
    }

    [Fact]
    public void EndToEnd_WorkerExecution_Snapshot_Migration_Continuation()
    {
        // Сквозной интеграционный сценарий:
        // 1. Корабль на воркере выполняет команду движения
        // 2. Формируется снимок состояния (JSON)
        // 3. Состояние восстанавливается на новом экземпляре
        // 4. Следующая команда применяется к восстановленному состоянию
        // 5. Попытка хода при исчерпании топлива корректно отклоняется

        var queue = new BlockingCollection<ICommand>();
        var workerThread = new GameThread(queue, (_, _) => new EmptyCommand());
        workerThread.Start();

        var agent = new ShipScenarioAgent();
        var initialShip = new Ship(
            "flagship-01",
            new NVector(10, 20),
            new NVector(2, 3),
            new Angle(new int[] { 0, 360 }),
            35,
            15
        );

        // Ход 1 через воркер
        var req1 = new MoveShipRequest("game-100", "flagship-01");
        agent.SendAndProcess(workerThread, initialShip, req1);

        // Дожидаемся обработки очереди воркером
        Thread.Sleep(80);

        Assert.Equal(new NVector(12, 23), initialShip.Position);
        Assert.Equal(20, initialShip.FuelLevel);
        Assert.Equal(2, initialShip.DomainEvents.Count);

        // Снимок состояния и сохранение в JSON
        var jsonSnapshot = initialShip.ToJson();
        Assert.Contains("flagship-01", jsonSnapshot);

        // Восстановление корабля из JSON (имитация перезапуска сервера или миграции)
        var restoredShip = Ship.FromJson(jsonSnapshot);
        Assert.Equal(new NVector(12, 23), restoredShip.Position);
        Assert.Equal(20, restoredShip.FuelLevel);

        // Ход 2 на восстановленном объекте
        var req2 = new MoveShipRequest("game-100", "flagship-01");
        agent.SendAndProcess(workerThread, restoredShip, req2);
        Thread.Sleep(80);

        Assert.Equal(new NVector(14, 26), restoredShip.Position);
        Assert.Equal(5, restoredShip.FuelLevel);
        Assert.Equal(2, restoredShip.DomainEvents.Count);

        // Ход 3: топлива (5) меньше, чем расход (15) -> отказ
        var req3 = new MoveShipRequest("game-100", "flagship-01");
        var directResult = agent.ExecuteDirect(restoredShip, req3);

        Assert.False(directResult.IsSuccess);
        Assert.Equal(new NVector(14, 26), restoredShip.Position);
        Assert.Equal(5, restoredShip.FuelLevel);

        workerThread.StopImmediately();
        workerThread.Join();
    }
}
