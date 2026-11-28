namespace SpaceBattle.Lib;

public class BurnFuelCommand : ICommand
{
    private readonly IFuelable _fuelable;

    public BurnFuelCommand(IFuelable fuelable)
    {
        _fuelable = fuelable ?? throw new ArgumentNullException(nameof(fuelable));
    }

    public void Execute()
    {
        if (_fuelable.FuelLevel < _fuelable.FuelConsumption)
        {
            throw new CommandException("Недостаточно топлива для выполнения маневра.");
        }

        _fuelable.FuelLevel -= _fuelable.FuelConsumption;
    }
}
