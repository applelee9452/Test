using System;
using System.Collections.Generic;
using System.Numerics;

using Test;

// 常量定义（单位：米、千米、天文单位AU、光年）
public static class Constants
{
    public const double EarthRadius = 6371000; // 地球半径（米）
    public const double MarsRadius = 3390000;  // 火星半径（米）
    public const double SvoCriticalHeight = 100000; // SVO临界高度（100公里）
    public const double TransitionLayerRange = 10000; // 过渡层范围（10公里）
    public const double AuToMeter = 149597870700; // 1AU=1.496e11米
    public const double LightYearToMeter = 9460730472580800; // 1光年=9.46e15米
    public const double VoidOriginResetThreshold = 1e9; // 虚空原点重置阈值（1000公里）
}

// 全局128位ID（用两个ulong存储高位和低位）
public struct GlobalId : IEquatable<GlobalId>
{
    public ulong High; // 高位64位：星区(16)+星系(16)+恒星系(16)+天体(16)
    public ulong Low;  // 低位64位：LocalX(32)+LocalZ(32)（带符号）

    // 编码：从层级信息生成GlobalId
    public static GlobalId Encode(ushort sectorId, ushort galaxyId, ushort systemId, ushort bodyId, int localX, int localZ)
    {
        ulong high = 0;
        high |= (ulong)sectorId << 48;   // 星区ID：63-48位（高位64中的16位）
        high |= (ulong)galaxyId << 32;  // 星系ID：47-32位
        high |= (ulong)systemId << 16;  // 恒星系ID：31-16位
        high |= bodyId;                 // 天体ID：15-0位

        ulong low = 0;
        low |= (ulong)((uint)localX) << 32; // LocalX：63-32位（低位64中的32位）
        low |= (uint)localZ;                // LocalZ：31-0位

        return new GlobalId { High = high, Low = low };
    }

    // 解码：从GlobalId解析层级信息
    public void Decode(out ushort sectorId, out ushort galaxyId, out ushort systemId, out ushort bodyId, out int localX, out int localZ)
    {
        sectorId = (ushort)(High >> 48);
        galaxyId = (ushort)(High >> 32);
        systemId = (ushort)(High >> 16);
        bodyId = (ushort)(High & 0xFFFF);

        uint xRaw = (uint)(Low >> 32);
        localX = xRaw <= int.MaxValue ? (int)xRaw : (int)(xRaw - uint.MaxValue - 1);

        uint zRaw = (uint)(Low & 0xFFFFFFFF);
        localZ = zRaw <= int.MaxValue ? (int)zRaw : (int)(zRaw - uint.MaxValue - 1);
    }

    public bool Equals(GlobalId other) => High == other.High && Low == other.Low;
    public override bool Equals(object obj) => obj is GlobalId other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(High, Low);
}