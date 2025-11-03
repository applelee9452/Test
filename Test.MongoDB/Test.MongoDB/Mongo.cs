using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

// 实体类（对应MongoDB中的"Users"集合）
public class User
{
    // 指定主键（MongoDB默认使用"_id"字段，这里映射为字符串类型）
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)] // 允许用字符串传递ObjectId
    public string Id { get; set; }

    public string Name { get; set; } // 姓名

    public int Age { get; set; } // 年龄

    public string Email { get; set; } // 邮箱

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)] // 存储本地时间
    public DateTime CreateTime { get; set; } = DateTime.Now; // 创建时间
}

// 元数据类（不常变化的信息，用于分组）
public class SensorMetadata
{
    public string SensorId { get; set; } // 传感器唯一标识
    public string DeviceName { get; set; } // 设备名称
    public string Location { get; set; } // 安装位置
}

public class SensorReading
{
    public ObjectId Id { get; set; }

    // 字段名必须与TimeSeriesOptions.timeField一致（此处为"Timestamp"）
    public DateTime Timestamp { get; set; }

    public SensorMetadata Metadata { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
}

public class Mongo
{
    MongoClient MongoClient { get; set; }
    IMongoDatabase Db { get; set; }

    private static readonly string _connectionString = "mongodb://localhost:27017";
    private static readonly string _databaseName = "Test";

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1);

        MongoClient = new MongoClient(_connectionString);
        Db = MongoClient.GetDatabase(_databaseName);

        {
            string _collectionName = "Users";

            var user_collection = Db.GetCollection<User>(_collectionName);

            var newUser = new User
            {
                Name = "张三",
                Age = 28,
                Email = "zhangsan@example.com"
            };
            await user_collection.InsertOneAsync(newUser);

            var allUsers = await user_collection.Find(_ => true).ToListAsync();

            allUsers.ForEach(u => Console.WriteLine($"- {u.Id}：{u.Name}（{u.Age}岁）"));

            var targetId = newUser.Id;
            var userById = await user_collection.Find(u => u.Id == targetId).FirstOrDefaultAsync();

            if (userById != null)
            {
                Console.WriteLine($"\n查询ID为{targetId}的用户：{userById.Name}，邮箱：{userById.Email}");
            }

            var filter = Builders<User>.Filter.Gt(u => u.Age, 25); // Gt = Greater Than
            var usersOver25 = await user_collection.Find(filter).ToListAsync();

            Console.WriteLine("\n年龄大于25的用户：");
            usersOver25.ForEach(u => Console.WriteLine($"- {u.Name}（{u.Age}岁）"));

            var updateFilter = Builders<User>.Filter.Eq(u => u.Id, targetId); // Eq = Equal
            var updateDefinition = Builders<User>.Update
                .Set(u => u.Age, 29)
                .Set(u => u.Email, "zhangsan_updated@example.com");
            var updateResult = await user_collection.UpdateOneAsync(updateFilter, updateDefinition);
            Console.WriteLine($"\n更新结果：影响行数 {updateResult.ModifiedCount}");

            //var deleteFilter = Builders<User>.Filter.Eq(u => u.Id, targetId);
            //var deleteResult = await user_collection.DeleteOneAsync(deleteFilter);

            //Console.WriteLine($"\n删除结果：影响行数 {deleteResult.DeletedCount}");
        }

        {
            string _collectionName1 = "Events";

            var collectionsCursor = await Db.ListCollectionNamesAsync();

            var allCollectionNames = await collectionsCursor.ToListAsync();

            var collectionExists = allCollectionNames.Any(c => c == _collectionName1);

            if (!collectionExists)
            {
                var timeSeriesOptions = new TimeSeriesOptions(
                    timeField: "Timestamp", // 必须：时间字段名（与实体属性一致）
                    metaField: "Metadata", // 可选：元数据字段名（直接传字符串，隐式转为Optional<string>）
                    granularity: TimeSeriesGranularity.Minutes // 可选：粒度（直接传枚举值，隐式转为Optional<TimeSeriesGranularity?>）
                );

                await Db.CreateCollectionAsync(
                    name: _collectionName1,
                    options: new CreateCollectionOptions<SensorReading>
                    {
                        TimeSeriesOptions = timeSeriesOptions,
                        ExpireAfter = TimeSpan.FromDays(30) // 数据保留30天
                    }
                );
            }

            // 获取时序集合
            var collection = Db.GetCollection<SensorReading>(_collectionName1);

            // 3. 写入单条事件数据
            var singleReading = new SensorReading
            {
                Timestamp = DateTime.UtcNow,
                Metadata = new SensorMetadata
                {
                    SensorId = "sensor_001",
                    DeviceName = "环境监测终端A",
                    Location = "办公室301"
                },
                Temperature = 25.6,
                Humidity = 45.2,
                Pressure = 1013.2
            };
            await collection.InsertOneAsync(singleReading);
            Console.WriteLine($"单条数据写入成功，ID：{singleReading.Id}");


            // 4. 批量写入事件数据（模拟10条连续时间的读数）
            var batchReadings = new List<SensorReading>();
            for (int i = 0; i < 10; i++)
            {
                batchReadings.Add(new SensorReading
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(i), // 时间依次递增1分钟
                    Metadata = new SensorMetadata
                    {
                        SensorId = "sensor_001", // 同一传感器
                        DeviceName = "环境监测终端A",
                        Location = "办公室301"
                    },
                    Temperature = 25.6 + (i * 0.1), // 温度轻微波动
                    Humidity = 45.2 - (i * 0.05),
                    Pressure = 1013.2 + (i * 0.02)
                });
            }
            await collection.InsertManyAsync(batchReadings);
            Console.WriteLine($"批量写入成功，共{batchReadings.Count}条数据");


            // 5. 时序数据查询示例（查询最近10分钟的传感器数据）
            var now = DateTime.Now;
            var tenMinutesAgo = now.AddMinutes(-10);

            // 过滤条件：时间在10分钟内，且传感器ID为sensor_001
            var filter1 = Builders<SensorReading>.Filter.And(
                Builders<SensorReading>.Filter.Gte(r => r.Timestamp, tenMinutesAgo), // 大于等于起始时间
                Builders<SensorReading>.Filter.Lte(r => r.Timestamp, now), // 小于等于当前时间
                Builders<SensorReading>.Filter.Eq(r => r.Metadata.SensorId, "sensor_001") // 匹配传感器ID
            );

            // 按时间升序排序
            var sort = Builders<SensorReading>.Sort.Ascending(r => r.Timestamp);

            // 执行查询
            var recentReadings = await collection.Find(filter1).Sort(sort).ToListAsync();

            //Console.WriteLine($"\n最近10分钟的传感器数据（共{recentReadings.Count}条）：");
            //foreach (var reading in recentReadings)
            //{
            //    Console.WriteLine(
            //        $"{reading.Timestamp:yyyy-MM-dd HH:mm:ss} | " +
            //        $"温度：{reading.Temperature}℃ | " +
            //        $"湿度：{reading.Humidity}% | " +
            //        $"气压：{reading.Pressure}hPa"
            //    );
            //}
        }

    }
}
