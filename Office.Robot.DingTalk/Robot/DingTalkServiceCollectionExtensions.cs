using Jusoft.DingtalkStream.Core;
using Jusoft.DingtalkStream.Robot;
using Microsoft.Extensions.DependencyInjection;

namespace Office.Robot.DingTalk.Robot;

/// <summary>钉钉机器人服务注册扩展</summary>
public static class DingTalkServiceCollectionExtensions
{
    /// <summary>
    /// 注册钉钉 Stream 机器人所需的服务：
    ///   - 钉钉 Stream 客户端（长连接接收机器人消息回调）
    ///   - 消息处理器 TRobotHandler（继承 RobotMessageHandlerBase）
    /// 处理器若依赖 AI Agent，请先调用 AddRobotAgent。
    /// </summary>
    /// <typeparam name="TRobotHandler">机器人消息处理器，须继承 RobotMessageHandlerBase</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configure">钉钉应用凭证配置</param>
    public static IServiceCollection AddDingTalkRobot<TRobotHandler>(
        this IServiceCollection services,
        Action<DingTalkRobotOptions> configure)
        where TRobotHandler : RobotMessageHandlerBase
    {
        var dingTalk = new DingTalkRobotOptions();
        configure(dingTalk);

        services.AddDingtalkStream(options =>
            {
                options.ClientId = dingTalk.ClientId;
                options.ClientSecret = dingTalk.ClientSecret;
                options.AutoReplySystemMessage = dingTalk.AutoReplySystemMessage; // 自动处理 SYSTEM 心跳消息
            })
            .RegisterIMRobotMessageCallback()          // 订阅机器人消息 topic
            .AddMessageHandler<TRobotHandler>()        // 挂上消息处理器
            .AddHostServices();                        // 启动 Stream 客户端

        return services;
    }
}
