/// <summary>
/// 飞船与地球SVO的碰撞检测
/// </summary>
public static class SvoCollisionDetector
{
    /// <summary>
    /// 检查飞船是否与地球体素碰撞
    /// </summary>
    public static bool CheckCollision(Spaceship ship, EarthSvoManager svoManager)
    {
        if (ship.CoordinateType != CoordinateType.SvoLocal)
            return false; // 不在SVO范围内

        // 获取飞船在SVO中的局部坐标（Y为海拔）
        var shipPos = ship.Position;
        var voxel = svoManager.GetVoxel(shipPos);

        // 碰撞条件：体素存在且可碰撞，且飞船海拔低于体素高度
        return voxel.HasValue && voxel.Value.IsCollidable && shipPos.Y < GetVoxelHeight(voxel.Value, shipPos);
    }

    /// <summary>
    /// 获取体素的高度（简化：根据地形类型返回高度）
    /// </summary>
    private static double GetVoxelHeight(VoxelData voxel, Vector3d pos)
    {
        // 示例：岩石地形海拔高，海洋海拔低
        return voxel.TerrainType == 1 ? 500 : 0; // 岩石500米，海洋0米
    }
}