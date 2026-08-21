using System.ComponentModel;
using Microsoft.Extensions.AI;
using Office.Platform.GitHub;

namespace Office.Robot.DingTalk.Chat;

/// <summary>
/// GitHub Agent 工具集：把 GitHubClient 的能力包装成 AIFunction，供大模型在对话中自动调用。
/// 所有工具均返回中文文本（含失败原因），便于模型直接组织回复。
/// </summary>
public static class GitHubAgentTools
{
    /// <summary>基于 GitHub 客户端构造 Agent 可用的工具列表</summary>
    public static IList<AITool> Create(GitHubClient client)
    {
        var functions = new GitHubToolFunctions(client);
        return
        [
            AIFunctionFactory.Create(functions.ListRepositories),
            AIFunctionFactory.Create(functions.ListCommits),
            AIFunctionFactory.Create(functions.ListPullRequests),
            AIFunctionFactory.Create(functions.ListIssues),
            AIFunctionFactory.Create(functions.ListBranches),
        ];
    }

    /// <summary>工具的具体实现（持有一个 GitHubClient）</summary>
    private sealed class GitHubToolFunctions(GitHubClient client)
    {
        [Description("列出当前 GitHub 账号可访问的仓库列表（owner/repo 格式）。当用户提到 GitHub 仓库、提交、PR、Issue 时，先调用此工具确认仓库名。")]
        public async Task<string> ListRepositories(
            [Description("按仓库名模糊搜索，可不传")] string? search = null)
        {
            var repos = await client.ListRepositoriesAsync(search);
            if (repos.Count == 0)
                return search is null
                    ? "GitHub 账号下没有查询到任何仓库"
                    : $"没有找到名称包含「{search}」的 GitHub 仓库";
            return "GitHub 仓库列表：" + string.Join("、", repos.Select(r => r.FullName));
        }

        [Description("查询指定 GitHub 仓库的最近提交记录（短SHA、提交信息、作者、时间）。")]
        public async Task<string> ListCommits(
            [Description("仓库名，格式 owner/repo（如 bambuo/sky-robot）或纯仓库名")] string repository,
            [Description("分支名，默认仓库默认分支")] string? branch = null,
            [Description("查询条数，默认 10")] int maxResults = 10)
        {
            var (error, fullName) = await ResolveRepositoryAsync(repository);
            if (error is not null) return error;

            var commits = await client.ListCommitsAsync(fullName!, branch, perPage: maxResults);
            if (commits.Count == 0)
                return $"仓库「{repository}」暂无提交记录";

            return $"仓库「{fullName}」最近的提交：\n" + string.Join("\n", commits.Select(c =>
                $"- {c.ShortSha} {FirstLine(c.Message)}（{c.AuthorName ?? "未知作者"}，{c.Date}）"));
        }

        [Description("查询指定 GitHub 仓库的 Pull Request 列表（编号、标题、作者、状态）。")]
        public async Task<string> ListPullRequests(
            [Description("仓库名，格式 owner/repo（如 bambuo/sky-robot）或纯仓库名")] string repository,
            [Description("状态：open=开启、closed=已关闭、all=全部，默认 open")] string state = "open",
            [Description("查询条数，默认 10")] int maxResults = 10)
        {
            var (error, fullName) = await ResolveRepositoryAsync(repository);
            if (error is not null) return error;

            var prs = await client.ListPullRequestsAsync(fullName!, state, perPage: maxResults);
            if (prs.Count == 0)
                return $"仓库「{repository}」暂无 {state} 状态的 Pull Request";

            return $"仓库「{fullName}」的 Pull Request：\n" + string.Join("\n", prs.Select(p =>
                $"- #{p.Number} {p.Title}（{p.UserLogin ?? "未知作者"}，{p.State}）"));
        }

        [Description("查询指定 GitHub 仓库的 Issue 列表（编号、标题、作者、状态、标签）。")]
        public async Task<string> ListIssues(
            [Description("仓库名，格式 owner/repo（如 bambuo/sky-robot）或纯仓库名")] string repository,
            [Description("状态：open=开启、closed=已关闭、all=全部，默认 open")] string state = "open",
            [Description("查询条数，默认 10")] int maxResults = 10)
        {
            var (error, fullName) = await ResolveRepositoryAsync(repository);
            if (error is not null) return error;

            var issues = await client.ListIssuesAsync(fullName!, state, perPage: maxResults);
            if (issues.Count == 0)
                return $"仓库「{repository}」暂无 {state} 状态的 Issue";

            return $"仓库「{fullName}」的 Issue：\n" + string.Join("\n", issues.Select(i =>
                $"- #{i.Number} {i.Title}（{i.UserLogin ?? "未知作者"}，{i.State}" +
                (i.Labels is { Count: > 0 } ? $"，标签：{string.Join("、", i.Labels)}" : "") + "）"));
        }

        [Description("查询指定 GitHub 仓库的分支列表。")]
        public async Task<string> ListBranches(
            [Description("仓库名，格式 owner/repo（如 bambuo/sky-robot）或纯仓库名")] string repository)
        {
            var (error, fullName) = await ResolveRepositoryAsync(repository);
            if (error is not null) return error;

            var branches = await client.ListBranchesAsync(fullName!);
            if (branches.Count == 0)
                return $"仓库「{repository}」暂无分支";

            return $"仓库「{fullName}」分支：" + string.Join("、", branches.Select(b => b.Name));
        }

        /// <summary>
        /// 解析仓库：owner/repo 直接用；纯仓库名时在账号仓库列表中按 fullName / name 匹配。
        /// 找不到时返回错误提示（附现有仓库列表，供模型转告用户）。
        /// </summary>
        private async Task<(string? Error, string? FullName)> ResolveRepositoryAsync(string repository)
        {
            if (repository.Contains('/'))
                return (null, repository);

            var repos = await client.ListRepositoriesAsync();
            var matched = repos.FirstOrDefault(r =>
                    string.Equals(r.Name, repository, StringComparison.OrdinalIgnoreCase))
                ?? repos.FirstOrDefault(r =>
                    string.Equals(r.FullName, repository, StringComparison.OrdinalIgnoreCase));
            return matched?.FullName is null
                ? ($"未找到 GitHub 仓库「{repository}」，可用仓库：{string.Join("、", repos.Select(r => r.FullName))}", null)
                : (null, matched.FullName);
        }

        /// <summary>取提交信息第一行（标题）</summary>
        private static string FirstLine(string? message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            var idx = message.IndexOf('\n');
            return idx > 0 ? message[..idx] : message;
        }
    }
}
