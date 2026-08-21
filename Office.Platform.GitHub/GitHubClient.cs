using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Office.Platform.GitHub.Models;

namespace Office.Platform.GitHub;

/// <summary>
/// GitHub REST API 客户端（只读查询）。
/// 认证：Authorization: Bearer {PersonalAccessToken}，请求 https://api.github.com。
/// 仓库用 owner/repo 定位；仓库列表用 /user/repos（token 有权限的仓库）。
/// </summary>
public sealed class GitHubClient
{
    private const string BaseUrl = "https://api.github.com";

    /// <summary>
    /// JSON 序列化配置：snake_case（GitHub API 字段是 full_name / default_branch 风格）、忽略 null。
    /// 注意不能用 CamelCase resolver——GitHub 返回 snake_case，反序列化会全部匹配失败。
    /// </summary>
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly GitHubOptions _options;
    private readonly HttpClient _http = new();

    public GitHubClient(GitHubOptions options)
    {
        _options = options;
        if (string.IsNullOrEmpty(options.Token))
            throw new ArgumentException("GitHub Token 未配置（GitHubOptions.Token）");
    }

    /// <summary>列出 token 有权限的仓库（个人账号的公开 + 私有仓库），可按名称搜索</summary>
    public async Task<List<GitHubRepository>> ListRepositoriesAsync(string? search = null, int perPage = 100,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string>
        {
            ["per_page"] = perPage.ToString(),
            ["sort"] = "updated",
        };
        var repos = await GetListAsync<GitHubRepository>("/user/repos", query, ct);
        if (!string.IsNullOrEmpty(search))
            repos = repos.Where(r =>
                    (r.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (r.FullName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        return repos;
    }

    /// <summary>查询仓库最近提交</summary>
    /// <param name="repository">owner/repo 或仓库名</param>
    /// <param name="branch">分支名，默认默认分支</param>
    public async Task<List<GitHubCommit>> ListCommitsAsync(string repository, string? branch = null, int perPage = 20,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string> { ["per_page"] = perPage.ToString() };
        if (!string.IsNullOrEmpty(branch)) query["sha"] = branch;
        return await GetListAsync<GitHubCommit>($"/repos/{repository}/commits", query, ct);
    }

    /// <summary>查询仓库 Pull Request 列表（默认 open）</summary>
    public async Task<List<GitHubPullRequest>> ListPullRequestsAsync(string repository, string state = "open",
        int perPage = 20, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string>
        {
            ["state"] = state,
            ["per_page"] = perPage.ToString(),
        };
        return await GetListAsync<GitHubPullRequest>($"/repos/{repository}/pulls", query, ct);
    }

    /// <summary>查询仓库 Issue 列表（默认 open；注意：PR 也会出现在 issues 接口中）</summary>
    public async Task<List<GitHubIssue>> ListIssuesAsync(string repository, string state = "open",
        int perPage = 20, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string>
        {
            ["state"] = state,
            ["per_page"] = perPage.ToString(),
        };
        return await GetListAsync<GitHubIssue>($"/repos/{repository}/issues", query, ct);
    }

    /// <summary>查询仓库分支列表</summary>
    public async Task<List<GitHubBranch>> ListBranchesAsync(string repository, int perPage = 100,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string> { ["per_page"] = perPage.ToString() };
        return await GetListAsync<GitHubBranch>($"/repos/{repository}/branches", query, ct);
    }

    /// <summary>GET 列表接口（分页只取首页），返回反序列化后的实体列表</summary>
    private async Task<List<T>> GetListAsync<T>(string path, Dictionary<string, string> query, CancellationToken ct)
    {
        var raw = await GetAsync(path, query, ct);
        return string.IsNullOrEmpty(raw) ? [] : JsonConvert.DeserializeObject<List<T>>(raw, JsonSettings) ?? [];
    }

    /// <summary>底层 GET 请求：Bearer 认证，带 GitHub 要求的 User-Agent 头，返回原始 JSON</summary>
    private async Task<string?> GetAsync(string path, Dictionary<string, string>? query = null, CancellationToken ct = default)
    {
        var uri = new UriBuilder(BaseUrl + path);
        if (query is { Count: > 0 })
            uri.Query = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        using var req = new HttpRequestMessage(HttpMethod.Get, uri.Uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        req.Headers.Add("User-Agent", "Sky.Robot"); // GitHub API 强制要求 User-Agent
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub API 调用失败({(int)resp.StatusCode}): {text}");
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
