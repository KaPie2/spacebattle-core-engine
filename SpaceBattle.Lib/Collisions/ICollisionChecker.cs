namespace SpaceBattle.Lib;

public interface ICollisionChecker
{
    bool HaveCollided(IMovingObject? movingObject1, IMovingObject? movingObject2);
}
