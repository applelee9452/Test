using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class AttrSet
{
    // 属性数据类
    class AttrData
    {
        public string Name { get; set; }
        public float BaseValue { get; set; }
        public float CurrentValue { get; set; }
        public bool IsDirty { get; set; }
        public List<AttrModifier> Modifiers { get; set; }

        public AttrData(string name, float base_value)
        {
            Name = name;
            BaseValue = base_value;
            CurrentValue = base_value;
            IsDirty = true;
            Modifiers = new List<AttrModifier>();
        }
    }

    public Action<string, float, float> OnAttrChanged { get; set; }// 属性变化事件（Unity 兼容委托）
    Dictionary<string, AttrData> DicAttr { get; set; } = new();// 存储所有属性（成员变量首字母大写 + GetSet）
    Dictionary<string, HashSet<string>> DicDependencies { get; set; } = new();// 依赖关系（成员变量首字母大写 + GetSet）
    Dictionary<string, List<IAttrObserver>> DicObservers { get; set; } = new();// 观察者（成员变量首字母大写 + GetSet）
    HashSet<string> HashDirtyAttr { get; set; } = new();// 脏属性集合（成员变量首字母大写 + GetSet）

    // 添加属性
    public void AddAttr(string name, float base_value = 0)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("属性名称不能为空", nameof(name));
        }

        if (!DicAttr.ContainsKey(name))
        {
            DicAttr[name] = new AttrData(name, base_value);
            DicDependencies[name] = new HashSet<string>();
            MarkDirty(name);
        }
    }

    // 设置属性基础值
    public void SetBaseValue(string name, float value)
    {
        if (!DicAttr.ContainsKey(name))
        {
            AddAttr(name, value);
            return;
        }

        var attr_data = DicAttr[name];
        if (attr_data.BaseValue != value)
        {
            attr_data.BaseValue = value;
            MarkDirty(name);
        }
    }

    // 添加修饰器
    public void AddModifier(AttrModifier modifier)
    {
        if (modifier == null)
        {
            throw new ArgumentNullException(nameof(modifier));
        }

        string target_attr = modifier.TargetAttr;
        AddAttr(target_attr);

        DicAttr[target_attr].Modifiers.Add(modifier);
        modifier.Initialize(this);

        MarkDirty(target_attr);
    }

    // 移除修饰器
    public bool RemoveModifier(AttrModifier modifier)
    {
        if (modifier == null)
            return false;

        string target_attr = modifier.TargetAttr;
        if (!DicAttr.TryGetValue(target_attr, out var attr_data))
            return false;

        if (attr_data.Modifiers.Remove(modifier))
        {
            modifier.Cleanup(this);
            MarkDirty(target_attr);
            return true;
        }

        return false;
    }

    // 获取属性值
    public float GetValue(string name)
    {
        if (!DicAttr.ContainsKey(name))
        {
            throw new KeyNotFoundException($"属性 '{name}' 不存在");
        }

        // 脏属性检查与更新
        if (DicAttr[name].IsDirty || HashDirtyAttr.Contains(name))
        {
            UpdateAttr(name);
        }

        return DicAttr[name].CurrentValue;
    }

    // 标记属性为脏
    public void MarkDirty(string name)
    {
        if (!DicAttr.ContainsKey(name))
        {
            return;
        }

        if (!HashDirtyAttr.Contains(name))
        {
            HashDirtyAttr.Add(name);

            // 递归标记依赖属性为脏
            foreach (var attr in DicDependencies.Keys)
            {
                if (DicDependencies[attr].Contains(name))
                {
                    MarkDirty(attr);
                }
            }
        }
    }

    // 注册观察者
    public void RegisterObserver(string attr_name, IAttrObserver observer)
    {
        if (!DicObservers.ContainsKey(attr_name))
        {
            DicObservers[attr_name] = new List<IAttrObserver>();
        }

        if (!DicObservers[attr_name].Contains(observer))
        {
            DicObservers[attr_name].Add(observer);
        }
    }

    // 注销观察者
    public void UnregisterObserver(string attr_name, IAttrObserver observer)
    {
        if (DicObservers.ContainsKey(attr_name))
        {
            DicObservers[attr_name].Remove(observer);
        }
    }

    // 添加依赖关系
    public void AddDependency(string dependent_attr, string source_attr)
    {
        AddAttr(dependent_attr);
        AddAttr(source_attr);

        // 循环依赖检查
        if (WouldCreateCycle(dependent_attr, source_attr))
        {
            throw new InvalidOperationException($"添加依赖 {dependent_attr} -> {source_attr} 会导致循环依赖");
        }

        DicDependencies[dependent_attr].Add(source_attr);
    }

    // 移除依赖关系
    public void RemoveDependency(string dependent_attr, string source_attr)
    {
        if (DicDependencies.ContainsKey(dependent_attr))
        {
            DicDependencies[dependent_attr].Remove(source_attr);
        }
    }

    // 检查循环依赖
    private bool WouldCreateCycle(string dependent_attr, string source_attr)
    {
        if (dependent_attr == source_attr)
        {
            return true;
        }

        HashSet<string> visited = new();
        Queue<string> queue = new();
        queue.Enqueue(source_attr);
        visited.Add(source_attr);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();

            if (current == dependent_attr)
                return true;

            // 遍历当前节点的所有依赖
            if (DicDependencies.TryGetValue(current, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        visited.Add(dependency);
                        queue.Enqueue(dependency);
                    }
                }
            }
        }

        return false;
    }

    // 更新属性值
    private void UpdateAttr(string name)
    {
        if (!DicAttr.TryGetValue(name, out var attr_data))
        {
            return;
        }

        // 先更新所有依赖属性
        if (DicDependencies.TryGetValue(name, out var dependencies))
        {
            foreach (var dependency in dependencies.ToList())
            {
                UpdateAttr(dependency);
            }
        }

        // 计算新值
        float new_value = CalculateAttrValue(name);

        // 值变化检查与通知
        if (Math.Abs(attr_data.CurrentValue - new_value) > float.Epsilon)
        {
            float old_value = attr_data.CurrentValue;
            attr_data.CurrentValue = new_value;

            // 通知观察者
            NotifyObservers(name, old_value, new_value);
        }

        // 清除脏标记
        attr_data.IsDirty = false;
        HashDirtyAttr.Remove(name);
    }

    // 计算属性值
    private float CalculateAttrValue(string name)
    {
        var attr_data = DicAttr[name];
        float value = attr_data.BaseValue;

        // 按优先级排序修饰器
        var sorted_modifiers = attr_data.Modifiers.OrderBy(m => m.Priority).ToList();

        // 应用所有修饰器
        foreach (var modifier in sorted_modifiers)
        {
            value = modifier.Apply(value, this);
        }

        return value;
    }

    // 通知观察者
    private void NotifyObservers(string attr_name, float old_value, float new_value)
    {
        // 触发事件
        OnAttrChanged?.Invoke(attr_name, old_value, new_value);

        // 通知注册观察者
        if (DicObservers.TryGetValue(attr_name, out var observers))
        {
            foreach (var observer in observers.ToList())
            {
                observer.OnAttrChanged(attr_name, old_value, new_value);
            }
        }
    }
}

