using FeishuNetSdk;
using FeishuNetSdk.Im;
using FeishuNetSdk.Im.Events;
using FeishuNetSdk.Services;
using Microsoft.Extensions.DependencyInjection;
using Office.Robot.Feishu.Chat;

namespace Office.Robot.Feishu.Robot;

/// <summary>飞书机器人服务注册扩展</summary>
public static class FeishuServiceCollectionExtensions
{
    /// <summary>
    /// 注册飞书机器人所需的服务：
    ///   - FeishuNetSdk 客户端（自动 Token 缓存刷新）
    ///   - WebSocket 长连接（接收消息事件推送）
    ///   - 消息处理器 TRobotHandler（实现 IEventHandler 处理 im.message.receive_v1）
    /// 处理器若依赖 AI Agent，请先调用 AddRobotAgent。
    /// </summary>
    /// <typeparam name="TRobotHandler">消息事件处理器，须实现 IEventHandler</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configure">飞书应用凭证配置</param>
    public static IServiceCollection AddFeishuRobot<TRobotHandler>(
        this IServiceCollection services,
        Action<FeishuRobotOptions> configure)
        where TRobotHandler : class, IEventHandler<EventV2Dto<ImMessageReceiveV1EventBodyDto>, ImMessageReceiveV1EventBodyDto>
    {
        var feishu = new FeishuRobotOptions();
        configure(feishu);

        services
            .AddFeishuNetSdk(options =>
            {
                options.AppId = feishu.AppId;
                options.AppSecret = feishu.AppSecret;
            })
            .AddFeishuWebSocket();

        // 注册事件处理器到 DI，SDK 会自动发现并订阅
        services.AddSingleton<TRobotHandler>();

        return services;
    }
}