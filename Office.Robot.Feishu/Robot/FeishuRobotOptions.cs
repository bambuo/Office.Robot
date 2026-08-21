namespace Office.Robot.Feishu.Robot;

/// <summary>
/// 飞书机器人配置。
/// 凭证在飞书开放平台「应用开发 → 凭证与基础信息」页面获取。
/// </summary>
public sealed class FeishuRobotOptions
{
    /// <summary>飞书应用 AppId</summary>
    public string AppId { get; set; } = "";

    /// <summary>飞书应用 AppSecret</summary>
    public string AppSecret { get; set; } = "";
}