using System;

using Test;

public class Spaceship
{
    public GlobalId GlobalId { get; private set; }
    public string Name { get; }
    public Vector3d Position { get; set; } // 当前坐标（依赖CoordinateType）
    public CoordinateType CoordinateType { get; private set; }
    public DynamicOriginManager OriginManager { get; set; } // 所属坐标系的动态原点

    // 战斗相关属性
    public bool IsAlive { get; set; } = true;
    public double Shield { get; set; } = 1000;

    public Spaceship(string name, GlobalId initialId, CoordinateType initialType, DynamicOriginManager originManager)
    {
        Name = name;
        GlobalId = initialId;
        CoordinateType = initialType;
        OriginManager = originManager;
        // 从GlobalId解析初始位置（简化：假设LocalX/LocalZ对应SVO坐标）
        initialId.Decode(out _, out _, out _, out var bodyId, out int x, out int z);
        Position = new Vector3d(x, 0, z); // Y暂定为0
    }

    // 移动飞船（更新位置并检查原点重置）
    public void Move(Vector3d delta)
    {
        Position += delta;
        var offset = OriginManager.CheckAndResetOrigin(Position);
        if (offset != Vector3d.Zero)
        {
            // 原点重置时，调整自身位置
            Position += offset;
        }
    }

    // 从地球SVO进入虚空
    public void EnterVoid()
    {
        // 1. 解码当前GlobalId（地球SVO）
        GlobalId.Decode(out var sectorId, out var galaxyId, out var systemId, out var earthId, out _, out _);

        // 2. 转换坐标：SVO局部 → 地球中心 → 太阳系虚空
        var earthCentric = CoordinateConverter.SvoToBodyCentric(Position, earthId, OriginManager);
        var systemVoidPos = CoordinateConverter.BodyCentricToSystemVoid(earthCentric, earthId);

        // 3. 更新状态
        Position = systemVoidPos;
        CoordinateType = CoordinateType.SystemVoid;
        GlobalId = GlobalId.Encode(sectorId, galaxyId, systemId, 0, 0, 0); // BodyId=0表示虚空
    }

    // 从虚空进入火星SVO
    public void EnterMarsSvo(DynamicOriginManager marsSvoOrigin)
    {
        // 1. 解码当前GlobalId（虚空）
        GlobalId.Decode(out var sectorId, out var galaxyId, out var systemId, out _, out _, out _);

        // 2. 转换坐标：太阳系虚空 → 火星中心 → 火星SVO局部
        var marsCentric = CoordinateConverter.SystemVoidToBodyCentric(Position, 4); // 火星BodyId=4
        var marsSvoPos = CoordinateConverter.BodyCentricToSvo(marsCentric, 4, marsSvoOrigin);

        // 3. 更新状态
        Position = marsSvoPos;
        CoordinateType = CoordinateType.SvoLocal;
        GlobalId = GlobalId.Encode(sectorId, galaxyId, systemId, 4, (int)marsSvoPos.X, (int)marsSvoPos.Z);
        OriginManager = marsSvoOrigin; // 切换到火星SVO的原点管理器
    }

    // 攻击其他飞船
    public void Attack(Spaceship target)
    {
        if (!IsAlive || !target.IsAlive) return;

        // 计算距离（根据双方坐标类型转换为统一坐标系）
        var distance = CalculateDistance(this, target);
        if (distance < 10000) // 10公里内有效攻击
        {
            target.Shield -= 100;
            if (target.Shield <= 0) target.IsAlive = false;
            Console.WriteLine($"{Name}攻击了{target.Name}，剩余护盾：{target.Shield:F0}");
        }
    }

    // 计算两艘飞船的距离（处理跨坐标系情况）
    private static double CalculateDistance(Spaceship a, Spaceship b)
    {
        // 简化：统一转换为恒星系虚空坐标计算
        Vector3d aSystem = a.CoordinateType switch
        {
            CoordinateType.SvoLocal => CoordinateConverter.BodyCentricToSystemVoid(
                CoordinateConverter.SvoToBodyCentric(a.Position, a.GetBodyId(), a.OriginManager), a.GetBodyId()),
            CoordinateType.BodyCentric => CoordinateConverter.BodyCentricToSystemVoid(a.Position, a.GetBodyId()),
            _ => a.Position
        };

        Vector3d bSystem = b.CoordinateType switch
        {
            CoordinateType.SvoLocal => CoordinateConverter.BodyCentricToSystemVoid(
                CoordinateConverter.SvoToBodyCentric(b.Position, b.GetBodyId(), b.OriginManager), b.GetBodyId()),
            CoordinateType.BodyCentric => CoordinateConverter.BodyCentricToSystemVoid(b.Position, b.GetBodyId()),
            _ => b.Position
        };

        return Vector3d.Distance(aSystem, bSystem);
    }

    // 获取所属天体ID（SVO模式下）
    private ushort GetBodyId()
    {
        GlobalId.Decode(out _, out _, out _, out var bodyId, out _, out _);
        return bodyId;
    }
}