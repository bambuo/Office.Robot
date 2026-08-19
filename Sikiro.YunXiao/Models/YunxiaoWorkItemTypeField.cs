namespace Sikiro.YunXiao.Models;

/// <summary>
/// 工作项类型的字段定义（来自「字段配置」）。
/// 创建工作项前可先查询，用于确定哪些字段必填、list 字段有哪些可选值。
/// </summary>
public sealed class YunxiaoWorkItemTypeField
{
    /// <summary>字段标识（创建时作为 customFieldValues 的 key），如 subject / priority / seriousLevel</summary>
    public string? Id { get; set; }

    /// <summary>字段中文名，如「标题」「优先级」「严重程度」</summary>
    public string? Name { get; set; }

    /// <summary>字段说明</summary>
    public string? Description { get; set; }

    /// <summary>字段类型：NativeField（原生）/ SystemCustomField（系统自定义）/ Role（角色）/ Application（应用）</summary>
    public string? Type { get; set; }

    /// <summary>字段格式：string / user / multiUser / list / date / float / sprint / tag / auto</summary>
    public string? Format { get; set; }

    /// <summary>字段默认值（list 格式时为选项 Id，可与 Options 匹配得到默认选项）</summary>
    public string? DefaultValue { get; set; }

    /// <summary>是否必填（创建时必须提供，如 Bug 的 严重程度）</summary>
    public bool Required { get; set; }

    /// <summary>是否在创建弹窗中展示</summary>
    public bool ShowWhenCreate { get; set; }

    /// <summary>list 格式字段的全部可选项</summary>
    public List<YunxiaoFieldOption> Options { get; set; } = [];
}
