using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

public class Mongo
{
    MongoClient MongoClient { get; set; }
    IMongoDatabase Db { get; set; }

    private static readonly string _connectionString = "mongodb://localhost:27017";
    private static readonly string _databaseName = "Test";
    private static readonly string _collectionName = "Users";

    public async Task ConnectAsync()
    {
        await Task.Delay(1);

        MongoClient = new MongoClient(_connectionString);
        Db = MongoClient.GetDatabase(_databaseName);

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
}
