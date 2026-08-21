namespace Office.Platform.YunXiao.Models;

/// <summary>
/// 云效代码仓库（Codeup）。
/// 仓库在组织下以 group/namespace 树组织，用 repositoryId（数字 ID 或完整路径）寻址。
/// </summary>
public sealed class YunxiaoCodeupRepository
{
    /// <summary>仓库 ID（查询提交/分支时使用）</summary>
    public string? Id { get; set; }

    /// <summary>仓库名称</summary>
    public string? Name { get; set; }

    /// <summary>仓库完整路径（如 group1/demo-repo）</summary>
    public string? Path { get; set; }

    /// <summary>仓库描述</summary>
    public string? Description { get; set; }

    /// <summary>可见性：private / public / internal</summary>
    public string? Visibility { get; set; }

    /// <summary>是否已归档</summary>
    public bool? Archived { get; set; }

    /// <summary>仓库在 Codeup 页面的地址</summary>
    public string? WebUrl { get; set; }

    /// <summary>创建时间（ISO 8601）</summary>
    public string? CreatedAt { get; set; }

    /// <summary>最近活跃时间（ISO 8601）</summary>
    public string? LastActivityAt { get; set; }
}
