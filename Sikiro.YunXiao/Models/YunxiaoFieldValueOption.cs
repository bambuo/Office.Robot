namespace Sikiro.YunXiao.Models;

/// <summary>自定义字段取值中的单个选项（如 优先级 = 高）</summary>
public sealed class YunxiaoFieldValueOption
{
    /// <summary>选项标识（更新字段时传这个 Id）</summary>
    public string? Identifier { get; set; }

    /// <summary>选项展示文案（如「3-一般」）</summary>
    public string? DisplayValue { get; set; }
}
