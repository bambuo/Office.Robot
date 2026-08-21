namespace Office.Platform.GitHub.Models;

/// <summary>GitHub 提交（/repos/{owner}/{repo}/commits 返回的条目）</summary>
public sealed class GitHubCommit
{
    /// <summary>提交 SHA</summary>
    public string? Sha { get; set; }

    /// <summary>短 SHA（前 7 位，列表展示用）</summary>
    public string? ShortSha => Sha is { Length: >= 7 } ? Sha[..7] : Sha;

    /// <summary>提交信息</summary>
    public string? Message { get; set; }

    /// <summary>作者姓名</summary>
    public string? AuthorName { get; set; }

    /// <summary>提交时间（ISO 8601）</summary>
    public string? Date { get; set; }

    /// <summary>提交在 GitHub 的页面地址</summary>
    public string? HtmlUrl { get; set; }
}
