using System.Net.Http.Headers;
using System.Text;
using AlibabaCloud.OpenApiClient;
using AlibabaCloud.OpenApiClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Office.Platform.YunXiao.Enums;
using Office.Platform.YunXiao.Models;
using RuntimeOptions = AlibabaCloud.TeaUtil.Models.RuntimeOptions;

namespace Office.Platform.YunXiao;

// =====================================================================================
// 云效（Yunxiao / Projex）工作项封装 —— Bug / 需求(Req) / 任务(Task)
//
// 两种认证方案，构造 YunxiaoClient 时二选一（凭证见 YunxiaoOptions）：
//   方案一：阿里云 AccessKey（V2.0 通用 SDK ROA 泛化调用，devops.cn-hangzhou.aliyuncs.com）
//   方案二：个人访问令牌 PAT（x-yunxiao-token 头直连 oapi/v1，openapi-rdc.aliyuncs.com）
//
// spaceId 即项目 ID（Projex URL 中 /projex/project/{projectId} 的那段）；
// workitemTypeId / assignedTo 可通过 ListWorkItemTypesAsync / ListProjectMembersAsync 获取；
// 极简创建用 QuickCreateWorkItemAsync（标题 + 项目名即可）；
// 底层 SendAsync 可直调任意云效 OpenAPI（返回原始 JSON 字符串）。
// =====================================================================================

/// <summary>
/// 云效工作项（Bug/需求/任务）客户端。
/// 认证方案一（AK 自签名）走 V2.0 通用 SDK 的 ROA 泛化调用；
/// 认证方案二（PAT）走 x-yunxiao-token 请求头直连 oapi/v1。
/// </summary>
public sealed class YunxiaoClient
{
    /// <summary>JSON 序列化配置：camelCase（与云效接口字段风格一致）、忽略 null</summary>
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly YunxiaoOptions _options;
    private readonly Client? _akClient; // 方案一
    private readonly HttpClient _http = new();                     // 方案二

    /// <summary>按配置自动选择认证方案：有 AK 走方案一，否则要求有 PAT</summary>
    /// <exception cref="ArgumentException">两种凭证都未提供时抛出</exception>
    public YunxiaoClient(YunxiaoOptions options)
    {
        _options = options;

        if (!string.IsNullOrEmpty(options.AccessKeyId) && !string.IsNullOrEmpty(options.AccessKeySecret))
        {
            // 方案一：V2.0 通用 SDK，仅需核心包，不安装云效产品 SDK
            _akClient = new Client(new Config
            {
                AccessKeyId = options.AccessKeyId,
                AccessKeySecret = options.AccessKeySecret,
                Endpoint = YunxiaoOptions.AkEndpoint,
            });
        }
        else if (!string.IsNullOrEmpty(options.PersonalAccessToken))
        {
            // 方案二：PAT，无签名
        }
        else
        {
            throw new ArgumentException("必须提供 AccessKeyId/AccessKeySecret（方案一）或 PersonalAccessToken（方案二）");
        }
    }

