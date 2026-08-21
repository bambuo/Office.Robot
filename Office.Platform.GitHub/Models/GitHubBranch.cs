namespace Office.Platform.GitHub.Models;

/// <summary>GitHub 分支（/repos/{owner}/{repo}/branches 返回的条目）</summary>
public sealed class GitHubBranch
{
    /// <summary>分支名</summary>
    public string? Name { get; set; }

    /// <summary>是否受保护分支</summary>
    public bool? Protected { get; set; }

    /// <summary>分支最新提交 SHA</summary>
    public string? CommitSha { get; set; }
}
