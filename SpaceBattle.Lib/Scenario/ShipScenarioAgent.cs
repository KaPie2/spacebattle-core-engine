namespace SpaceBattle.Lib;

public class ShipScenarioAgent
{
    // Первый проход: прямая проверка агрегата «Корабль» без серверного контекста
    public ScenarioResult ExecuteDirect(Ship ship, MoveShipRequest request)
    {
        if (ship == null)
        {
            throw new ArgumentNullException(nameof(ship));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (ship.FuelLevel < ship.FuelConsumption)
        {
            return new ScenarioResult
            {
                IsSuccess = false,
                Message = "Отказ: Недостаточно топлива для перемещения корабля.",
                Position = ship.Position,
                FuelLevel = ship.FuelLevel,
                Events = Array.Empty<IDomainEvent>()
            };
        }

        ship.Move();

        return new ScenarioResult
        {
            IsSuccess = true,
            Message = "Команда успешно выполнена.",
            Position = ship.Position,
            FuelLevel = ship.FuelLevel,
            Events = ship.DomainEvents.ToList()
        };
    }

    // Итоговый проход: проверка пути от запроса через получатель команд (ICommandReceiver) до изменения состояния и события
    public ScenarioResult SendAndProcess(ICommandReceiver receiver, Ship ship, MoveShipRequest request)
    {
        if (receiver == null)
        {
            throw new ArgumentNullException(nameof(receiver));
        }

        if (ship == null)
        {
            throw new ArgumentNullException(nameof(ship));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var initialEventsCount = ship.DomainEvents.Count;

        try
        {
            var command = new MoveShipCommand(ship);
            receiver.Receive(command);

            return new ScenarioResult
            {
                IsSuccess = true,
                Message = "Команда поставлена в очередь и принята к исполнению.",
                Position = ship.Position,
                FuelLevel = ship.FuelLevel,
                Events = ship.DomainEvents.Skip(initialEventsCount).ToList()
            };
        }
        catch (Exception ex)
        {
            return new ScenarioResult
            {
                IsSuccess = false,
                Message = $"Отказ при отправке команды: {ex.Message}",
                Position = ship.Position,
                FuelLevel = ship.FuelLevel,
                Events = Array.Empty<IDomainEvent>()
            };
        }
    }
}