    #region 工作项
    /// <summary>
    /// 创建工作项（Bug / 需求 / 任务）。创建成功后自动回查详情，返回完整实体。
    /// </summary>
    /// <param name="category">工作项大类</param>
    /// <param name="subject">标题</param>
    /// <param name="spaceId">项目 ID（可通过 ListProjectsAsync 获取）</param>
    /// <param name="assignedTo">负责人用户 ID（可通过 ListProjectMembersAsync 获取）</param>
    /// <param name="workitemTypeId">工作项类型 ID（可通过 ListWorkItemTypesAsync 获取）</param>
    /// <param name="description">描述（Markdown），可不传</param>
    /// <param name="customFieldValues">
    /// 自定义/系统自定义字段（字段id → 值），如 Bug 必填的 ["seriousLevel"] = "选项id"；
    /// 字段是否必填、选项 id 可通过 ListWorkItemTypeFieldsAsync 查询。
    /// </param>
    /// <param name="ct">取消令牌</param>
    public async Task<YunxiaoWorkItem> CreateWorkItemAsync(
        YunxiaoWorkItemCategory category,
        string subject,
        string spaceId,
        string assignedTo,
        string workitemTypeId,
        string? description = null,
        IDictionary<string, string>? customFieldValues = null,
        CancellationToken ct = default)
    {
        string raw;
        if (_akClient is not null)
        {
            // POST /organization/{orgId}/workitems/create
            var body = new Dictionary<string, object?>
            {
                ["subject"] = subject,
                ["category"] = category.ToApiValue(),
                ["space"] = spaceId,
                ["spaceIdentifier"] = spaceId,
                ["spaceType"] = "Project",
                ["assignedTo"] = assignedTo,
                ["workitemType"] = workitemTypeId,
            };
            if (description is not null)
            {
                body["description"] = description;
                body["descriptionFormat"] = "MARKDOWN";
            }
            if (customFieldValues is not null)
                body["customFieldValues"] = customFieldValues;
            raw = await CallRoaAsync(HttpMethod.Post, "/organization/{0}/workitems/create", body, ct: ct)
                ?? throw new YunxiaoException("创建工作项失败：响应为空");
        }
        else
        {
            // POST /oapi/v1/projex/organizations/{orgId}/workitems
            var body = new Dictionary<string, object?>
            {
                ["subject"] = subject,
                ["spaceId"] = spaceId,
                ["assignedTo"] = assignedTo,
                ["workitemTypeId"] = workitemTypeId,
            };
            if (description is not null)
            {
                body["description"] = description;
                body["formatType"] = "MARKDOWN";
            }
            if (customFieldValues is not null)
                body["customFieldValues"] = customFieldValues;
            raw = await CallPatAsync(HttpMethod.Post, "/oapi/v1/projex/organizations/{0}/workitems", body, ct: ct)
                ?? throw new YunxiaoException("创建工作项失败：响应为空");
        }

        // 创建接口只返回 id，回查详情补全编号、状态等字段
        var created = JObject.Parse(raw);
        var id = (created["id"] ?? created["identifier"])?.ToString()
            ?? throw new YunxiaoException($"创建工作项失败：响应中无 id（{raw}）");
        return (await GetWorkItemAsync(id, ct))!;
    }

    /// <summary>
    /// 获取工作项列表（分页）。
    /// 方案一：GET /organization/{orgId}/listWorkitems（category=Req/Task/Bug）
    /// 方案二：POST /oapi/v1/projex/organizations/{orgId}/workitems:search
    /// </summary>
    /// <param name="category">工作项大类</param>
    /// <param name="spaceId">项目 ID</param>
    /// <param name="nextToken">翻页令牌（方案一 AK 用，首页不传）</param>
    /// <param name="maxResults">每页条数（方案一 maxResults / 方案二 perPage）</param>
    /// <param name="page">页码，从 1 开始（方案二 PAT 用）</param>
    public async Task<List<YunxiaoWorkItem>> ListWorkItemsAsync(
        YunxiaoWorkItemCategory category,
        string spaceId,
        string? nextToken = null,
        int maxResults = 20,
        int page = 1,
        CancellationToken ct = default)
    {
        string raw;
        if (_akClient is not null)
        {
            var query = new Dictionary<string, string>
            {
                ["spaceType"] = "Project",
                ["spaceIdentifier"] = spaceId,
                ["category"] = category.ToApiValue(),
                ["maxResults"] = maxResults.ToString(),
            };
            if (nextToken is not null) query["nextToken"] = nextToken;
            raw = await CallRoaAsync(HttpMethod.Get, "/organization/{0}/listWorkitems", query: query, ct: ct) ?? "[]";
        }
        else
        {
            var body = new Dictionary<string, object?>
            {
                ["category"] = category.ToApiValue(),
                ["spaceId"] = spaceId,
                ["spaceType"] = "Project",
                ["page"] = page,
                ["perPage"] = maxResults,
                ["orderBy"] = "gmtCreate",
                ["sort"] = "desc",
            };
            raw = await CallPatAsync(HttpMethod.Post, "/oapi/v1/projex/organizations/{0}/workitems:search", body, ct: ct) ?? "[]";
        }
        return DeserializeList<YunxiaoWorkItem>(raw);
    }

    /// <summary>
    /// 搜索组织下的项目列表。
    /// 方案二：POST /oapi/v1/projex/organizations/{orgId}/projects:search（page/perPage 分页）
    /// 方案一：GET /organization/{orgId}/listProjects（该网关仅有 2021-06-25 老版路径）
    /// 项目总数在响应头 x-total。
    /// </summary>
    public async Task<List<YunxiaoProject>> ListProjectsAsync(int page = 1, int perPage = 20, CancellationToken ct = default)
    {
        string raw;
        if (_akClient is not null)
        {
            var query = new Dictionary<string, string> { ["maxResults"] = perPage.ToString() };
            raw = await CallRoaAsync(HttpMethod.Get, "/organization/{0}/listProjects", query: query, ct: ct) ?? "[]";
        }
        else
        {
            var body = new Dictionary<string, object?> { ["page"] = page, ["perPage"] = perPage };
            raw = await CallPatAsync(HttpMethod.Post, "/oapi/v1/projex/organizations/{0}/projects:search", body, ct: ct) ?? "[]";
        }
        return DeserializeList<YunxiaoProject>(raw);
    }

