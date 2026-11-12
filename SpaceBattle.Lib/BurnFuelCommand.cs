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
        CheckFuelCommand.EnsureSufficientFuel(_fuelable);
        _fuelable.FuelLevel -= _fuelable.FuelConsumption;
    }
}
