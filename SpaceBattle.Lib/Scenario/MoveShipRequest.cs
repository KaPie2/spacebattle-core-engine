namespace SpaceBattle.Lib;

public class MoveShipRequest
{
    public string GameId { get; set; } = string.Empty;
    public string ShipId { get; set; } = string.Empty;
    public string CommandType { get; set; } = "Ship.Move";

    public MoveShipRequest()
    {
    }

    public MoveShipRequest(string gameId, string shipId, string commandType = "Ship.Move")
    {
        GameId = gameId ?? throw new ArgumentNullException(nameof(gameId));
        ShipId = shipId ?? throw new ArgumentNullException(nameof(shipId));
        CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
    }
}
