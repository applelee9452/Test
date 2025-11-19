using System;

/// <summary>
/// 稀疏体素八叉树节点
/// </summary>
public class SvoNode
{
    // 节点空间信息
    public Vector3d Position; // 节点中心点坐标（相对于SVO分块原点）
    public double Size; // 节点立方体的边长（米）
    public int LodLevel; // LOD级别（1-9）
    public SvoNodeType NodeType; // 节点类型

    // 子节点（仅内部节点有，稀疏存储非空节点）
    private SvoNode[] _children; // 索引0-7对应八叉树8个象限

    // 体素数据（仅叶子节点有）
    public VoxelData? Voxel; // 可为null（表示空体素，如空气）

    public SvoNode(Vector3d position, double size, int lodLevel)
    {
        Position = position;
        Size = size;
        LodLevel = lodLevel;
        NodeType = SvoNodeType.Internal; // 默认内部节点，可细分
        _children = new SvoNode[EarthSvoConstants.NodeChildCount];
    }

    /// <summary>
    /// 获取子节点（延迟初始化，只创建非空节点）
    /// </summary>
    public SvoNode GetChild(int index)
    {
        if (index < 0 || index >= EarthSvoConstants.NodeChildCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_children[index] == null)
        {
            // 计算子节点的位置和尺寸（子节点尺寸为父节点的1/2）
            double childSize = Size / 2;
            var offset = GetChildOffset(index, childSize); // 计算象限偏移
            _children[index] = new SvoNode(Position + offset, childSize, LodLevel + 1);
        }
        return _children[index];
    }

    /// <summary>
    /// 计算子节点相对于父节点的偏移量（8个象限）
    /// </summary>
    private Vector3d GetChildOffset(int index, double childSize)
    {
        // 八叉树8个象限的X/Y/Z偏移（±childSize/2）
        var offsets = new[]
        {
            new Vector3d(-childSize/2, -childSize/2, -childSize/2), // 0: 左下后
            new Vector3d(childSize/2, -childSize/2, -childSize/2),  // 1: 右下后
            new Vector3d(-childSize/2, childSize/2, -childSize/2),  // 2: 左上前
            new Vector3d(childSize/2, childSize/2, -childSize/2),   // 3: 右上前
            new Vector3d(-childSize/2, -childSize/2, childSize/2),  // 4: 左下前
            new Vector3d(childSize/2, -childSize/2, childSize/2),   // 5: 右下前
            new Vector3d(-childSize/2, childSize/2, childSize/2),   // 6: 左上前
            new Vector3d(childSize/2, childSize/2, childSize/2)     // 7: 右上前
        };
        return offsets[index];
    }

    /// <summary>
    /// 检查节点是否为叶子节点（达到最大LOD或手动设置为叶子）
    /// </summary>
    public bool IsLeaf() => NodeType == SvoNodeType.Leaf || LodLevel >= EarthSvoConstants.LodMaxLevel;

    /// <summary>
    /// 将节点标记为叶子节点并设置体素数据（停止细分）
    /// </summary>
    public void SetAsLeaf(VoxelData voxel)
    {
        NodeType = SvoNodeType.Leaf;
        Voxel = voxel;
    }
}