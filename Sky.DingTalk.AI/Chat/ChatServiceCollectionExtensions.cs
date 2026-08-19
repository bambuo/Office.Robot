using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using Sikiro.YunXiao;

namespace Sky.DingTalk.AI.Chat;

/// <summary>钉钉机器人 AI Agent 服务注册扩展</summary>
public static class ChatServiceCollectionExtensions
{
    /// <summary>
    /// 注册钉钉机器人 AI Agent（基于 Microsoft.Agents.AI）：
    ///   - IChatClient（OpenAI 兼容接口，如 DeepSeek / 智谱 / OpenAI，模型须支持工具调用）
    ///   - DingTalkRobotAgent（多轮对话 + 自动调用已注册的工具，按群保留会话上下文）
    /// 依赖 AddYunxiao 注册的 YunxiaoClient（云效工具执行能力），请先调用 AddYunxiao。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">Agent 配置（Key / Endpoint / Model）</param>
    public static IServiceCollection AddDingTalkRobotAgent(this IServiceCollection services, Action<DingTalkRobotAgentOptions> configure)
    {
        var ai = new DingTalkRobotAgentOptions();
        configure(ai);

        services.AddSingleton<IChatClient>(_ =>
            new OpenAIClient(new ApiKeyCredential(ai.ApiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(ai.Endpoint),
            }).GetChatClient(ai.Model).AsIChatClient());

        services.AddSingleton(sp => new DingTalkRobotAgent(
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<YunxiaoClient>(),
            sp.GetRequiredService<ILogger<DingTalkRobotAgent>>()));

        return services;
    }
}
