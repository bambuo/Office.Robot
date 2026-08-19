namespace Sikiro.YunXiao.Models;

/// <summary>
/// 工作项类型（大类的细分，如 Bug 大类下的「缺陷」「线上故障」）。
/// Id 即创建工作项时的 workitemTypeId。
/// </summary>
public sealed class YunxiaoWorkItemType
{
    /// <summary>类型 ID（创建工作项时的 workitemTypeId）</summary>
    public string? Id { get; set; }

    /// <summary>类型中文名，如「缺陷」「线上故障」</summary>
    public string? Name { get; set; }

    /// <summary>类型英文名</summary>
    public string? NameEn { get; set; }

    /// <summary>所属大类：Req / Task / Bug</summary>
    public string? CategoryId { get; set; }

    /// <summary>类型说明</summary>
    public string? Description { get; set; }

    /// <summary>是否启用</summary>
    public bool Enable { get; set; }

    /// <summary>是否为该大类下的默认类型（创建时通常取默认类型）</summary>
    public bool DefaultType { get; set; }

    /// <summary>是否系统内置类型</summary>
    public bool SystemDefault { get; set; }
}
