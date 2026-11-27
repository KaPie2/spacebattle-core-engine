namespace SpaceBattle.Lib;

public class ScenarioResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public NVector? Position { get; set; }
    public int FuelLevel { get; set; }
    public IReadOnlyList<IDomainEvent> Events { get; set; } = Array.Empty<IDomainEvent>();
}
