namespace Office.Platform.GitHub.Models;

/// <summary>GitHub Issue（/repos/{owner}/{repo}/issues 返回的条目）</summary>
public sealed class GitHubIssue
{
    /// <summary>Issue 编号</summary>
    public int? Number { get; set; }

    /// <summary>Issue 标题</summary>
    public string? Title { get; set; }

    /// <summary>状态：open / closed</summary>
    public string? State { get; set; }

    /// <summary>作者登录名</summary>
    public string? UserLogin { get; set; }

    /// <summary>创建时间（ISO 8601）</summary>
    public string? CreatedAt { get; set; }

    /// <summary>标签名列表</summary>
    public List<string>? Labels { get; set; }

    /// <summary>Issue 页面地址</summary>
    public string? HtmlUrl { get; set; }
}
