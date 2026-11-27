namespace SpaceBattle.Lib;

public class Angle
{
    public int Degrees { get; }
    public int Denominator { get; }
    public Angle(int[] degrees)
    {
        Degrees = degrees[0];
        Denominator = degrees[1];
    }

    public static Angle operator +(Angle a, Angle b)
    {
        if (a.Denominator != b.Denominator)
        {
            throw new ArgumentException("angles not compatible");
        }
        var res = new int[] { (a.Degrees + b.Degrees) % a.Denominator, a.Denominator };
        return new Angle(res);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;

        if (GetType() != obj.GetType()) return false;
        Angle a = (Angle)obj;

        return a.Denominator == Denominator && a.Degrees % Denominator == Degrees % Denominator;
    }

    public static bool operator ==(Angle a, Angle b)
    {
        if (a.Denominator != b.Denominator || a.Degrees % a.Denominator != b.Degrees % b.Denominator) return false;

        return true;
    }

    public static bool operator !=(Angle a, Angle b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}