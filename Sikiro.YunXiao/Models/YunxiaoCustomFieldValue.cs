namespace Sikiro.YunXiao.Models;

/// <summary>
/// 工作项上的自定义/系统自定义字段取值，如 优先级（priority）、严重程度（seriousLevel）。
/// 一个字段可多选（multiUser 等格式），所以 Values 是列表。
/// </summary>
public sealed class YunxiaoCustomFieldValue
{
    /// <summary>字段标识，如 priority / seriousLevel</summary>
    public string? FieldId { get; set; }

    /// <summary>字段中文名，如「优先级」</summary>
    public string? FieldName { get; set; }

    /// <summary>字段格式：list / user / multiUser / date / float 等</summary>
    public string? FieldFormat { get; set; }

    /// <summary>当前取值（一个或多个选项）</summary>
    public List<YunxiaoFieldValueOption> Values { get; set; } = [];
}