// 观察者接口
public interface IAttrObserver
{
    void OnAttrChanged(string attr_name, float old_value, float new_value);
}

// 修饰器基类
public abstract class AttrModifier
{
    // 成员变量首字母大写 + GetSet
    public string TargetAttr { get; set; }
    public int Priority { get; set; }

    public AttrModifier(string target_attr)
    {
        TargetAttr = target_attr ?? throw new ArgumentNullException(nameof(target_attr));
    }

    public abstract float Apply(float value, AttrSet attr_set);

    public virtual void Initialize(AttrSet set)
    {
    }

    public virtual void Cleanup(AttrSet set)
    {
    }
}

// 加法修饰器
public class AddModifier : AttrModifier
{
    // 成员变量首字母大写 + GetSet
    public float Value { get; set; }

    public AddModifier(string target_attr, float value)
    : base(target_attr)
    {
        Value = value;
        Priority = 10; // 加法优先级最低
    }

    public override float Apply(float value, AttrSet attr_set)
    {
        return value + Value;
    }
}

// 乘法修饰器
public class MultiplyModifier : AttrModifier
{
    // 成员变量首字母大写 + GetSet
    public float Value { get; set; }

    public MultiplyModifier(string target_attr, float value)
    : base(target_attr)
    {
        Value = value;
        Priority = 20; // 乘法在加法之后
    }

