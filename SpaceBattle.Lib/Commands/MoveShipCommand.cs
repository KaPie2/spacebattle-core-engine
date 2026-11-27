namespace SpaceBattle.Lib;

public class MoveShipCommand : ICommand
{
    private readonly Ship _ship;

    public MoveShipCommand(Ship ship)
    {
        _ship = ship ?? throw new ArgumentNullException(nameof(ship));
    }

    public void Execute()
    {
        _ship.Move();
    }
}
