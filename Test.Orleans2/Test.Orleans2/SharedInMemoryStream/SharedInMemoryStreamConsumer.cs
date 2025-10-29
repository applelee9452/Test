using Microsoft.Orleans.Streams; // 命名空间调整：Orleans.Streams → Microsoft.Orleans.Streams
using Microsoft.Orleans.Streams.InMemory; // 内存流令牌所在命名空间（关键）
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class SharedInMemoryAsyncStream<T> : IAsyncStream<T>
{
    private readonly IAsyncStream<T> _innerStream;
    private readonly LocalMessageCache _localCache;
    private readonly StreamId _streamId;

    public SharedInMemoryAsyncStream(IAsyncStream<T> innerStream, LocalMessageCache localCache, StreamId streamId)
    {
        _innerStream = innerStream;
        _localCache = localCache;
        _streamId = streamId;
    }

    // 核心：订阅方法（适配 9.2.1 重载和令牌类型）
    public async Task<StreamSubscriptionHandle<T>> SubscribeAsync(
        Func<T, StreamSequenceToken, CancellationToken, Task> onNextAsync,
        Func<Exception, CancellationToken, Task> onErrorAsync = null,
        Func<CancellationToken, Task> onCompletedAsync = null,
        StreamSequenceToken? token = null,
        CancellationToken cancellationToken = default)
    {
        Func<T, StreamSequenceToken, CancellationToken, Task> wrappedOnNext = async (item, seqToken, ct) =>
        {
            // 关键：使用 Microsoft.Orleans.Streams.InMemory 命名空间下的 InMemoryStreamSequenceToken
            if (seqToken is InMemoryStreamSequenceToken inMemoryToken)
            {
                if (!_localCache.TryGet(_streamId, inMemoryToken.SequenceNumber, out var cachedItem))
                {
                    _localCache.TryAdd(_streamId, inMemoryToken.SequenceNumber, item);
                    cachedItem = item;
                }
                await onNextAsync((T)cachedItem, seqToken, ct);
            }
            else
            {
                await onNextAsync(item, seqToken, ct);
            }
        };

        // 适配 9.2.1 的 SubscribeAsync 重载（参数匹配）
        return await _innerStream.SubscribeAsync(
            onNext: wrappedOnNext,
            onError: onErrorAsync,
            onCompleted: onCompletedAsync,
            token: token,
            cancellationToken: cancellationToken);
    }

    // ------------------------------
    // 所有接口成员委托给_innerStream（确保完整实现）
    // ------------------------------
    public StreamId StreamId => _innerStream.StreamId;
    public bool IsRewindable => _innerStream.IsRewindable;
    public string ProviderName => _innerStream.ProviderName;

    public Task<IEnumerable<StreamSubscriptionHandle<T>>> GetAllSubscriptionHandles()
        => _innerStream.GetAllSubscriptionHandles();

    public bool Equals(IAsyncStream<T>? other)
        => _innerStream.Equals(other);

    public int CompareTo(IAsyncStream<T>? other)
        => _innerStream.CompareTo(other);

    public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer)
        => _innerStream.SubscribeAsync(observer);

    public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncObserver<T> observer, StreamSequenceToken? token, string? filterData)
        => _innerStream.SubscribeAsync(observer, token, filterData);

    public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncBatchObserver<T> observer)
        => _innerStream.SubscribeAsync(observer);

    public Task<StreamSubscriptionHandle<T>> SubscribeAsync(IAsyncBatchObserver<T> observer, StreamSequenceToken? token)
        => _innerStream.SubscribeAsync(observer, token);

    public Task OnNextBatchAsync(IEnumerable<T> batch, StreamSequenceToken token)
        => _innerStream.OnNextBatchAsync(batch, token);

    public Task OnNextAsync(T item, StreamSequenceToken? token = null, CancellationToken cancellationToken = default)
        => _innerStream.OnNextAsync(item, token, cancellationToken);

    public Task OnErrorAsync(Exception error, CancellationToken cancellationToken = default)
        => _innerStream.OnErrorAsync(error, cancellationToken);

    public Task OnCompletedAsync(CancellationToken cancellationToken = default)
        => _innerStream.OnCompletedAsync(cancellationToken);

    public override bool Equals(object? obj)
        => _innerStream.Equals(obj);

    public override int GetHashCode()
        => _innerStream.GetHashCode();

    public override string ToString()
        => _innerStream.ToString();
}