    public override float Apply(float value, AttrSet attr_set)
    {
        return value * Value;
    }
}

// 额外乘法修饰器
public class ExtraMultiplyModifier : AttrModifier
{
    // 成员变量首字母大写 + GetSet
    public float Value { get; set; }

    public ExtraMultiplyModifier(string target_attr, float value)
    : base(target_attr)
    {
        Value = value;
        Priority = 30; // 额外乘法在普通乘法之后
    }

    public override float Apply(float value, AttrSet attr_set)
    {
        return value * (1 + Value);
    }
}

// 同步修饰器
public class SyncModifier : AttrModifier, IAttrObserver
{
    // 成员变量首字母大写 + GetSet（原_dsource_attr/_set 修改）
    string SourceAttr { get; set; }
    AttrSet AttrSet { get; set; }

    public SyncModifier(string target_attr, string source_attr)
    : base(target_attr)
    {
        SourceAttr = source_attr ?? throw new ArgumentNullException(nameof(source_attr));
        Priority = 100; // 同步修饰器优先级最高
    }

    public override void Initialize(AttrSet attr_set)
    {
        AttrSet = attr_set;
        attr_set.AddDependency(TargetAttr, SourceAttr);
        attr_set.RegisterObserver(SourceAttr, this);
    }

    public override void Cleanup(AttrSet attr_set)
    {
        AttrSet = null;
        attr_set.RemoveDependency(TargetAttr, SourceAttr);
        attr_set.UnregisterObserver(SourceAttr, this);
    }

    public override float Apply(float value, AttrSet attr_set)
    {
        // 直接返回源属性值（覆盖其他计算）
        return attr_set.GetValue(SourceAttr);
    }

    // 实现 IAttrObserver 接口方法
    public void OnAttrChanged(string attr_name, float old_value, float new_value)
    {
        if (attr_name == SourceAttr && AttrSet != null)
        {
            AttrSet.MarkDirty(TargetAttr);
        }
    }
}

// 依赖修饰器
public class DependencyModifier : AttrModifier, IAttrObserver
{
    // 成员变量首字母大写 + GetSet（原_source_attr/_coefficient/_set 修改）
    private string SourceAttr { get; set; }
    private float Coefficient { get; set; }
    private AttrSet AttrSet { get; set; }

    public DependencyModifier(string target_attr, string source_attr, float coefficient)
    : base(target_attr)
    {
        SourceAttr = source_attr ?? throw new ArgumentNullException(nameof(source_attr));
        Coefficient = coefficient;
        Priority = 15; // 依赖计算在加法之后、乘法之前
    }

    public override void Initialize(AttrSet attr_set)
    {
        AttrSet = attr_set;
        attr_set.AddDependency(TargetAttr, SourceAttr);
        attr_set.RegisterObserver(SourceAttr, this);
    }

    public override void Cleanup(AttrSet attr_set)
    {
        AttrSet = null;
        attr_set.RemoveDependency(TargetAttr, SourceAttr);
        attr_set.UnregisterObserver(SourceAttr, this);
    }

