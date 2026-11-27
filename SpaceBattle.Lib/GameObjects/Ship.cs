using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SpaceBattle.Lib;

public class Ship : IMovingObject, IFuelable, IRotatingObject, IUObject
{
    private readonly object _id;
    private readonly UObject _uObject = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public object Id => _id;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public NVector Position
    {
        get => (NVector)_uObject["Position"];
        set => _uObject["Position"] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public NVector Velocity
    {
        get => (NVector)_uObject["Velocity"];
        set => _uObject["Velocity"] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Angle Angle
    {
        get => (Angle)_uObject["Angle"];
        set => _uObject["Angle"] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Angle AngleVelocity
    {
        get => (Angle)_uObject["AngleVelocity"];
        set => _uObject["AngleVelocity"] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int FuelLevel
    {
        get => (int)_uObject["FuelLevel"];
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Запас топлива не может быть отрицательным.");
            }
            _uObject["FuelLevel"] = value;
        }
    }

    public int FuelConsumption
    {
        get => (int)_uObject["FuelConsumption"];
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Расход топлива не может быть отрицательным.");
            }
            _uObject["FuelConsumption"] = value;
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

        _uObject["Id"] = _id;
        _uObject["Position"] = position ?? throw new ArgumentNullException(nameof(position));
        _uObject["Velocity"] = velocity ?? throw new ArgumentNullException(nameof(velocity));
        _uObject["Angle"] = direction ?? throw new ArgumentNullException(nameof(direction));
        _uObject["AngleVelocity"] = angleVelocity ?? new Angle(new int[] { 0, direction.Denominator });
        _uObject["FuelLevel"] = fuelLevel;
        _uObject["FuelConsumption"] = fuelConsumption;
    }

    public void Move()
    {
        if (FuelLevel < FuelConsumption)
        {
            throw new CommandException("Недостаточно топлива для выполнения перемещения.");
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

    // IUObject implementation
    public object GetProperty(string key) => _uObject.GetProperty(key);
    public void SetProperty(string key, object value) => _uObject.SetProperty(key, value);

    public object this[string key]
    {
        get => _uObject[key];
        set => _uObject[key] = value;
    }

    public ICollection<string> Keys => _uObject.Keys;
    public ICollection<object> Values => _uObject.Values;
    public int Count => _uObject.Count;
    public bool IsReadOnly => _uObject.IsReadOnly;

    public void Add(string key, object value) => _uObject.Add(key, value);
    public void Add(KeyValuePair<string, object> item) => _uObject.Add(item);
    public void Clear() => _uObject.Clear();
    public bool Contains(KeyValuePair<string, object> item) => _uObject.Contains(item);
    public bool ContainsKey(string key) => _uObject.ContainsKey(key);
    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _uObject.CopyTo(array, arrayIndex);
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _uObject.GetEnumerator();
    public bool Remove(string key) => _uObject.Remove(key);
    public bool Remove(KeyValuePair<string, object> item) => _uObject.Remove(item);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => _uObject.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_uObject).GetEnumerator();
}
