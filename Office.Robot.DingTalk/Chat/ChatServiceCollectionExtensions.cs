using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using Office.Platform.GitHub;
using Office.Platform.YunXiao;

namespace Office.Robot.DingTalk.Chat;

/// <summary>机器人 AI Agent 服务注册扩展</summary>
public static class ChatServiceCollectionExtensions
{
    /// <summary>
    /// 注册机器人 AI Agent（基于 Microsoft.Agents.AI）：
    ///   - IChatClient（OpenAI 兼容接口，如 DeepSeek / 智谱 / OpenAI，模型须支持工具调用）
    ///   - RobotAgent（多轮对话 + 自动调用已注册的工具，按群保留会话上下文）
    /// 依赖 AddYunxiao 注册的 YunxiaoClient、AddGitHub 注册的 GitHubClient（工具执行能力），请先调用。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">Agent 配置（Key / Endpoint / Model）</param>
    public static IServiceCollection AddRobotAgent(this IServiceCollection services, Action<RobotAgentOptions> configure)
    {
        var ai = new RobotAgentOptions();
        configure(ai);

        services.AddSingleton<IChatClient>(_ =>
            new OpenAIClient(new ApiKeyCredential(ai.ApiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(ai.Endpoint),
            }).GetChatClient(ai.Model).AsIChatClient());

        services.AddSingleton(sp => new RobotAgent(
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<YunxiaoClient>(),
            sp.GetRequiredService<GitHubClient>(),
            sp.GetRequiredService<ILogger<RobotAgent>>()));

        return services;
    }

    /// <summary>
    /// 注册 GitHub 客户端（提供 GitHub 只读查询工具的执行能力）。
    /// 依赖 appsettings.json 的 GitHub:Token 配置。
    /// </summary>
    public static IServiceCollection AddGitHub(this IServiceCollection services, Action<GitHubOptions> configure)
    {
        var github = new GitHubOptions();
        configure(github);
        services.AddSingleton(new GitHubClient(github));
        return services;
    }
}
