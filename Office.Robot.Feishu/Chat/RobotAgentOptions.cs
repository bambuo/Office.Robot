namespace Office.Robot.Feishu.Chat;

/// <summary>
/// 机器人 AI Agent 配置（任意 OpenAI 兼容接口：DeepSeek / 智谱 / OpenAI 等）。
/// 所配置的模型必须支持 Function Calling（工具调用），否则 Agent 无法调用工具。
/// </summary>
public sealed class RobotAgentOptions
{
    /// <summary>API Key（sk- 等开头，由服务商颁发）</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>接口地址，如 https://api.deepseek.com</summary>
    public string Endpoint { get; set; } = "";

    /// <summary>模型名，如 deepseek-chat / glm-4 等（须支持工具调用）</summary>
    public string Model { get; set; } = "";
}