using System.Collections.Generic;

using Test;

// 几何数据基类
public abstract class GeoData
{
    public ushort Id; // 层级ID
    public Vector3d Center; // 中心坐标（单位随层级变化）
    public double Radius; // 覆盖半径
}

// 星区几何数据（单位：光年）
public class SectorGeo : GeoData
{
}

// 星系几何数据（单位：光年，相对于星区中心）
public class GalaxyGeo : GeoData
{
    public ushort ParentSectorId;
}

// 恒星系几何数据（单位：AU，相对于星系中心）
public class SystemGeo : GeoData
{
    public ushort ParentGalaxyId;
}

// 天体几何数据（单位：米，相对于恒星系中心）
public class BodyGeo : GeoData
{
    public ushort ParentSystemId;
    public Vector3d SvoOriginOffset; // SVO局部原点相对于天体中心的偏移
}

// 几何数据库（模拟数据库，实际项目可用SQL/NoSQL替代）
public static class GeoDatabase
{
    // 预定义数据：太阳系（恒星系ID=1）、地球（BodyID=3）、火星（BodyID=4）
    public static readonly Dictionary<ushort, SectorGeo> Sectors = new()
    {
        { 15, new SectorGeo { Id = 15, Center = new Vector3d(0, 0, 0), Radius = 1000 } } // 星区15
    };

    public static readonly Dictionary<ushort, GalaxyGeo> Galaxies = new()
    {
        { 256, new GalaxyGeo { Id = 256, ParentSectorId = 15, Center = new Vector3d(50, 0, 0), Radius = 500 } } // 星系256
    };

    public static readonly Dictionary<ushort, SystemGeo> Systems = new()
    {
        { 1, new SystemGeo { Id = 1, ParentGalaxyId = 256, Center = new Vector3d(0, 0, 0), Radius = 1 } } // 太阳系（ID=1）
    };

    public static readonly Dictionary<ushort, BodyGeo> Bodies = new()
    {
        { 3, new BodyGeo // 地球（ID=3）
            {
                Id = 3,
                ParentSystemId = 1,
                Center = new Vector3d(Constants.AuToMeter, 0, 0), // 地球在太阳系中距离太阳1AU
                Radius = Constants.EarthRadius,
                SvoOriginOffset = new Vector3d(0, 0, 0) // SVO原点与地心重合（简化）
            }
        },
        { 4, new BodyGeo // 火星（ID=4）
            {
                Id = 4,
                ParentSystemId = 1,
                Center = new Vector3d(1.524 * Constants.AuToMeter, 0, 0), // 火星距太阳1.524AU
                Radius = Constants.MarsRadius,
                SvoOriginOffset = new Vector3d(0, 0, 0)
            }
        }
    };
}