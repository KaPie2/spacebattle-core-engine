namespace SpaceBattle.Lib;

public class CollisionChecker : ICollisionChecker
{
    public const double DefaultCollisionDistance = 1.0;
    private const double Epsilon = 1e-9;
    private const double TimeTickStart = 0.0;
    private const double TimeTickEnd = 1.0;
    private const int MinVectorDimensions = 2;

    private readonly double _collisionDistance;

    public CollisionChecker(double collisionDistance = DefaultCollisionDistance)
    {
        if (collisionDistance < 0)
        {
            throw new ArgumentException("Collision distance cannot be negative", nameof(collisionDistance));
        }

        _collisionDistance = collisionDistance;
    }

    public bool HaveCollided(IMovingObject? movingObject1, IMovingObject? movingObject2)
    {
        if (movingObject1 is null)
        {
            throw new ArgumentNullException(nameof(movingObject1));
        }
        if (movingObject2 is null)
        {
            throw new ArgumentNullException(nameof(movingObject2));
        }

        var p1 = movingObject1.Position ?? throw new ArgumentNullException(nameof(movingObject1.Position));
        var v1 = movingObject1.Velocity ?? throw new ArgumentNullException(nameof(movingObject1.Velocity));
        var p2 = movingObject2.Position ?? throw new ArgumentNullException(nameof(movingObject2.Position));
        var v2 = movingObject2.Velocity ?? throw new ArgumentNullException(nameof(movingObject2.Velocity));

        if (p1.Size < MinVectorDimensions || p2.Size < MinVectorDimensions ||
            v1.Size < MinVectorDimensions || v2.Size < MinVectorDimensions)
        {
            throw new ArgumentException("Vectors must have at least 2 dimensions");
        }

        double dx = p1[0] - p2[0];
        double dy = p1[1] - p2[1];

        double dvx = v1[0] - v2[0];
        double dvy = v1[1] - v2[1];

        double a = dvx * dvx + dvy * dvy;
        double b = dx * dvx + dy * dvy;

        double tClosest = Math.Abs(a) < Epsilon ? TimeTickStart : -b / a;

        double tClamped = Math.Clamp(tClosest, TimeTickStart, TimeTickEnd);

        double closestDx = dx + dvx * tClamped;
        double closestDy = dy + dvy * tClamped;
        double minDistanceSquared = closestDx * closestDx + closestDy * closestDy;

        return minDistanceSquared <= (_collisionDistance * _collisionDistance) + Epsilon;
    }
}
