using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

public interface IResettable
{
    void Reset();
}

// 分段子池中的单个分段（每个分段对应一组线程）
class ObjectPoolShard
{
    // 存储可复用对象的队列（非线程安全，需加锁访问）
    private readonly Queue<object> _objects = new Queue<object>();
    // 分段内的锁（细粒度锁，仅本分段竞争）
    private readonly object _lock = new object();

    // 从分段获取对象（无则通过工厂创建）
    public object Rent(Func<object> objectFactory)
    {
        lock (_lock)
        {
            if (_objects.Count > 0)
            {
                return _objects.Dequeue();
            }
        }
        // 队列空时，创建新对象（在锁外创建，减少锁持有时间）
        return objectFactory();
    }

    // 将对象归还给分段
    public void Return(object obj)
    {
        lock (_lock)
        {
            _objects.Enqueue(obj);
        }
    }
}

// 某一类型专属的分段子池
class ShardedSubPool
{
    // 该子池管理的对象类型
    public Type ObjectType { get; }
    // 分段数组（每个分段对应一个线程哈希桶）
    private readonly ObjectPoolShard[] _shards;
    // 创建该类型对象的委托（缓存反射构造，避免重复反射开销）
    private readonly Func<object> _objectFactory;

    public ShardedSubPool(Type objectType, int shardCount)
    {
        ObjectType = objectType;
        _shards = new ObjectPoolShard[shardCount];
        for (int i = 0; i < shardCount; i++)
        {
            _shards[i] = new ObjectPoolShard();
        }

        // 缓存构造函数：要求类型有公开无参构造函数（或通过其他方式注入）
        var ctor = objectType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
        if (ctor == null)
        {
            throw new InvalidOperationException($"类型 {objectType} 必须有公开无参构造函数");
        }
        _objectFactory = () => ctor.Invoke(null); // 反射创建对象的委托
    }

    // 从当前线程对应的分段获取对象
    public object Rent()
    {
        int shardIndex = GetShardIndexForCurrentThread();
        return _shards[shardIndex].Rent(_objectFactory);
    }

    // 将对象归还给当前线程对应的分段
    public void Return(object obj)
    {
        // 归还前重置对象状态
        if (obj is IResettable resettable)
        {
            resettable.Reset();
        }
        int shardIndex = GetShardIndexForCurrentThread();
        _shards[shardIndex].Return(obj);
    }

    // 基于当前线程ID哈希到分段索引
    private int GetShardIndexForCurrentThread()
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;
        return threadId % _shards.Length;
    }
}

// 支持所有可池化类型的通用分段锁对象池
public class ShardedObjectPool
{
    // 核心：类型→该类型的分段子池（线程安全字典）
    private readonly ConcurrentDictionary<Type, ShardedSubPool> _typeToSubPools;
    // 每个类型的子池包含的分段数量（建议与CPU核心数一致）
    private readonly int _shardCount;

    // 初始化通用对象池
    // <param name="shardCount">每个类型的子池分段数量（默认=CPU核心数）</param>
    public ShardedObjectPool(int shard_count = 0)
    {
        _shardCount = shard_count > 0 ? shard_count : Environment.ProcessorCount;
        _typeToSubPools = new ConcurrentDictionary<Type, ShardedSubPool>();
    }

    // 获取指定类型的对象
    public T Get<T>() where T : class, IResettable, new()
    {
        Type type = typeof(T);

        var sub_pool = _typeToSubPools.GetOrAdd(type, t => new ShardedSubPool(t, _shardCount));

        return (T)sub_pool.Rent();
    }

    // 归还对象到池（自动根据对象类型找到对应子池）
    public void Free(object obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        Type type = obj.GetType();
        // 查找该类型的子池（未找到说明对象不是从池获取的，直接忽略或抛异常）
        if (_typeToSubPools.TryGetValue(type, out var subPool))
        {
            subPool.Return(obj);
        }
        else
        {
            // 可选：抛异常或日志警告（防止归还非池化对象）
            // throw new InvalidOperationException($"对象 {obj} 不是从当前池获取的，无法归还");
        }
    }
}
