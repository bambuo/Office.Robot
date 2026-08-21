namespace Office.Platform.YunXiao.Models;

/// <summary>
/// 代码提交（Commit）。列表接口返回精简信息，详情接口返回完整信息（含变更统计）。
/// </summary>
public sealed class YunxiaoCodeupCommit
{
    /// <summary>提交 SHA（详情接口使用）</summary>
    public string? Id { get; set; }

    /// <summary>短 SHA（列表展示用）</summary>
    public string? ShortId { get; set; }

    /// <summary>提交标题（第一行）</summary>
    public string? Title { get; set; }

    /// <summary>完整提交信息</summary>
    public string? Message { get; set; }

    /// <summary>作者姓名</summary>
    public string? AuthorName { get; set; }

    /// <summary>作者邮箱</summary>
    public string? AuthorEmail { get; set; }

    /// <summary>提交时间（ISO 8601）</summary>
    public string? CreatedAt { get; set; }

    /// <summary>父提交 SHA 列表</summary>
    public List<string>? ParentIds { get; set; }

    /// <summary>变更统计（详情接口返回）</summary>
    public YunxiaoCodeupCommitStats? Stats { get; set; }

    /// <summary>提交在 Codeup 页面的地址</summary>
    public string? WebUrl { get; set; }
}

/// <summary>提交变更统计：增删行数</summary>
public sealed class YunxiaoCodeupCommitStats
{
    /// <summary>新增行数</summary>
    public int? Additions { get; set; }

    /// <summary>删除行数</summary>
    public int? Deletions { get; set; }

    /// <summary>变更文件数</summary>
    public int? Total { get; set; }
}
