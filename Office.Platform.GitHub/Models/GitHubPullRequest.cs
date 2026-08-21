namespace Office.Platform.GitHub.Models;

/// <summary>GitHub Pull Request（/repos/{owner}/{repo}/pulls 返回的条目）</summary>
public sealed class GitHubPullRequest
{
    /// <summary>PR 编号</summary>
    public int? Number { get; set; }

    /// <summary>PR 标题</summary>
    public string? Title { get; set; }

    /// <summary>状态：open / closed / merged（merged 由 merged_at 判断）</summary>
    public string? State { get; set; }

    /// <summary>作者登录名</summary>
    public string? UserLogin { get; set; }

    /// <summary>创建时间（ISO 8601）</summary>
    public string? CreatedAt { get; set; }

    /// <summary>目标分支</summary>
    public string? BaseRef { get; set; }

    /// <summary>源分支</summary>
    public string? HeadRef { get; set; }

    /// <summary>PR 页面地址</summary>
    public string? HtmlUrl { get; set; }
}
