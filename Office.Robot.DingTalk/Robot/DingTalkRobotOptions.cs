namespace Office.Robot.DingTalk.Robot;

/// <summary>
/// 钉钉机器人（Stream 模式）配置。
/// 凭证在钉钉开放平台「应用开发 → 凭证与基础信息」页面获取。
/// </summary>
public sealed class DingTalkRobotOptions
{
    /// <summary>应用 ClientId（即 AppKey / AgentId）</summary>
    public string ClientId { get; set; } = "";

    /// <summary>应用 ClientSecret（即 AppSecret）</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>是否自动回复 SYSTEM 心跳消息（保持默认 true 即可）</summary>
    public bool AutoReplySystemMessage { get; set; } = true;
}
