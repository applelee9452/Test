public class MyPooledObject : IResettable
{
    public int Value { get; set; }

    public void Reset()
    {
        Value = 0; // 重置状态
    }
}

public class AnotherPooledObject : IResettable
{
    public string Name { get; set; }
    public void Reset() => Name = null;
}

public class TestC
{
    public void Test()
    {
        var pool = new ShardedObjectPool();

        var obj1 = pool.Rent<MyPooledObject>();
        obj1.Value = 100;
        pool.Return(obj1);

        var obj2 = pool.Rent<AnotherPooledObject>();
        obj2.Name = "test";
        pool.Return(obj2);
    }
}