    /// <summary>
    /// 获取项目成员列表（用于拿 assignedTo 的用户 ID）。
    /// 方案二：GET /oapi/v1/projex/organizations/{orgId}/projects/{projectId}/members
    /// 方案一：GET /organization/{orgId}/projects/{projectId}/listMembers（该网关仅有 2021-06-25 老版路径）
    /// </summary>
    public async Task<List<YunxiaoProjectMember>> ListProjectMembersAsync(string projectId, string? nextToken = null,
        int maxResults = 20, CancellationToken ct = default)
    {
        string raw;
        if (_akClient is not null)
        {
            var query = new Dictionary<string, string> { ["maxResults"] = maxResults.ToString() };
            if (nextToken is not null) query["nextToken"] = nextToken;
            raw = await CallRoaAsync(HttpMethod.Get, "/organization/{0}/projects/" + Uri.EscapeDataString(projectId) + "/listMembers",
                query: query, ct: ct) ?? "[]";
        }
        else
        {
            raw = await CallPatAsync(HttpMethod.Get,
                "/oapi/v1/projex/organizations/{0}/projects/" + Uri.EscapeDataString(projectId) + "/members", ct: ct) ?? "[]";
        }
        return DeserializeList<YunxiaoProjectMember>(raw);
    }

    /// <summary>
    /// 获取某项目下指定大类（Req/Task/Bug）的工作项类型列表。
    /// 方案二：GET /oapi/v1/projex/organizations/{org}/projects/{projectId}/workitemTypes?category=Bug
    /// 返回的 Id 即创建工作项时的 workitemTypeId。
    /// </summary>
    public async Task<List<YunxiaoWorkItemType>> ListWorkItemTypesAsync(
        string projectId, YunxiaoWorkItemCategory category, CancellationToken ct = default)
    {
        var raw = await CallPatAsync(HttpMethod.Get,
            "/oapi/v1/projex/organizations/{0}/projects/" + Uri.EscapeDataString(projectId) + "/workitemTypes",
            query: new Dictionary<string, string> { ["category"] = category.ToApiValue() }, ct: ct) ?? "[]";
        return DeserializeList<YunxiaoWorkItemType>(raw);
    }

    /// <summary>
    /// 获取某项目下指定工作项类型的字段定义（含必填项与选项列表）。
    /// 方案二：GET /oapi/v1/projex/organizations/{org}/projects/{projectId}/workitemTypes/{typeId}/fields
    /// 创建工作项时把「字段id → 选项id」映射放进 customFieldValues。
    /// </summary>
    public async Task<List<YunxiaoWorkItemTypeField>> ListWorkItemTypeFieldsAsync(string projectId, string workitemTypeId,
        CancellationToken ct = default)
    {
        var raw = await CallPatAsync(HttpMethod.Get,
            "/oapi/v1/projex/organizations/{0}/projects/" + Uri.EscapeDataString(projectId)
            + "/workitemTypes/" + Uri.EscapeDataString(workitemTypeId) + "/fields", ct: ct) ?? "[]";
        return DeserializeList<YunxiaoWorkItemTypeField>(raw);
    }

    /// <summary>获取单个工作项详情</summary>
    public async Task<YunxiaoWorkItem?> GetWorkItemAsync(string workItemId, CancellationToken ct = default)
    {
        var raw = await SendWorkItemAsync(HttpMethod.Get, workItemId, ct: ct);
        return raw is null ? null : JsonConvert.DeserializeObject<YunxiaoWorkItem>(raw, JsonSettings);
    }

    /// <summary>
    /// 更新工作项字段（如 subject/status/priority 等），成功后回查并返回最新实体。
    /// fields 可传匿名对象，如 new { subject = "新标题" }。
    /// </summary>
    public async Task<YunxiaoWorkItem> UpdateWorkItemAsync(string workItemId, object fields, CancellationToken ct = default)
    {
        await SendWorkItemAsync(HttpMethod.Put, workItemId, fields, roaSuffix: "/update", ct: ct);
        return (await GetWorkItemAsync(workItemId, ct))!;
    }

