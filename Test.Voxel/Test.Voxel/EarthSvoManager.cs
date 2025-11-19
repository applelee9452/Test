using System;
using System.Collections.Generic;

// 地球表面按CellSize（100 公里）分块，每个 Cell 对应一个独立的 SVO 子树，通过CellID索引，支持动态加载 / 卸载。
/// <summary>
/// 地球SVO分块管理器（负责Cell级SVO的加载、卸载和LOD调整）
/// </summary>
public class EarthSvoManager
{
    private readonly Dictionary<(int x, int z), SvoNode> _loadedCells = new(); // 已加载的Cell（X/Z索引）
    private readonly DynamicOriginManager _svoOrigin; // SVO动态原点（关联地球局部坐标）

    public EarthSvoManager(DynamicOriginManager svoOrigin)
    {
        _svoOrigin = svoOrigin;
    }

    /// <summary>
    /// 计算坐标对应的CellID（X/Z索引）
    /// </summary>
    public (int x, int z) GetCellId(Vector3d svoLocalPos)
    {
        int cellX = (int)Math.Floor(svoLocalPos.X / EarthSvoConstants.CellSize);
        int cellZ = (int)Math.Floor(svoLocalPos.Z / EarthSvoConstants.CellSize);
        return (cellX, cellZ);
    }

    /// <summary>
    /// 加载指定Cell的SVO（从磁盘/内存加载，这里模拟生成）
    /// </summary>
    public SvoNode LoadCell((int x, int z) cellId)
    {
        if (_loadedCells.TryGetValue(cellId, out var existingNode))
            return existingNode;

        // 计算Cell的原点（相对于SVO全局原点）
        var cellOrigin = new Vector3d(
            cellId.x * EarthSvoConstants.CellSize,
            0,
            cellId.z * EarthSvoConstants.CellSize
        );

        // 创建Cell的根节点（LOD级别1，尺寸=CellSize）
        var rootNode = new SvoNode(cellOrigin, EarthSvoConstants.CellSize, EarthSvoConstants.LodMinLevel);

        // 模拟生成体素数据（示例：生成简单地形，中间高四周低）
        GenerateTerrainData(rootNode, cellId);

        _loadedCells[cellId] = rootNode;
        Console.WriteLine($"加载地球SVO Cell ({cellId.x}, {cellId.z})，根节点尺寸：{rootNode.Size}米");
        return rootNode;
    }

    /// <summary>
    /// 模拟生成地形数据（递归细分节点并设置体素）
    /// </summary>
    private void GenerateTerrainData(SvoNode node, (int x, int z) cellId)
    {
        // 达到最大LOD或手动终止条件（如平坦区域无需细分）
        if (node.IsLeaf() || ShouldStopSubdivision(node))
        {
            // 设置体素数据（示例：根据位置生成地形类型）
            var voxel = new VoxelData
            {
                TerrainType = GetTerrainType(node.Position, cellId),
                IsCollidable = true,
                TextureId = GetTextureId(node.Position)
            };
            node.SetAsLeaf(voxel);
            return;
        }

        // 递归细分8个子节点
        for (int i = 0; i < EarthSvoConstants.NodeChildCount; i++)
        {
            var child = node.GetChild(i);
            GenerateTerrainData(child, cellId);
        }
    }

    /// <summary>
    /// 判断是否停止细分（示例：平坦区域或高LOD）
    /// </summary>
    private bool ShouldStopSubdivision(SvoNode node)
    {
        // 简化逻辑：LOD级别>5时停止细分（精度足够）
        return node.LodLevel > 5;
    }

    /// <summary>
    /// 根据位置获取地形类型（示例：模拟海拔高度）
    /// </summary>
    private byte GetTerrainType(Vector3d position, (int x, int z) cellId)
    {
        // 简单噪声函数模拟海拔：中间高（山脉），边缘低（海洋）
        double distanceFromCenter = Vector3d.Distance(position, new Vector3d(
            (cellId.x + 0.5) * EarthSvoConstants.CellSize,
            0,
            (cellId.z + 0.5) * EarthSvoConstants.CellSize
        ));
        return distanceFromCenter < EarthSvoConstants.CellSize * 0.3
            ? (byte)1 // 山脉（岩石）
            : (byte)2; // 海洋（水）
    }

    /// <summary>
    /// 获取纹理ID（示例）
    /// </summary>
    private ushort GetTextureId(Vector3d position) => (ushort)(position.Y % 5); // 简单映射

    /// <summary>
    /// 卸载超出视野的Cell
    /// </summary>
    public void UnloadDistantCells(Vector3d playerPos, int viewDistance)
    {
        var playerCell = GetCellId(playerPos);
        var cellsToRemove = new List<(int x, int z)>();

        foreach (var (cellId, _) in _loadedCells)
        {
            // 计算Cell与玩家的距离（格子数）
            int distanceX = Math.Abs(cellId.x - playerCell.x);
            int distanceZ = Math.Abs(cellId.z - playerCell.z);
            if (distanceX > viewDistance || distanceZ > viewDistance)
                cellsToRemove.Add(cellId);
        }

        foreach (var cellId in cellsToRemove)
        {
            _loadedCells.Remove(cellId);
            Console.WriteLine($"卸载地球SVO Cell ({cellId.x}, {cellId.z})");
        }
    }

    /// <summary>
    /// 根据局部坐标获取体素数据（用于碰撞检测、渲染）
    /// </summary>
    public VoxelData? GetVoxel(Vector3d svoLocalPos)
    {
        var cellId = GetCellId(svoLocalPos);
        if (!_loadedCells.TryGetValue(cellId, out var cellRoot))
            return null; // Cell未加载

        // 递归查找体素所在的叶子节点
        return FindVoxelInNode(cellRoot, svoLocalPos);
    }

    /// <summary>
    /// 在节点中递归查找体素
    /// </summary>
    private VoxelData? FindVoxelInNode(SvoNode node, Vector3d targetPos)
    {
        if (node.IsLeaf())
            return node.Voxel; // 叶子节点直接返回体素

        // 判断目标位置属于哪个子节点
        int childIndex = GetChildIndex(node, targetPos);
        var child = node.GetChild(childIndex);
        return FindVoxelInNode(child, targetPos);
    }

    /// <summary>
    /// 计算目标位置属于哪个子节点（0-7）
    /// </summary>
    private int GetChildIndex(SvoNode parent, Vector3d targetPos)
    {
        int index = 0;
        if (targetPos.X > parent.Position.X) index |= 1;
        if (targetPos.Y > parent.Position.Y) index |= 2;
        if (targetPos.Z > parent.Position.Z) index |= 4;
        return index;
    }
}