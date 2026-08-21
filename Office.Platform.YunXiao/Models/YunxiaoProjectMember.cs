namespace Office.Platform.YunXiao.Models;

/// <summary>
/// 项目成员。
/// UserId 可直接作为创建工作项时的 assignedTo 参数。
/// </summary>
public sealed class YunxiaoProjectMember
{
    /// <summary>成员用户 ID（即 assignedTo）</summary>
    public string? UserId { get; set; }

    /// <summary>成员名称</summary>
    public string? UserName { get; set; }

    /// <summary>项目角色名（如「项目经理」「开发者」）</summary>
    public string? RoleName { get; set; }
}