    /// <summary>删除工作项（进入回收站）</summary>
    public Task DeleteWorkItemAsync(string workItemId, CancellationToken ct = default)
        => SendWorkItemAsync(HttpMethod.Delete, workItemId, ct: ct);

    /// <summary>
    /// 极简创建工作项：调用方只传大类、标题、项目名，其余参数自动推断，返回工作项地址（可直接打开）。
    /// 自动推断规则（依赖 oapi/v1 接口，仅支持 PAT 方案）：
    ///   - 项目：按名称在组织下搜索（spaceId）
    ///   - 负责人：可传用户 ID 或成员姓名（UserName，自动解析为用户 ID）；不传则取项目第一个成员
    ///   - 类型：该大类下的默认类型（workitemTypeId）
    ///   - 必填自定义字段：取字段配置的默认选项（如 Bug 的 严重程度）
    /// </summary>
    /// <param name="category">工作项大类：需求 / 任务 / 缺陷</param>
    /// <param name="subject">标题</param>
    /// <param name="projectName">项目名称（组织下唯一）</param>
    /// <param name="assignedTo">负责人：用户 ID 或成员姓名，可不传（取项目第一个成员）</param>
    /// <param name="description">描述（Markdown），可不传</param>
    /// <returns>工作项在 Projex 页面的地址，如 https://devops.aliyun.com/projex/project/{spaceId}/bug/{workItemId}</returns>
    public async Task<string> QuickCreateWorkItemAsync(
        YunxiaoWorkItemCategory category,
        string subject,
        string projectName,
        string? assignedTo = null,
        string? description = null,
        CancellationToken ct = default)
    {
        if (_akClient is not null)
            throw new YunxiaoException("极简创建依赖 oapi/v1 接口，仅支持 PAT 方案；AK 方案请使用 CreateWorkItemAsync");

        // 1. 按名称找项目
        var projects = await ListProjectsAsync(perPage: 100, ct: ct);
        var project = projects.FirstOrDefault(p => p.Name == projectName)
            ?? throw new YunxiaoException($"未找到项目「{projectName}」，现有项目: {string.Join(", ", projects.Select(p => p.Name))}");

        // 2. 负责人：不传取第一个成员；传了则按 用户ID 或 姓名 在成员列表中解析（同时校验成员资格）
        var members = await ListProjectMembersAsync(project.Id!, ct: ct);
        if (string.IsNullOrEmpty(assignedTo))
        {
            assignedTo = members.FirstOrDefault()?.UserId
                ?? throw new YunxiaoException($"项目「{projectName}」没有成员，无法指定负责人");
        }
        else
        {
            var matched = members.FirstOrDefault(m => m.UserId == assignedTo)
                ?? members.FirstOrDefault(m => string.Equals(m.UserName, assignedTo, StringComparison.OrdinalIgnoreCase));
            assignedTo = matched?.UserId
                ?? throw new YunxiaoException($"项目「{projectName}」中找不到成员「{assignedTo}」（可传用户 ID 或成员姓名）");
        }

        // 3. 类型取该大类下的默认类型
        var types = await ListWorkItemTypesAsync(project.Id!, category, ct);
        var type = types.FirstOrDefault(t => t.DefaultType) ?? types.First();

        // 4. 必填的自定义字段（如 严重程度）统一取默认选项
        var fields = await ListWorkItemTypeFieldsAsync(project.Id!, type.Id!, ct);
        var customFieldValues = fields
            .Where(f => f.Required && f.Type == "SystemCustomField" && !string.IsNullOrEmpty(f.DefaultValue))
            .ToDictionary(f => f.Id!, f => f.DefaultValue!);

        // 5. 创建并返回地址
        var item = await CreateWorkItemAsync(category, subject, project.Id!, assignedTo, type.Id!,
            description, customFieldValues, ct);
        return item.Url ?? throw new YunxiaoException("创建成功但无法拼接工作项地址（缺少 Space/CategoryId/Id）");
    }

    #endregion

