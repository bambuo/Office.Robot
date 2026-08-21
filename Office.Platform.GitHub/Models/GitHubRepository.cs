namespace Office.Platform.GitHub.Models;

/// <summary>GitHub 仓库（/user/repos 返回的条目）</summary>
public sealed class GitHubRepository
{
    /// <summary>仓库数字 ID</summary>
    public long? Id { get; set; }

    /// <summary>仓库名（不含 owner）</summary>
    public string? Name { get; set; }

    /// <summary>完整名称 owner/repo（工具中定位仓库用）</summary>
    public string? FullName { get; set; }

    /// <summary>仓库描述</summary>
    public string? Description { get; set; }

    /// <summary>默认分支名</summary>
    public string? DefaultBranch { get; set; }

    /// <summary>可见性：public / private</summary>
    public string? Visibility { get; set; }

    /// <summary>是否已归档</summary>
    public bool? Archived { get; set; }

    /// <summary>仓库在 GitHub 的页面地址</summary>
    public string? HtmlUrl { get; set; }
}
