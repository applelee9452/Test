using System;
using System.Collections.Generic;

/// <summary>
/// 表示三维整数坐标
/// </summary>
public struct Vector3Int
{
    public int X;
    public int Y;
    public int Z;

    public Vector3Int(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override bool Equals(object obj)
    {
        return obj is Vector3Int int3 &&
               X == int3.X &&
               Y == int3.Y &&
               Z == int3.Z;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(Vector3Int left, Vector3Int right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3Int left, Vector3Int right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}

/// <summary>
/// 体素类，包含LOD信息
/// </summary>
public class Voxel
{
    // 体素属性
    public int MaterialId { get; set; }
    public bool IsActive { get; set; }

    // LOD相关属性
    public int LODLevel { get; }
    public Dictionary<int, Voxel> LodVariants { get; }

    public Voxel(int materialId, int lodLevel)
    {
        if (lodLevel < 0)
            throw new ArgumentException("LOD级别不能为负数", nameof(lodLevel));

        MaterialId = materialId;
        IsActive = true;
        LODLevel = lodLevel;
        LodVariants = new Dictionary<int, Voxel>();
    }

    /// <summary>
    /// 添加或更新特定LOD级别的变体
    /// </summary>
    public void AddLodVariant(int lodLevel, Voxel variant)
    {
        if (variant == null)
            throw new ArgumentNullException(nameof(variant));
        if (variant.LODLevel != lodLevel)
            throw new ArgumentException("变体的LOD级别不匹配", nameof(variant));

        if (LodVariants.ContainsKey(lodLevel))
            LodVariants[lodLevel] = variant;
        else
            LodVariants.Add(lodLevel, variant);
    }

    /// <summary>
    /// 获取特定LOD级别的变体
    /// </summary>
    public Voxel GetLodVariant(int lodLevel)
    {
        LodVariants.TryGetValue(lodLevel, out var variant);
        return variant;
    }
}

/// <summary>
/// 八叉树节点
/// </summary>
public class OctreeNode
{
    private const int ChildCount = 8;
    private readonly OctreeNode[] _children;
    private Voxel _voxel;
    private readonly int _depth;

    public Vector3Int Min { get; }
    public Vector3Int Max { get; }
    public Vector3Int Center => new Vector3Int(
        (Min.X + Max.X) / 2,
        (Min.Y + Max.Y) / 2,
        (Min.Z + Max.Z) / 2
    );

    public int Depth => _depth;
    public bool IsLeaf => _voxel != null;
    public Voxel Voxel
    {
        get => _voxel;
        set => _voxel = value;
    }

    public OctreeNode(Vector3Int min, Vector3Int max, int depth)
    {
        Min = min;
        Max = max;
        _depth = depth;
        _children = new OctreeNode[ChildCount];
    }

    public OctreeNode GetChild(int index)
    {
        if (index < 0 || index >= ChildCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }

    public void SetChild(int index, OctreeNode child)
    {
        if (index < 0 || index >= ChildCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        _children[index] = child;
    }

    public IEnumerable<OctreeNode> GetChildren()
    {
        for (int i = 0; i < ChildCount; i++)
        {
            yield return _children[i];
        }
    }

    /// <summary>
    /// 计算点所在的子节点索引
    /// </summary>
    public int GetChildIndex(Vector3Int point)
    {
        int index = 0;
        if (point.X > Center.X) index |= 1;
        if (point.Y > Center.Y) index |= 2;
        if (point.Z > Center.Z) index |= 4;
        return index;
    }

    /// <summary>
    /// 分裂节点，创建8个子节点
    /// </summary>
    public void Split()
    {
        Vector3Int center = Center;
        int childDepth = _depth + 1;

        // 创建8个子节点
        _children[0] = new OctreeNode(Min, new Vector3Int(center.X, center.Y, center.Z), childDepth);
        _children[1] = new OctreeNode(new Vector3Int(center.X, Min.Y, Min.Z), new Vector3Int(Max.X, center.Y, center.Z), childDepth);
        _children[2] = new OctreeNode(new Vector3Int(Min.X, center.Y, Min.Z), new Vector3Int(center.X, Max.Y, center.Z), childDepth);
        _children[3] = new OctreeNode(new Vector3Int(center.X, center.Y, Min.Z), new Vector3Int(Max.X, Max.Y, center.Z), childDepth);
        _children[4] = new OctreeNode(new Vector3Int(Min.X, Min.Y, center.Z), new Vector3Int(center.X, center.Y, Max.Z), childDepth);
        _children[5] = new OctreeNode(new Vector3Int(center.X, Min.Y, center.Z), new Vector3Int(Max.X, center.Y, Max.Z), childDepth);
        _children[6] = new OctreeNode(new Vector3Int(Min.X, center.Y, center.Z), new Vector3Int(center.X, Max.Y, Max.Z), childDepth);
        _children[7] = new OctreeNode(center, Max, childDepth);
    }

    /// <summary>
    /// 合并节点，删除所有子节点
    /// </summary>
    public void Merge()
    {
        Array.Clear(_children, 0, ChildCount);
    }

    /// <summary>
    /// 检查点是否在节点范围内
    /// </summary>
    public bool ContainsPoint(Vector3Int point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }
}

/// <summary>
/// 稀疏体素八叉树，支持LOD级别
/// </summary>
public class SparseVoxelOctree
{
    private readonly OctreeNode _root;
    private readonly int _maxLODLevel;
    private readonly int _maxDepth;

    public Vector3Int RootMin => _root.Min;
    public Vector3Int RootMax => _root.Max;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="maxLODLevel">最大LOD级别，例如9表示支持LOD0-LOD9</param>
    /// <param name="rootMin">根节点最小坐标</param>
    /// <param name="rootMax">根节点最大坐标</param>
    public SparseVoxelOctree(int maxLODLevel, Vector3Int rootMin, Vector3Int rootMax)
    {
        if (maxLODLevel < 0)
            throw new ArgumentException("最大LOD级别不能为负数", nameof(maxLODLevel));
        if (rootMin.X >= rootMax.X || rootMin.Y >= rootMax.Y || rootMin.Z >= rootMax.Z)
            throw new ArgumentException("根节点最小坐标必须小于最大坐标");

        _maxLODLevel = maxLODLevel;
        _maxDepth = maxLODLevel; // 最大深度对应最高LOD级别
        _root = new OctreeNode(rootMin, rootMax, 0);
    }

    /// <summary>
    /// 插入体素
    /// </summary>
    public void Insert(Vector3Int position, Voxel voxel)
    {
        if (voxel == null)
            throw new ArgumentNullException(nameof(voxel));
        if (voxel.LODLevel > _maxLODLevel)
            throw new ArgumentException($"LOD级别不能超过最大值 {_maxLODLevel}", nameof(voxel));
        if (!_root.ContainsPoint(position))
            throw new ArgumentException("位置在八叉树范围之外", nameof(position));

        InsertRecursive(_root, position, voxel);
    }

    private void InsertRecursive(OctreeNode node, Vector3Int position, Voxel voxel)
    {
        // 如果当前节点深度等于目标LOD级别，设置为叶子节点
        if (node.Depth == voxel.LODLevel)
        {
            node.Voxel = voxel;
            return;
        }

        // 如果需要分裂节点
        if (node.IsLeaf)
        {
            // 保存当前体素并分裂
            var currentVoxel = node.Voxel;
            node.Split();
            node.Voxel = null;

            // 为子节点创建LOD变体
            if (currentVoxel != null)
            {
                foreach (var child in node.GetChildren())
                {
                    if (child != null)
                    {
                        var lodVariant = new Voxel(currentVoxel.MaterialId, child.Depth);
                        currentVoxel.AddLodVariant(child.Depth, lodVariant);
                        child.Voxel = lodVariant;
                    }
                }
            }
        }

        // 确定子节点索引
        int childIndex = node.GetChildIndex(position);
        var childNode = node.GetChild(childIndex);

        // 如果子节点不存在则创建
        if (childNode == null)
        {
            var center = node.Center;
            Vector3Int childMin, childMax;

            // 根据索引计算子节点边界
            switch (childIndex)
            {
                case 0:
                    childMin = node.Min;
                    childMax = new Vector3Int(center.X, center.Y, center.Z);
                    break;
                case 1:
                    childMin = new Vector3Int(center.X, node.Min.Y, node.Min.Z);
                    childMax = new Vector3Int(node.Max.X, center.Y, center.Z);
                    break;
                case 2:
                    childMin = new Vector3Int(node.Min.X, center.Y, node.Min.Z);
                    childMax = new Vector3Int(center.X, node.Max.Y, center.Z);
                    break;
                case 3:
                    childMin = new Vector3Int(center.X, center.Y, node.Min.Z);
                    childMax = new Vector3Int(node.Max.X, node.Max.Y, center.Z);
                    break;
                case 4:
                    childMin = new Vector3Int(node.Min.X, node.Min.Y, center.Z);
                    childMax = new Vector3Int(center.X, center.Y, node.Max.Z);
                    break;
                case 5:
                    childMin = new Vector3Int(center.X, node.Min.Y, center.Z);
                    childMax = new Vector3Int(node.Max.X, center.Y, node.Max.Z);
                    break;
                case 6:
                    childMin = new Vector3Int(node.Min.X, center.Y, center.Z);
                    childMax = new Vector3Int(center.X, node.Max.Y, node.Max.Z);
                    break;
                case 7:
                    childMin = center;
                    childMax = node.Max;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            childNode = new OctreeNode(childMin, childMax, node.Depth + 1);
            node.SetChild(childIndex, childNode);
        }

        // 递归插入到子节点
        InsertRecursive(childNode, position, voxel);
    }

    /// <summary>
    /// 获取指定位置和LOD级别的体素
    /// </summary>
    public Voxel GetVoxel(Vector3Int position, int lodLevel)
    {
        if (lodLevel < 0 || lodLevel > _maxLODLevel)
            throw new ArgumentException($"LOD级别必须在0到{_maxLODLevel}之间", nameof(lodLevel));
        if (!_root.ContainsPoint(position))
            return null;

        return GetVoxelRecursive(_root, position, lodLevel);
    }

    private Voxel GetVoxelRecursive(OctreeNode node, Vector3Int position, int targetLOD)
    {
        // 如果达到目标LOD级别
        if (node.Depth == targetLOD)
        {
            return node.Voxel;
        }

        // 如果是叶子节点但还没到目标LOD级别，尝试获取变体
        if (node.IsLeaf)
        {
            return node.Voxel?.GetLodVariant(targetLOD);
        }

        // 获取子节点
        int childIndex = node.GetChildIndex(position);
        var childNode = node.GetChild(childIndex);

        // 如果子节点不存在，返回null
        if (childNode == null)
        {
            return null;
        }

        // 递归查询
        return GetVoxelRecursive(childNode, position, targetLOD);
    }

    /// <summary>
    /// 更新指定位置的体素
    /// </summary>
    public bool UpdateVoxel(Vector3Int position, Voxel newVoxel)
    {
        if (newVoxel == null)
            throw new ArgumentNullException(nameof(newVoxel));
        if (newVoxel.LODLevel > _maxLODLevel)
            throw new ArgumentException($"LOD级别不能超过最大值 {_maxLODLevel}", nameof(newVoxel));

        return UpdateVoxelRecursive(_root, position, newVoxel);
    }

    private bool UpdateVoxelRecursive(OctreeNode node, Vector3Int position, Voxel newVoxel)
    {
        // 如果是叶子节点且包含目标位置
        if (node.IsLeaf && node.ContainsPoint(position) && node.Depth == newVoxel.LODLevel)
        {
            node.Voxel = newVoxel;
            return true;
        }

        // 如果达到目标深度但不是叶子节点
        if (node.Depth == newVoxel.LODLevel)
        {
            return false;
        }

        // 获取子节点
        int childIndex = node.GetChildIndex(position);
        var childNode = node.GetChild(childIndex);

        // 子节点不存在
        if (childNode == null)
        {
            return false;
        }

        // 递归更新
        return UpdateVoxelRecursive(childNode, position, newVoxel);
    }

    /// <summary>
    /// 简化八叉树到指定LOD级别
    /// </summary>
    public void SimplifyToLOD(int lodLevel)
    {
        if (lodLevel < 0 || lodLevel > _maxLODLevel)
            throw new ArgumentException($"LOD级别必须在0到{_maxLODLevel}之间", nameof(lodLevel));

        SimplifyRecursive(_root, lodLevel);
    }

    private void SimplifyRecursive(OctreeNode node, int targetLOD)
    {
        // 如果达到目标LOD级别
        if (node.Depth == targetLOD)
        {
            // 检查是否有子节点
            bool hasChildren = false;
            foreach (var child in node.GetChildren())
            {
                if (child != null)
                {
                    hasChildren = true;
                    break;
                }
            }

            if (hasChildren)
            {
                // 合并子节点并创建新体素
                var mergedVoxel = MergeChildren(node, targetLOD);
                node.Merge();
                node.Voxel = mergedVoxel;
            }
            return;
        }

        // 递归简化子节点
        for (int i = 0; i < 8; i++)
        {
            var child = node.GetChild(i);
            if (child != null)
            {
                SimplifyRecursive(child, targetLOD);
            }
        }
    }

    /// <summary>
    /// 合并子节点为一个体素
    /// </summary>
    private Voxel MergeChildren(OctreeNode parentNode, int lodLevel)
    {
        // 简单的合并策略：使用出现次数最多的材质
        var materialCount = new Dictionary<int, int>();
        int totalVoxels = 0;

        foreach (var child in parentNode.GetChildren())
        {
            if (child != null && child.IsLeaf && child.Voxel != null)
            {
                totalVoxels++;
                int materialId = child.Voxel.MaterialId;

                if (materialCount.ContainsKey(materialId))
                    materialCount[materialId]++;
                else
                    materialCount[materialId] = 1;
            }
        }

        // 如果没有体素，返回空
        if (totalVoxels == 0)
            return null;

        // 找到最常见的材质
        int mostCommonMaterial = 0;
        int maxCount = 0;
        foreach (var kvp in materialCount)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                mostCommonMaterial = kvp.Key;
            }
        }

        // 创建合并后的体素
        var mergedVoxel = new Voxel(mostCommonMaterial, lodLevel);

        // 添加子节点的LOD变体
        foreach (var child in parentNode.GetChildren())
        {
            if (child != null && child.IsLeaf && child.Voxel != null)
            {
                mergedVoxel.AddLodVariant(child.Depth, child.Voxel);
            }
        }

        return mergedVoxel;
    }

    /// <summary>
    /// 获取指定LOD级别的体素大小
    /// </summary>
    public float GetVoxelSize(int lodLevel)
    {
        if (lodLevel < 0 || lodLevel > _maxLODLevel)
            throw new ArgumentException($"LOD级别必须在0到{_maxLODLevel}之间", nameof(lodLevel));

        // 计算根节点大小
        float rootSize = Math.Max(
            RootMax.X - RootMin.X,
            Math.Max(RootMax.Y - RootMin.Y, RootMax.Z - RootMin.Z)
        );

        // LOD级别越高，体素越大
        return rootSize / (float)Math.Pow(2, lodLevel);
    }
}
