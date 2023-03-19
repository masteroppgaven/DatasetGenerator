using UnityEngine;

public struct DVector3
{
    public double x;
    public double y;
    public double z;

    public DVector3(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static DVector3 operator +(DVector3 a, DVector3 b)
    {
        return new DVector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static DVector3 operator -(DVector3 a, DVector3 b)
    {
        return new DVector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static DVector3 operator *(DVector3 a, double scalar)
    {
        return new DVector3(a.x * scalar, a.y * scalar, a.z * scalar);
    }

    public DVector3 normalized
    {
        get
        {
            double magnitude = System.Math.Sqrt(x * x + y * y + z * z);
            return new DVector3(x / magnitude, y / magnitude, z / magnitude);
        }
    }

    public static DVector3 Cross(DVector3 a, DVector3 b)
    {
        return new DVector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);
    }

    public static double Dot(DVector3 a, DVector3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    public static Vector3 RayDirectionToPlane(Vector3 rayStart, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        DVector3 drayStart = new DVector3(rayStart.x, rayStart.y, rayStart.z);
        DVector3 dp1 = new DVector3(p1.x, p1.y, p1.z);
        DVector3 dp2 = new DVector3(p2.x, p2.y, p2.z);
        DVector3 dp3 = new DVector3(p3.x, p3.y, p3.z);

        DVector3 N = Cross(dp2 - dp1, dp3 - dp1).normalized;
        DVector3 result = (dp1 - drayStart - N * Dot(dp1 - drayStart, N)).normalized;

        return new Vector3((float)result.x, (float)result.y, (float)result.z);
    }
}
