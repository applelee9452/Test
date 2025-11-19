using System;
using System.Collections.Generic;

// 地球SVO常量（基于之前的LOD设计）
public static class EarthSvoConstants
{
    public const int LodMaxLevel = 9; // 最大LOD级别（1米精度）
    public const int LodMinLevel = 1; // 最小LOD级别（8192米精度）
    public const int BaseVoxelSize = 1; // 最低LOD（9级）的体素尺寸（米）
    public const double CellSize = 100000; // SVO分块大小（100公里/块，便于加载）
    public const int NodeChildCount = 8; // 八叉树每个节点的子节点数（2^3）
}

// 体素数据（存储地形类型、是否可碰撞等信息）
public struct VoxelData
{
    public byte TerrainType; // 0=空气，1=岩石，2=水，3=建筑等
    public bool IsCollidable; // 是否可碰撞
    public ushort TextureId; // 纹理ID（用于渲染）
}

// 八叉树节点类型（内部节点/叶子节点）
public enum SvoNodeType
{
    Internal,
    Leaf
}