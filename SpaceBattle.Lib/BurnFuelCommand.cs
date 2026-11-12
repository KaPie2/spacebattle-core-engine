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
        if (_fuelable.FuelConsumption < 0)
        {
            throw new InvalidOperationException("Fuel consumption cannot be negative");
        }

        if (_fuelable.FuelLevel < _fuelable.FuelConsumption)
        {
            throw new InvalidOperationException("Not enough fuel");
        }

        _fuelable.FuelLevel -= _fuelable.FuelConsumption;
    }
}
