namespace SpaceBattle.Lib;

public interface IFuelable
{
    int FuelLevel { get; set; }
    int FuelConsumption { get; }
}
