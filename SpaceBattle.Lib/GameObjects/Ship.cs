using System.Text.Json;

namespace SpaceBattle.Lib;

public class Ship : IMovingObject, IRotatingObject
{
    private readonly object _id;
    private readonly List<IDomainEvent> _domainEvents = new();
    private NVector _position;
    private NVector _velocity;
    private Angle _angle;
    private Angle _angleVelocity;
    private int _fuelLevel;
    private int _fuelConsumption;

    public object Id => _id;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public NVector Position
    {
        get => _position;
        set => _position = value ?? throw new ArgumentNullException(nameof(value));
    }

    public NVector Velocity
    {
        get => _velocity;
        set => _velocity = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Angle Angle
    {
        get => _angle;
        set => _angle = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Angle AngleVelocity
    {
        get => _angleVelocity;
        set => _angleVelocity = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int FuelLevel
    {
        get => _fuelLevel;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Запас топлива не может быть отрицательным.");
            }
            _fuelLevel = value;
        }
    }

    public int FuelConsumption
    {
        get => _fuelConsumption;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Расход топлива не может быть отрицательным.");
            }
            _fuelConsumption = value;
        }
    }

    public Ship(
        object id,
        NVector position,
        NVector velocity,
        Angle direction,
        int fuelLevel,
        int fuelConsumption,
        Angle? angleVelocity = null)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        if (fuelLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fuelLevel), "Начальный запас топлива не может быть отрицательным.");
        }
        if (fuelConsumption < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fuelConsumption), "Расход топлива не может быть отрицательным.");
        }

        _position = position ?? throw new ArgumentNullException(nameof(position));
        _velocity = velocity ?? throw new ArgumentNullException(nameof(velocity));
        _angle = direction ?? throw new ArgumentNullException(nameof(direction));
        _angleVelocity = angleVelocity ?? new Angle(new int[] { 0, direction.Denominator });
        _fuelLevel = fuelLevel;
        _fuelConsumption = fuelConsumption;
    }

    public void Move()
    {
        if (FuelLevel < FuelConsumption)
        {
            throw new InvalidOperationException("Недостаточно топлива для выполнения перемещения.");
        }

        var oldPos = Position;
        var newPos = Position + Velocity;

        Position = newPos;
        FuelLevel -= FuelConsumption;

        _domainEvents.Add(new PositionChangedEvent(Id, oldPos, newPos));
        _domainEvents.Add(new FuelBurnedEvent(Id, FuelLevel, FuelConsumption));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public ShipSnapshot CreateSnapshot()
    {
        int[] posCoords = new int[Position.Size];
        for (int i = 0; i < Position.Size; i++)
        {
            posCoords[i] = Position[i];
        }

        int[] velCoords = new int[Velocity.Size];
        for (int i = 0; i < Velocity.Size; i++)
        {
            velCoords[i] = Velocity[i];
        }

        return new ShipSnapshot
        {
            ShipId = Id.ToString() ?? string.Empty,
            Position = posCoords,
            Velocity = velCoords,
            Direction = new int[] { Angle.Degrees, Angle.Denominator },
            FuelLevel = FuelLevel,
            FuelConsumption = FuelConsumption
        };
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(CreateSnapshot());
    }

    public static Ship FromSnapshot(ShipSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new Ship(
            snapshot.ShipId,
            new NVector(snapshot.Position),
            new NVector(snapshot.Velocity),
            new Angle(snapshot.Direction),
            snapshot.FuelLevel,
            snapshot.FuelConsumption
        );
    }

    public static Ship FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON не может быть пустым.", nameof(json));
        }

        var snapshot = JsonSerializer.Deserialize<ShipSnapshot>(json)
            ?? throw new InvalidOperationException("Не удалось восстановить состояние корабля из JSON.");

        return FromSnapshot(snapshot);
    }
}
