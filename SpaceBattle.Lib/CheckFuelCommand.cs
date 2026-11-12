namespace SpaceBattle.Lib;

public class CheckFuelCommand : ICommand
{
    private readonly IFuelable _fuelable;

    public CheckFuelCommand(IFuelable fuelable)
    {
        _fuelable = fuelable ?? throw new ArgumentNullException(nameof(fuelable));
    }

    public void Execute()
    {
        EnsureSufficientFuel(_fuelable);
    }

    internal static void EnsureSufficientFuel(IFuelable fuelable)
    {
        if (fuelable.FuelConsumption < 0)
        {
            throw new InvalidOperationException("Fuel consumption cannot be negative");
        }

        if (fuelable.FuelLevel < fuelable.FuelConsumption)
        {
            throw new InvalidOperationException("Not enough fuel");
        }
    }
}
