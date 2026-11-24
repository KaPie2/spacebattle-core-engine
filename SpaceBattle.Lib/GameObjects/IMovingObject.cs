namespace SpaceBattle.Lib;

public interface IMovingObject
{
    NVector Position { get; set; }
    NVector Velocity { get; }
}
