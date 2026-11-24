namespace SpaceBattle.Lib;

public class NVector
{
    private readonly int[] _coords;

    public int Size => _coords.Length;

    public int this[int index] => _coords[index];

    public NVector(params int[] coords)
    {
        _coords = (int[])coords.Clone();
    }

    public static NVector operator +(NVector a, NVector b)
    {
        if (a.Size != b.Size)
        {
            throw new ArgumentException("Dimensions are not compatible");
        }

        int[] res = new int[a.Size];
        for (int i = 0; i < a.Size; i++)
        {
            res[i] = a[i] + b[i];
        }

        return new NVector(res);
    }

    public static bool operator ==(NVector a, NVector b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

        return a.Equals(b);
    }

    public static bool operator !=(NVector a, NVector b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not NVector other) return false;
        if (this.Size != other.Size) return false;

        return _coords.SequenceEqual(other._coords);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var coord in _coords)
        {
            hash.Add(coord);
        }
        return hash.ToHashCode();
    }
}
