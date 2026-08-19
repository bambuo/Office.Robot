namespace Sikiro.YunXiao;

/// <summary>
/// 云效客户端配置。
/// 认证方案二选一：填 AccessKeyId/Secret 走方案一（AK 签名），填 PersonalAccessToken 走方案二（PAT）。
/// </summary>
public sealed class YunxiaoOptions
{
    /// <summary>方案一（AK 自签名）网关：云效 2021-06-25 ROA 接口</summary>
    public const string AkEndpoint = "devops.cn-hangzhou.aliyuncs.com";

    /// <summary>方案二（PAT）网关：云效 oapi/v1 中心版接口</summary>
    public const string PatEndpoint = "openapi-rdc.aliyuncs.com";

    /// <summary>API 版本（方案一 ROA 签名用）</summary>
    public const string ApiVersion = "2021-06-25";

    /// <summary>云效 Web 站点前缀（拼接工作项访问地址用）</summary>
    public const string WebEndpoint = "https://devops.aliyun.com";

    /// <summary>组织 ID（组织管理后台 → 基本信息页面获取）</summary>
    public string OrganizationId { get; set; } = "";

    /// <summary>方案一：阿里云 AccessKey ID（与 AccessKeySecret 成对出现）</summary>
    public string? AccessKeyId { get; set; }

    /// <summary>方案一：阿里云 AccessKey Secret</summary>
    public string? AccessKeySecret { get; set; }

    /// <summary>
    /// 方案二：云效个人访问令牌（pt- 开头）。
    /// 在云效「个人设置 → 个人访问令牌」创建，请求时通过 x-yunxiao-token 头携带，无需签名。
    /// </summary>
    public string? PersonalAccessToken { get; set; }
}