    #region Codeup 代码仓库
    /// <summary>
    /// 列出组织下代码仓库（Codeup），按名称模糊搜索可选。
    /// GET /oapi/v1/codeup/organizations/{orgId}/repositories
    /// </summary>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="perPage">每页条数（1-100）</param>
    /// <param name="search">按仓库路径模糊搜索，可不传</param>
    public async Task<List<YunxiaoCodeupRepository>> ListCodeupRepositoriesAsync(int page = 1, int perPage = 20,
        string? search = null, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string>
        {
            ["page"] = page.ToString(),
            ["perPage"] = perPage.ToString(),
        };
        if (!string.IsNullOrEmpty(search)) query["search"] = search;

        var raw = await CallPatAsync(HttpMethod.Get, "/oapi/v1/codeup/organizations/{0}/repositories", query: query, ct: ct)
            ?? "[]";
        return DeserializeList<YunxiaoCodeupRepository>(raw);
    }

    /// <summary>
    /// 列出指定代码仓库的提交记录（分页）。
    /// GET /oapi/v1/codeup/organizations/{orgId}/repositories/{repositoryId}/commits
    /// 注意：refName 必传（不传会 400），未指定时自动查仓库默认分支。
    /// </summary>
    /// <param name="repositoryId">仓库 ID 或 URL 编码的完整路径</param>
    /// <param name="refName">分支名 / Tag / 提交 SHA；为空时自动解析默认分支</param>
    /// <param name="since">起始时间（ISO 8601），可不传</param>
    /// <param name="until">截止时间（ISO 8601），可不传</param>
    /// <param name="perPage">每页条数</param>
    public async Task<List<YunxiaoCodeupCommit>> ListCodeupCommitsAsync(string repositoryId, string? refName = null,
        string? since = null, string? until = null, int page = 1, int perPage = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refName))
        {
            // API 要求 refName 必传：先查默认分支，再查提交
            var branches = await ListCodeupBranchesAsync(repositoryId, perPage: 100, ct: ct);
            refName = branches.FirstOrDefault(b => b.DefaultBranch == true)?.Name
                ?? branches.FirstOrDefault()?.Name
                ?? throw new YunxiaoException($"仓库 {repositoryId} 没有分支，无法查询提交");
        }

        var query = new Dictionary<string, string>
        {
            ["refName"] = refName,
            ["page"] = page.ToString(),
            ["perPage"] = perPage.ToString(),
        };
        if (!string.IsNullOrEmpty(since)) query["since"] = since;
        if (!string.IsNullOrEmpty(until)) query["until"] = until;

        var raw = await CallPatAsync(HttpMethod.Get,
            "/oapi/v1/codeup/organizations/{0}/repositories/" + Uri.EscapeDataString(repositoryId) + "/commits",
            query: query, ct: ct) ?? "[]";
        return DeserializeList<YunxiaoCodeupCommit>(raw);
    }

    /// <summary>
    /// 查询单个代码提交详情（含变更统计）。
    /// GET /oapi/v1/codeup/organizations/{orgId}/repositories/{repositoryId}/commits/{sha}
    /// </summary>
    /// <param name="repositoryId">仓库 ID 或 URL 编码的完整路径</param>
    /// <param name="sha">提交 SHA</param>
    public async Task<YunxiaoCodeupCommit?> GetCodeupCommitAsync(string repositoryId, string sha, CancellationToken ct = default)
    {
        var raw = await CallPatAsync(HttpMethod.Get,
            "/oapi/v1/codeup/organizations/{0}/repositories/" + Uri.EscapeDataString(repositoryId) + "/commits/"
            + Uri.EscapeDataString(sha), ct: ct);
        return raw is null ? null : JsonConvert.DeserializeObject<YunxiaoCodeupCommit>(raw, JsonSettings);
    }

    /// <summary>
    /// 列出指定代码仓库的分支（分页）。
    /// GET /oapi/v1/codeup/organizations/{orgId}/repositories/{repositoryId}/branches
    /// </summary>
    /// <param name="repositoryId">仓库 ID 或 URL 编码的完整路径</param>
    /// <param name="search">按分支名模糊搜索，可不传</param>
    public async Task<List<YunxiaoCodeupBranch>> ListCodeupBranchesAsync(string repositoryId, int page = 1,
        int perPage = 20, string? search = null, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string>
        {
            ["page"] = page.ToString(),
            ["perPage"] = perPage.ToString(),
        };
        if (!string.IsNullOrEmpty(search)) query["search"] = search;

        var raw = await CallPatAsync(HttpMethod.Get,
            "/oapi/v1/codeup/organizations/{0}/repositories/" + Uri.EscapeDataString(repositoryId) + "/branches",
            query: query, ct: ct) ?? "[]";
        return DeserializeList<YunxiaoCodeupBranch>(raw);
    }
    #endregion

    #region 通用远程请求
    /// <summary>
    /// 对单个工作项发起请求（GET 详情 / PUT 更新 / DELETE 删除）。
    /// 两种认证方案的路径前缀不同：ROA 网关 /organization/{org}/workitems/{id}（更新为 …/update），
    /// PAT 网关 /oapi/v1/projex/organizations/{org}/workitems/{id}。
    /// </summary>
    private Task<string?> SendWorkItemAsync(HttpMethod method, string workItemId, object? body = null,
        string roaSuffix = "", CancellationToken ct = default)
        => _akClient is not null
            ? CallRoaAsync(method, "/organization/{0}/workitems/" + Uri.EscapeDataString(workItemId) + roaSuffix, body, ct: ct)
            : CallPatAsync(method, "/oapi/v1/projex/organizations/{0}/workitems/" + Uri.EscapeDataString(workItemId), body, ct: ct);

    /// <summary>
    /// 底层直调任意云效 OpenAPI：按当前认证方案自动选择网关并完成认证（AK 签名 / PAT 头）。
    /// pathTemplate 中的 {0} 会被替换为 OrganizationId；返回原始 JSON 字符串。
    /// </summary>
    public Task<string?> SendAsync(HttpMethod method, string pathTemplate, object? body = null,
        IDictionary<string, string>? query = null, CancellationToken ct = default)
        => _akClient is not null
            ? CallRoaAsync(method, pathTemplate, body, query, ct)
            : CallPatAsync(method, pathTemplate, body, query, ct);

    /// <summary>兼容两种列表响应：裸数组（oapi/v1）或 { items = [...] } 包装（老版 ROA）</summary>
    private static List<T> DeserializeList<T>(string raw)
    {
        var token = JToken.Parse(raw);
        var arr = token as JArray ?? token["items"] as JArray;
        return arr is null ? [] : arr.ToObject<List<T>>(JsonSerializer.Create(JsonSettings)) ?? [];
    }

    /// <summary>
    /// 方案一：V2.0 通用 SDK 泛化调用（ROA 风格，SDK 自动完成 ACS V3 签名），返回原始 JSON 字符串。
    /// </summary>
    private async Task<string?> CallRoaAsync(HttpMethod method, string pathTemplate, object? body = null,
        IDictionary<string, string>? query = null, CancellationToken ct = default)
    {
        var @params = new Params
        {
            Action = "YunxiaoRoaRequest",
            Version = YunxiaoOptions.ApiVersion,
            Protocol = "HTTPS",
            Pathname = string.Format(pathTemplate, _options.OrganizationId),
            Method = method.Method,
            AuthType = "AK",
            Style = "ROA",
            ReqBodyType = "json",
            BodyType = "json",
        };

        var request = new OpenApiRequest
        {
            Query = query is null ? null : new Dictionary<string, string>(query),
            Body = body,
        };

        try
        {
            // 0.2.x 通用 SDK 的 CallApiAsync 直接返回解析后的 JSON Body
            var resp = await _akClient!.CallApiAsync(@params, request, new RuntimeOptions());
            return resp is null ? null : JsonConvert.SerializeObject(resp);
        }
        catch (Tea.TeaException ex)
        {
            throw new YunxiaoException($"云效 API 调用失败: {ex.Message}", null);
        }
    }

    /// <summary>
    /// 方案二：个人访问令牌直连（x-yunxiao-token 头，无需签名），返回原始 JSON 字符串。
    /// </summary>
    private async Task<string?> CallPatAsync(HttpMethod method, string pathTemplate, object? body = null,
        IDictionary<string, string>? query = null, CancellationToken ct = default)
    {
        var path = string.Format(pathTemplate, _options.OrganizationId);
        var uri = new UriBuilder($"https://{YunxiaoOptions.PatEndpoint}{path}");
        if (query is { Count: > 0 })
            uri.Query = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        using var req = new HttpRequestMessage(method, uri.Uri);
        req.Headers.Add("x-yunxiao-token", _options.PersonalAccessToken);
        if (body is not null)
        {
            req.Content = new StringContent(JsonConvert.SerializeObject(body, JsonSettings), Encoding.UTF8, "application/json");
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new YunxiaoException($"云效 API 调用失败({(int)resp.StatusCode}): {text}", (int)resp.StatusCode);
        return string.IsNullOrEmpty(text) ? null : text;
    } 
    #endregion
}
