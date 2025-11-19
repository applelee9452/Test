using Test;

// 坐标类型枚举
public enum CoordinateType
{
    SvoLocal,       // SVO局部坐标（米）
    BodyCentric,    // 天体中心坐标（米）
    SystemVoid      // 恒星系虚空坐标（米，相对于恒星系中心）
}

// 动态原点管理器（处理SVO和虚空的原点重置）
public class DynamicOriginManager
{
    public Vector3d CurrentOrigin { get; private set; } // 当前原点
    public double ResetThreshold { get; } // 原点重置阈值
    public CoordinateType CoordinateType { get; } // 适用坐标类型

    public DynamicOriginManager(CoordinateType type, double threshold)
    {
        CoordinateType = type;
        ResetThreshold = threshold;
        CurrentOrigin = Vector3d.Zero;
    }

    // 检查是否需要重置原点，若需要则更新原点并返回偏移量
    public Vector3d CheckAndResetOrigin(Vector3d currentPosition)
    {
        var offsetFromOrigin = currentPosition - CurrentOrigin;
        if (offsetFromOrigin.Length() > ResetThreshold)
        {
            var oldOrigin = CurrentOrigin;
            CurrentOrigin = currentPosition; // 以当前位置为新原点
            return oldOrigin - CurrentOrigin; // 偏移量：旧原点→新原点的反向
        }
        return Vector3d.Zero;
    }
}

// 坐标转换器（处理不同坐标系间的转换）
public static class CoordinateConverter
{
    // 1. SVO局部坐标 → 天体中心坐标
    public static Vector3d SvoToBodyCentric(Vector3d svoLocal, ushort bodyId, DynamicOriginManager svoOrigin)
    {
        var body = GeoDatabase.Bodies[bodyId];
        // SVO局部坐标 = 相对于SVO原点的偏移，需加上原点到地心的偏移
        return svoLocal + svoOrigin.CurrentOrigin + body.SvoOriginOffset;
    }

    // 2. 天体中心坐标 → 恒星系虚空坐标
    public static Vector3d BodyCentricToSystemVoid(Vector3d bodyCentric, ushort bodyId)
    {
        var body = GeoDatabase.Bodies[bodyId];
        var system = GeoDatabase.Systems[body.ParentSystemId];
        // 天体中心在恒星系中的绝对坐标 + 相对于天体中心的偏移
        return body.Center + bodyCentric;
    }

    // 3. 恒星系虚空坐标 → 目标天体中心坐标
    public static Vector3d SystemVoidToBodyCentric(Vector3d systemVoid, ushort targetBodyId)
    {
        var targetBody = GeoDatabase.Bodies[targetBodyId];
        // 相对于恒星系中心的坐标 - 目标天体中心的坐标
        return systemVoid - targetBody.Center;
    }

    // 4. 天体中心坐标 → SVO局部坐标
    public static Vector3d BodyCentricToSvo(Vector3d bodyCentric, ushort bodyId, DynamicOriginManager svoOrigin)
    {
        var body = GeoDatabase.Bodies[bodyId];
        // 相对于天体中心的坐标 - SVO原点到地心的偏移 - 当前SVO原点
        return bodyCentric - body.SvoOriginOffset - svoOrigin.CurrentOrigin;
    }
}