    public override float Apply(float value, AttrSet attr_set)
    {
        return value + attr_set.GetValue(SourceAttr) * Coefficient;
    }

    // 实现 IAttrObserver 接口方法
    public void OnAttrChanged(string attr_name, float old_value, float new_value)
    {
        if (attr_name == SourceAttr && AttrSet != null)
        {
            AttrSet.MarkDirty(TargetAttr);
        }
    }
}

// 测试程序
public class Program
{
    static void Main()
    {
        int npc_num = 10000;
        int state_num = 1000;
        int action_num = 200;
        Random rd = new();

        Stopwatch sw = new();
        sw.Start();

        for (int i = 0; i < npc_num; i++)
        {
            for (int j = 0; j < state_num; j++)
            {
                for (int k = 0; k < action_num; k++)
                {
                    int aa = rd.Next();
                }
            }
        }

        var tm = sw.Elapsed.TotalSeconds;
        sw.Stop();
        Console.WriteLine($"Elapsed time: {tm}s");
    }

    //static void Main()
    //{
    //    var attr_set = new AttrSet();

    //    // 添加基础属性
    //    attr_set.AddAttr("HP", 100);
    //    attr_set.AddAttr("Attack", 10);
    //    attr_set.AddAttr("Defense", 5);

    //    // 测试 1: 初始值
    //    float attack1 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack1: {attack1}"); // 预期: 10

    //    // 添加加法修饰器
    //    attr_set.AddModifier(new AddModifier("Attack", 5));
    //    float attack2 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack2: {attack2}"); // 预期: 15

    //    // 添加乘法修饰器
    //    attr_set.AddModifier(new MultiplyModifier("Attack", 1.5f));
    //    float attack3 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack3: {attack3}"); // 预期: 22.5

    //    // 添加额外乘法修饰器
    //    attr_set.AddModifier(new ExtraMultiplyModifier("Attack", 0.1f));
    //    float attack4 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack4: {attack4}"); // 预期: 24.75

    //    // 添加同步修饰器：Attack 同步 Defense 的值
    //    var sync_modifier = new SyncModifier("Attack", "Defense");
    //    attr_set.AddModifier(sync_modifier);
    //    float attack5 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack5: {attack5}"); // 预期: 5

    //    // 添加依赖修饰器：Attack 附加 10% HP
    //    attr_set.AddModifier(new DependencyModifier("Attack", "HP", 0.1f));
    //    float attack6 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack6: {attack6}"); // 预期: 5 (被同步修饰器覆盖)

    //    // 修改 Defense 值
    //    attr_set.SetBaseValue("Defense", 20);
    //    float defense1 = attr_set.GetValue("Defense");
    //    Console.WriteLine($"defense1: {defense1}"); // 预期: 20

    //    float attack7 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack7: {attack7}"); // 预期: 20

    //    // 修改 HP 值
    //    attr_set.SetBaseValue("HP", 200);
    //    float attack8 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack8: {attack8}"); // 预期: 20 (被同步修饰器覆盖)

    //    // 移除同步修饰器
    //    attr_set.RemoveModifier(sync_modifier);
    //    float attack9 = attr_set.GetValue("Attack");
    //    Console.WriteLine($"attack9: {attack9}"); // 预期: 57.75

    //    Console.WriteLine("\n 预期结果:");
    //    Console.WriteLine("attack1: 10");
    //    Console.WriteLine("attack2: 15");
    //    Console.WriteLine("attack3: 22.5");
    //    Console.WriteLine("attack4: 24.75");
    //    Console.WriteLine("attack5: 5");
    //    Console.WriteLine("attack6: 5");
    //    Console.WriteLine("defense1: 20");
    //    Console.WriteLine("attack7: 20");
    //    Console.WriteLine("attack8: 20");
    //    Console.WriteLine("attack9: 57.75");
    //}
}