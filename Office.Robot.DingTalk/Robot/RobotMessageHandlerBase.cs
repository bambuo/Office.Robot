using Jusoft.DingtalkStream.Core;
using Jusoft.DingtalkStream.Robot;

namespace Office.Robot.DingTalk.Robot;

/// <summary>
/// 机器人消息处理器模板基类（模板方法模式）。
/// HandleMessage 固定了「过滤消息 → 提取文本 → 生成回答 → 回复 → 应答」的处理骨架，
/// 子类只需实现（或按需重写）其中的变化步骤：
///   - ProcessAsync：如何由用户消息生成回答（必须实现）
///   - CanHandle / GetTextContent / ReplyAsync：按需重写
/// </summary>
public abstract class RobotMessageHandlerBase : IDingtalkStreamMessageHandler
{
    /// <summary>模板方法：钉钉 Stream 消息处理骨架，不要重写</summary>
    public async Task HandleMessage(MessageEventHanderArgs e)
    {
        // 1. 过滤：只处理机器人消息回调
        if (!CanHandle(e))
            return;

        // 2. 提取消息内容
        var message = e.GetRobotMessageData();
        var content = GetTextContent(message);

        // 3. 生成回答（子类实现）
        var answer = await ProcessAsync(message, content);

        // 4. 通过 sessionWebhook 回复用户
        await ReplyAsync(message, answer);

        // 5. 回复 ack，否则服务端会重推消息
        await AckAsync(e);
    }

    /// <summary>是否处理该消息，默认仅处理机器人消息回调（CALLBACK + robot topic）</summary>
    protected virtual bool CanHandle(MessageEventHanderArgs e)
        => e.Type == SubscriptionType.CALLBACK && e.Headers.IsRobotTopic();

    /// <summary>
    /// 从回调消息中提取文本内容，按消息类型分派：
    /// text → 纯文本；richText → 拼接全部文本段；其他类型（纯图片/音视频/文件）返回空字符串。
    /// </summary>
    protected virtual string GetTextContent(ReceivedRobotMessage message)
        => message.MsgType?.ToLowerInvariant() switch
        {
            "text" => message.GetTextContent().Content ?? "",
            "richtext" => string.Concat(message.GetRichTextContent().RichText?.Select(r => r.Text) ?? []),
            _ => "",
        };

    /// <summary>核心变化点：根据消息内容生成回答（如调用 AI、查云效 Bug 等）</summary>
    protected abstract Task<string> ProcessAsync(ReceivedRobotMessage message, string content);

    /// <summary>把回答发回给用户，默认通过 sessionWebhook 回复文本消息</summary>
    protected virtual Task ReplyAsync(ReceivedRobotMessage message, string answer)
        => DingtalkRobotWebhookUtilites.SendTextMessage(message.SessionWebhook, answer);

    /// <summary>向钉钉服务端应答，表示消息已消费</summary>
    private static async Task AckAsync(MessageEventHanderArgs e)
    {
        var reply = await DingtalkStreamUtilities.CreateReplyMessage(
            e.Headers.MessageId,
            DingtalkStreamUtilities.CreateReply_Callback_MessageData("success"));
        await e.Reply(reply);
    }
}
