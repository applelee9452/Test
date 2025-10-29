//// 1. 定义可池化的对象（实现IResettable）
//using System.Collections.Generic;
//using System.Threading.Tasks;

//public class GameEntity : IResettable
//{
//    public int Id { get; set; }
//    public string Name { get; set; } = string.Empty;
//    public Dictionary<string, object> Properties { get; set; } = new();

//    // 重置状态至初始值
//    public void Reset()
//    {
//        Id = 0;
//        Name = string.Empty;
//        Properties.Clear();
//    }
//}

//// 2. 在Orleans中注册为单例服务（Silo级共享）
//public class OrleansSiloConfig
//{
//    public void Configure(ISiloBuilder siloBuilder)
//    {
//        siloBuilder.ConfigureServices(services =>
//        {
//            // 注册游戏实体对象池（8个子池，每个最大1000个对象）
//            services.AddSingleton<ShardedObjectPool<GameEntity>>(
//                new ShardedObjectPool<GameEntity>(
//                    shardCount: 8,
//                    maxCapacityPerShard: 1000
//                )
//            );
//        });
//    }
//}

//// 3. 在Actor中使用对象池
//public class GameActor : Grain, IGameActor
//{
//    private readonly ShardedObjectPool<GameEntity> _entityPool;

//    // 构造函数注入对象池
//    public GameActor(ShardedObjectPool<GameEntity> entityPool)
//    {
//        _entityPool = entityPool;
//    }

//    public async Task UpdateEntity(int entityId)
//    {
//        // 借出对象
//        var entity = _entityPool.Rent();
//        try
//        {
//            // 业务操作
//            entity.Id = entityId;
//            entity.Name = $"Entity_{entityId}";
//            entity.Properties["health"] = 100;

//            await SaveEntity(entity); // 模拟持久化
//        }
//        finally
//        {
//            // 确保归还（即使发生异常）
//            _entityPool.Return(entity);
//        }
//    }

//    private Task SaveEntity(GameEntity entity)
//    {
//        // 模拟数据库操作
//        return Task.CompletedTask;
//    }
//}