using System;

using Test;

/// <summary>
/// 双精度三维向量（支持坐标计算的核心结构）
/// </summary>
public struct Vector3d : IEquatable<Vector3d>
{
    public static readonly Vector3d Zero = new(0, 0, 0);

    public double X;
    public double Y;
    public double Z;

    public Vector3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// 向量长度（模长）
    /// </summary>
    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// 归一化（单位向量）
    /// </summary>
    public Vector3d Normalize()
    {
        double len = Length();
        if (len < 1e-9) return new Vector3d(0, 0, 0); // 避免除零
        return new Vector3d(X / len, Y / len, Z / len);
    }

    /// <summary>
    /// 两个向量相加
    /// </summary>
    public static Vector3d operator +(Vector3d a, Vector3d b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>
    /// 两个向量相减
    /// </summary>
    public static Vector3d operator -(Vector3d a, Vector3d b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>
    /// 向量乘以标量（缩放）
    /// </summary>
    public static Vector3d operator *(Vector3d v, double s)
        => new Vector3d(v.X * s, v.Y * s, v.Z * s);

    /// <summary>
    /// 标量乘以向量（缩放）
    /// </summary>
    public static Vector3d operator *(double s, Vector3d v)
        => new(v.X * s, v.Y * s, v.Z * s);

    /// <summary>
    /// 重载==运算符：检查两个向量是否相等
    /// </summary>
    public static bool operator ==(Vector3d a, Vector3d b)
    {
        // 直接比较X/Y/Z分量（考虑双精度浮点数的精度误差）
        return Math.Abs(a.X - b.X) < 1e-9 &&
               Math.Abs(a.Y - b.Y) < 1e-9 &&
               Math.Abs(a.Z - b.Z) < 1e-9;
    }

    /// <summary>
    /// 重载!=运算符：检查两个向量是否不相等
    /// </summary>
    public static bool operator !=(Vector3d a, Vector3d b)
    {
        return !(a == b); // 复用==的逻辑
    }

    /// <summary>
    /// 计算两个点之间的距离
    /// </summary>
    public static double Distance(Vector3d a, Vector3d b)
        => (a - b).Length();

    public bool Equals(Vector3d other)
        => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object obj)
        => obj is Vector3d other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);

    public override string ToString()
        => $"({X:F2}, {Y:F2}, {Z:F2})";
}