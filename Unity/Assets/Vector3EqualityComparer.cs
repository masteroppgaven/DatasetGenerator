using System.Collections.Generic;

public class Vector3EqualityComparer : IEqualityComparer<System.Numerics.Vector3>
{
    public bool Equals(System.Numerics.Vector3 v1, System.Numerics.Vector3 v2)
    {
        return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
    }

    public int GetHashCode(System.Numerics.Vector3 v)
    {
        return v.X.GetHashCode() ^ v.Y.GetHashCode() ^ v.Z.GetHashCode();
    }
}
