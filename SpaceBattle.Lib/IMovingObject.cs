using System;

namespace SpaceBattle.Lib;

public interface IMovingObject
{
    NVector Position { get; set; }

    [Adapter(typeof(IMovingObject), "Velocity", typeof(MovementCalculations), "GetVelocityByAngle")]
    NVector Velocity { get; }
}
