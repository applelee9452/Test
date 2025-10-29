using System;
using System.Collections.Generic;
using System.Threading;

// 可重置对象接口（确保复用前状态清零）
public interface IResettable
{
    void Reset();
}

// 分段锁对象池核心实现
public class ShardedObjectPool1<T> where T : class, IResettable, new()
{
    // 子池结构：包含对象队列、同步锁、容量限制
    private class PoolShard
    {
        internal readonly Queue<T> Objects = new();
        internal readonly object SyncLock = new();
        internal int MaxCapacity;
    }

    private readonly PoolShard[] _shards;
    private readonly int _shardCount;

    // 初始化分段锁对象池
    // <param name="shardCount">子池数量（默认等于CPU核心数）</param>
    // <param name="maxCapacityPerShard">每个子池最大容量</param>
    public ShardedObjectPool1(int? shard_count = null, int max_capacity_per_shard = 1000)
    {
        // 子池数量默认使用CPU核心数（平衡竞争与内存占用）
        _shardCount = shard_count ?? Environment.ProcessorCount;
        _shards = new PoolShard[_shardCount];

        // 初始化所有子池
        for (int i = 0; i < _shardCount; i++)
        {
            _shards[i] = new PoolShard
            {
                MaxCapacity = max_capacity_per_shard
            };
        }
    }

    // 从池中借出对象
    public T Get()
    {
        // 根据当前线程ID哈希到固定子池（确保线程与子池稳定绑定）
        var shard = GetShardForCurrentThread();

        // 先尝试从本地子池获取
        lock (shard.SyncLock)
        {
            if (shard.Objects.Count > 0)
            {
                return shard.Objects.Dequeue();
            }
        }

        // 本地子池为空时，尝试跨子池补充（可选逻辑，降低新建频率）
        var crossShardItem = TryStealFromOtherShards();
        if (crossShardItem != null)
        {
            return crossShardItem;
        }

        // 所有子池都为空，新建对象
        return new T();
    }

    // 归还对象到池中
    public void Free(T item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        // 归还前重置对象状态（避免状态污染）
        item.Reset();

        // 归还给当前线程绑定的子池
        var shard = GetShardForCurrentThread();
        lock (shard.SyncLock)
        {
            // 子池未满时才存储，避免内存溢出
            if (shard.Objects.Count < shard.MaxCapacity)
            {
                shard.Objects.Enqueue(item);
            }
        }
    }

    // 清理所有子池中的对象（用于测试或资源释放）
    public void Clear()
    {
        foreach (var shard in _shards)
        {
            lock (shard.SyncLock)
            {
                shard.Objects.Clear();
            }
        }
    }

    // 获取当前线程绑定的子池
    PoolShard GetShardForCurrentThread()
    {
        // 基于线程ID哈希（.NET 9中Thread.ManagedThreadId是稳定的）
        int threadId = Thread.CurrentThread.ManagedThreadId;
        int shardIndex = threadId % _shardCount;
        return _shards[shardIndex];
    }

    // 尝试从其他子池"窃取"一个对象（低频操作，减少竞争）
    T TryStealFromOtherShards()
    {
        // 随机检查几个子池（而非全部，减少竞争范围）
        for (int i = 0; i < Math.Min(3, _shardCount - 1); i++)
        {
            int randomShardIndex = Random.Shared.Next(_shardCount);
            var shard = _shards[randomShardIndex];

            lock (shard.SyncLock)
            {
                if (shard.Objects.Count > 0)
                {
                    return shard.Objects.Dequeue();
                }
            }
        }

        return null;
    }
}