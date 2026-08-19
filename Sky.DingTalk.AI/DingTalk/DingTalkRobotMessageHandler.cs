using Jusoft.DingtalkStream.Robot;
using Sky.DingTalk.AI.Chat;

namespace Sky.DingTalk.AI.DingTalk;

/// <summary>
/// 钉钉 AI 机器人：把用户消息交给 DingTalkRobotAgent 处理。
/// Agent 会分析消息、自动调用已注册的工具（当前为云效工作项的创建/查询，能力可持续扩展），
/// 信息不全（如不知道是哪个项目）时回复用户补充，用户在同一会话里补充后继续处理。
/// 依赖 AddDingTalkRobotAgent 注册的 DingTalkRobotAgent 服务。
/// </summary>
public class DingTalkRobotMessageHandler(DingTalkRobotAgent agent) : RobotMessageHandlerBase
{
    /// <summary>按群（ConversationId）保留会话上下文，连同发起人昵称交给 Agent 处理（含工具调用）</summary>
    protected override Task<string> ProcessAsync(ReceivedRobotMessage message, string content)
        => agent.AskAsync(message.ConversationId, content, message.SenderNick);
}
