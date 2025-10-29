using Orleans.Runtime;
using System;
using System.Collections.Concurrent;
using System.IO;

/// <summary>
/// 同一Silo内的消息缓存，key为流ID+消息序号（确保唯一性）
/// </summary>
internal class LocalMessageCache
{
    // 缓存容器：StreamId -> (SequenceNumber -> 反序列化后的消息对象)
    private readonly ConcurrentDictionary<StreamId, ConcurrentDictionary<long, object>> _cache = new();

    /// <summary>
    /// 尝试添加消息到缓存（仅当不存在时）
    /// </summary>
    public bool TryAdd(StreamId streamId, long sequenceNumber, object message)
    {
        var streamCache = _cache.GetOrAdd(streamId, _ => new ConcurrentDictionary<long, object>());
        return streamCache.TryAdd(sequenceNumber, message);
    }

    /// <summary>
    /// 从缓存获取消息
    /// </summary>
    public bool TryGet(StreamId streamId, long sequenceNumber, out object message)
    {
        message = null;
        if (_cache.TryGetValue(streamId, out var streamCache))
        {
            return streamCache.TryGetValue(sequenceNumber, out message);
        }
        return false;
    }

    /// <summary>
    /// 清理过期缓存（可选：避免内存泄漏）
    /// </summary>
    public void Cleanup(TimeSpan maxLifetime)
    {
        // 实际场景可定时清理超过生命周期的消息（此处简化实现）
    }
}