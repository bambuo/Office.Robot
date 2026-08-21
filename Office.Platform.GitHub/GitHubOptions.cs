namespace Office.Platform.GitHub;

/// <summary>
/// GitHub API 配置。
/// Token 在 GitHub 个人设置 → Developer settings → Personal access tokens 创建，
/// 只读查询给 repo（含私有仓库）或 public_repo scope 即可。
/// </summary>
public sealed class GitHubOptions
{
    /// <summary>GitHub 个人访问令牌（ghp_ 开头）</summary>
    public string Token { get; set; } = "";
}
