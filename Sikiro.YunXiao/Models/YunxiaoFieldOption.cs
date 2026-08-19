namespace Sikiro.YunXiao.Models;

/// <summary>
/// 字段定义中的可选项（list 格式字段的候选值，如 严重程度的「1-致命 ~ 4-轻微」）。
/// 创建/更新工作项时传 Option.Id（不是中文文案）。
/// </summary>
public sealed class YunxiaoFieldOption
{
    /// <summary>选项 ID（写入 customFieldValues 的值）</summary>
    public string? Id { get; set; }

    /// <summary>选项值（与 DisplayValue 基本一致）</summary>
    public string? Value { get; set; }

    /// <summary>选项英文值</summary>
    public string? ValueEn { get; set; }

    /// <summary>选项展示文案</summary>
    public string? DisplayValue { get; set; }
}
