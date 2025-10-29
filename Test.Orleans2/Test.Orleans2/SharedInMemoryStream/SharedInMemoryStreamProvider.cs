using Orleans;
using Orleans.Configuration;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

public class SharedInMemoryStreamProvider : IStreamProvider
{
    private readonly InMemoryStreamProvider _innerProvider;
    private readonly LocalMessageCache _localCache;

    public SharedInMemoryStreamProvider(InMemoryStreamProvider innerProvider, LocalMessageCache localCache)
    {
        _innerProvider = innerProvider;
        _localCache = localCache;
    }

    // 获取流时返回自定义包装流
    public IAsyncStream<T> GetStream<T>(StreamId streamId)
    {
        var innerStream = _innerProvider.GetStream<T>(streamId);
        return new SharedInMemoryAsyncStream<T>(innerStream, _localCache, streamId);
    }

    // 初始化与关闭委托给内部提供者
    public Task InitAsync(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        => _innerProvider.InitAsync(name, providerRuntime, config);

    public Task CloseAsync() => _innerProvider.CloseAsync();

    public string Name => _innerProvider.Name;
}