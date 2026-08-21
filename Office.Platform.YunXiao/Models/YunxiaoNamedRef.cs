namespace Office.Platform.YunXiao.Models;

/// <summary>
/// 命名引用（Id + Name 的轻量结构）。
/// 用于工作项里的所属项目（Space）、工作项类型（WorkitemType）、迭代（Sprint）等关联对象。
/// </summary>
public sealed class YunxiaoNamedRef
{
    /// <summary>关联对象 ID</summary>
    public string? Id { get; set; }

    /// <summary>关联对象名称</summary>
    public string? Name { get; set; }
}
