using FeishuNetSdk;
using FeishuNetSdk.Contact;
using FeishuNetSdk.Im;
using FeishuNetSdk.Im.Events;
using FeishuNetSdk.Services;
using Microsoft.Extensions.Logging;
using Office.Robot.Feishu.Chat;

namespace Office.Robot.Feishu.Robot;

/// <summary>
/// 飞书 AI 机器人：接收 im.message.receive_v1 事件，将消息交给 RobotAgent 处理并回复。
/// Agent 会分析消息、自动调用已注册的工具（当前为云效工作项的创建/查询，能力可持续扩展），
/// 信息不全（如不知道是哪个项目）时回复用户补充，用户在同一会话里补充后继续处理。
/// 依赖 AddRobotAgent 注册的 RobotAgent 服务。
///
/// 注意：飞书长连接模式要求事件回调在 3 秒内消费完成（SDK 超时 3.5 秒），
/// 而 LLM 调用 + 工具执行通常需要数秒，因此 ExecuteAsync 只做快速应答，
/// 实际处理与回复在后台异步执行。
/// </summary>
public class FeishuRobotMessageHandler(
    RobotAgent agent,
    IFeishuTenantApi tenantApi,
    ILogger<FeishuRobotMessageHandler> logger) : IEventHandler<EventV2Dto<ImMessageReceiveV1EventBodyDto>, ImMessageReceiveV1EventBodyDto>
{
    public Task ExecuteAsync(EventV2Dto<ImMessageReceiveV1EventBodyDto> eventBody,
        CancellationToken cancellationToken = default)
    {
        // 快速应答事件，避免超过飞书 3 秒处理限制；AI 处理与回复在后台执行。
        // 不沿用 SDK 的取消令牌（3.5 秒后会被取消），后台任务使用独立令牌。
        _ = Task.Run(() => ProcessAsync(eventBody), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <summary>后台处理：解析消息 → 查发起人昵称 → AI Agent 生成回答 → 发送到群</summary>
    private async Task ProcessAsync(EventV2Dto<ImMessageReceiveV1EventBodyDto> eventBody)
    {
        try
        {
            var message = eventBody.Event?.Message;
            if (message is null)
                return;

            // 只处理群聊消息，忽略单聊
            if (message.ChatType != "group")
                return;

            // 只处理文本消息
            if (message.MessageType != "text")
                return;

            // 提取消息内容（飞书文本消息 content 为 JSON：{"text":"..."}）
            var text = ExtractText(message.Content);
            if (string.IsNullOrWhiteSpace(text))
                return;

            // 查发起人昵称（供 Agent 在用户未指定负责人时默认使用），失败则降级为 null
            var senderName = await ResolveSenderNameAsync(eventBody.Event?.Sender?.SenderId?.OpenId);

            // 调用 AI Agent 处理
            var chatId = message.ChatId ?? "";
            var answer = await agent.AskAsync(chatId, text, senderName);

            // 通过飞书 API 发送消息到群。
            // 用 interactive 卡片 + lark_md 渲染 Markdown；text 消息是纯文本，Markdown 会原样显示。
            var card = new
            {
                config = new { wide_screen_mode = true },
                elements = new object[]
                {
                    new
                    {
                        tag = "div",
                        text = new { tag = "lark_md", content = answer }
                    }
                }
            };
            var body = new PostImV1MessagesBodyDto
            {
                ReceiveId = chatId,
                MsgType = "interactive",
                Content = System.Text.Json.JsonSerializer.Serialize(card)
            };
            await tenantApi.PostImV1MessagesAsync("chat_id", body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理飞书消息失败");
        }
    }

    /// <summary>
    /// 按 open_id 查询发起人昵称。
    /// 需要应用具备「获取用户基本信息」权限（contact:user.base:readonly），
    /// 查询失败（无权限/用户已离职等）时返回 null，不影响消息处理。
    /// </summary>
    private async Task<string?> ResolveSenderNameAsync(string? openId)
    {
        if (string.IsNullOrEmpty(openId))
            return null;

        try
        {
            var resp = await tenantApi.GetContactV3UsersByUserIdAsync(openId, "open_id");
            return resp?.Data?.User?.Name;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "查询发起人昵称失败（openId: {OpenId}），将使用默认值", openId);
            return null;
        }
    }

    /// <summary>从飞书文本消息 content 中提取纯文本</summary>
    private static string ExtractText(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "";
        try
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(content);
            return dict?.GetValueOrDefault("text") ?? "";
        }
        catch
        {
            return "";
        }
    }
}