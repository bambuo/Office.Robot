using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Office.Platform.GitHub;
using Office.Platform.YunXiao;

namespace Office.Robot.Feishu.Chat;

/// <summary>
/// 机器人 AI Agent（基于 Microsoft.Agents.AI 的 ChatClientAgent）。
/// 大模型根据用户的群消息自动调用已注册的工具——当前内置云效工具集
/// （查项目 / 查成员 / 创建工作项 / 查工作项列表），后续可按需扩展更多机器人能力。
/// 信息不全时优先用合理默认值；仅当关键信息（如项目名）完全无法确定时才回复用户追问，
/// 用户在同一会话里补充后即可继续处理。
/// 每个群（按 ConversationId）独立会话，超过上限自动丢弃最旧的消息（滑动窗口），
/// 只保留最近一段上下文，防止 token 无限膨胀。
/// </summary>
public sealed class RobotAgent
{
    /// <summary>每个群最多保留的消息条数（约 20 轮对话），超过后丢弃最旧的，保留最近 N 条</summary>
    private const int MaxSessionMessages = 40;

    /// <summary>内置默认提示词：云效项目助手的角色、参数提取规则与追问策略</summary>
    private const string DefaultInstructions = """
        你是钉钉群里的「云效项目助手」，帮助团队成员把口语化的反馈落地成云效工作项。

        可用工具：ListProjects（查项目列表）、ListProjectMembers（查项目成员）、
        CreateWorkItem（创建工作项）、ListWorkItems（查工作项列表）、
        ListCodeupRepositories（查云效代码仓库）、ListCodeupCommits（查云效提交）、
        GetCodeupCommitDetail（查云效提交详情）、ListCodeupBranches（查云效分支）、
        ListGitHubRepositories（查 GitHub 仓库）、ListGitHubCommits（查 GitHub 提交）、
        ListGitHubPullRequests（查 GitHub PR）、ListGitHubIssues（查 GitHub Issue）、
        ListGitHubBranches（查 GitHub 分支）。

        处理用户消息的规则：
        1. 从消息中提取：项目名、工作项类型（Bug 缺陷 / Req 需求 / Task 任务）、标题、负责人、描述。
        2. 信息不全时优先用合理默认值，不要向用户二次确认：
           - 类型未提及 → 默认 Bug；
           - 描述未提及 → 把用户的原话整理成描述；
           - 负责人未提及 → 默认用消息标注的「发起人」（@机器人的用户）。
        3. 只有当「项目名」完全无法确定时（用户没说且上下文里没有），才回复用户请他补充是哪个项目；
           用户补充后必须结合上下文继续处理，不要重复追问。
        4. 不确定项目名是否真实存在时，先调用 ListProjects 核对（宁可多查一次，不要猜）。
        5. 创建成功后，回复一句话结果并附上工作项地址（URL）。
        6. 与云效无关的闲聊，友好地说明自己只负责云效工作项的创建与查询。
        回复使用简洁的中文，适合在群里阅读。
        """;

    private readonly ChatClientAgent _agent;
    private readonly ILogger<RobotAgent> _logger;

    /// <summary>按群隔离的会话表（键为 ConversationId，内存存储，进程重启后清空）</summary>
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    /// <summary>
    /// 构造机器人 Agent。
    /// </summary>
    /// <param name="chatClient">任意 OpenAI 兼容的聊天客户端（须支持工具调用）</param>
    /// <param name="yunxiao">云效客户端（提供云效工具的实际执行能力）</param>
    /// <param name="github">GitHub 客户端（提供 GitHub 工具的实际执行能力）</param>
    /// <param name="logger">日志</param>
    public RobotAgent(IChatClient chatClient, YunxiaoClient yunxiao, GitHubClient github, ILogger<RobotAgent> logger)
    {
        _agent = new ChatClientAgent(
            chatClient,
            instructions: DefaultInstructions,
            name: "robot-agent",
            description: "群机器人助手：分析群消息，支持云效工作项、云效代码仓库（Codeup）、GitHub 的查询能力，能力可持续扩展",
            tools: [.. YunxiaoAgentTools.Create(yunxiao), .. GitHubAgentTools.Create(github)]);
        _logger = logger;
    }

    /// <summary>
    /// 处理一条群消息，返回要回复的文本（会话上下文按群隔离）。
    /// 发起人昵称（@机器人的用户）会标注在消息开头，供模型在用户未指定负责人时默认使用。
    /// </summary>
    public async Task<string> AskAsync(string conversationId, string message, string? senderNick = null,
        CancellationToken ct = default)
    {
        try
        {
            var prompt = string.IsNullOrEmpty(senderNick) ? message : $"[发起人：{senderNick}]\n{message}";
            var session = await GetOrCreateSessionAsync(conversationId, ct);
            var response = await _agent.RunAsync(prompt, session, cancellationToken: ct);
            return string.IsNullOrWhiteSpace(response.Text)
                ? "（AI 没有返回内容，请换个说法再试）"
                : response.Text;
        }
        catch (Exception ex)
        {
            // 会话状态可能已不可用，重置该群以便下次重新开始
            _sessions.TryRemove(conversationId, out _);
            _logger.LogError(ex, "Agent Failed");
            return "AI 服务暂时不可用，请稍后再试或联系管理员。";
        }
    }

    /// <summary>
    /// 获取群会话；不存在则新建，超过上限则丢弃最旧的消息（保留最近 MaxSessionMessages 条）。
    /// 注意：会话必须用无参的 CreateSessionAsync（不带服务端 ConversationId），
    /// 否则会被框架视为"服务端托管聊天历史"模式，要求底层服务回传会话 ID
    /// （如 OpenAI Responses API）；DeepSeek 等普通 Chat Completions 接口不返回，
    /// 会直接抛 "Service did not return a valid conversation id..."。
    /// 多轮上下文改由下方 SetInMemoryChatHistory 在应用侧托管。
    /// </summary>
    private async Task<AgentSession> GetOrCreateSessionAsync(string conversationId, CancellationToken ct)
    {
        if (_sessions.TryGetValue(conversationId, out var session))
        {
            // 未超上限直接复用
            if (!session.TryGetInMemoryChatHistory(out var history) || history.Count <= MaxSessionMessages)
                return session;

            // 超过上限：丢弃最旧的消息，保留最近 N 条（滑动窗口）
            var trimmed = history.Skip(history.Count - MaxSessionMessages).ToList();
            // 截断处可能落在「工具调用 / 工具结果」中间，开头的孤儿 tool 消息会被接口拒绝，
            // 因此向前推进到第一条用户消息
            var firstUser = trimmed.FindIndex(m => m.Role == ChatRole.User);
            session.SetInMemoryChatHistory(firstUser > 0 ? trimmed.Skip(firstUser).ToList() : trimmed);
            return session;
        }

        session = await _agent.CreateSessionAsync(ct);
        // 启用内存聊天历史：Agent 每轮自动把新消息追加到会话，实现多轮追问补全
        session.SetInMemoryChatHistory([]);
        return _sessions[conversationId] = session;
    }
}