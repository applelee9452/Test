using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Streams;
using System;

public static class SharedStreamProviderExtensions
{
    /// <summary>
    /// 注册支持同一Silo消息共享的内存流提供者
    /// </summary>
    public static ISiloBuilder AddSharedInMemoryStreamProvider(
        this ISiloBuilder builder,
        string name,
        Action<InMemoryStreamOptions> configureOptions = null)
    {
        return builder
            // 先注册内置内存流提供者（作为基础）
            .AddInMemoryStreamProvider(name, configureOptions)
            // 注册本地缓存
            .ConfigureServices(services => services.AddSingleton<LocalMessageCache>())
            // 替换为自定义提供者
            .ConfigureServices(services =>
            {
                services.AddKeyedSingleton<IStreamProvider>(name, (sp, key) =>
                {
                    // 获取内置的InMemoryStreamProvider
                    var innerProvider = sp.GetKeyedService<IStreamProvider>(name) as InMemoryStreamProvider
                        ?? throw new InvalidOperationException("内置内存流提供者未找到");
                    var cache = sp.GetRequiredService<LocalMessageCache>();
                    return new SharedInMemoryStreamProvider(innerProvider, cache);
                });
            });
    }
}