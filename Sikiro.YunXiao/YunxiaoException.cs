namespace Sikiro.YunXiao;

/// <summary>云效 OpenAPI 调用异常（非 2xx 响应或 SDK 签名调用失败时抛出）</summary>
public sealed class YunxiaoException : Exception
{
    /// <summary>HTTP 状态码（SDK 内部错误时为 null）</summary>
    public int? HttpStatusCode { get; }

    public YunxiaoException(string message, int? statusCode = null) : base(message)
        => HttpStatusCode = statusCode;
}
