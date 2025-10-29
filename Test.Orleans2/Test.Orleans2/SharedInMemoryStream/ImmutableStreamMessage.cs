using System;

/// <summary>
/// 不可变流消息示例（核心：所有字段只读，无修改逻辑）
/// </summary>
[Serializable] // 支持跨Silo序列化
public sealed class ImmutableStreamMessage
{
    // 只读字段，确保无法被修改
    public readonly string Id;
    public readonly string Content;
    public readonly DateTime Timestamp;

    // 仅通过构造函数赋值，无setter
    public ImmutableStreamMessage(string id, string content, DateTime timestamp)
    {
        Id = id;
        Content = content;
        Timestamp = timestamp;
    }

    // 无任何修改字段的方法
}