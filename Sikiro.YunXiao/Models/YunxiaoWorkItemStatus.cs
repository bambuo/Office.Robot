namespace Sikiro.YunXiao.Models;

/// <summary>
/// 工作项/项目状态。
/// 云效通过「状态机」管理流转，每个状态有唯一 Id，展示时优先用 DisplayName。
/// </summary>
public sealed class YunxiaoWorkItemStatus
{
    /// <summary>状态 ID（随项目状态机配置变化，不要硬编码）</summary>
    public string? Id { get; set; }

    /// <summary>状态中文名（如「待确认」）</summary>
    public string? Name { get; set; }

    /// <summary>状态英文名（如「New」）</summary>
    public string? NameEn { get; set; }

    /// <summary>用于展示的名称，一般与 Name 一致</summary>
    public string? DisplayName { get; set; }
}
