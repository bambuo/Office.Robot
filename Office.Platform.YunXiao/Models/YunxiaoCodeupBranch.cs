namespace Office.Platform.YunXiao.Models;

/// <summary>
/// 代码仓库分支。
/// </summary>
public sealed class YunxiaoCodeupBranch
{
    /// <summary>分支名</summary>
    public string? Name { get; set; }

    /// <summary>是否为默认分支（API 字段名 defaultBranch）</summary>
    public bool? DefaultBranch { get; set; }

    /// <summary>是否受保护分支</summary>
    public bool? Protected { get; set; }

    /// <summary>分支在 Codeup 页面的地址</summary>
    public string? WebUrl { get; set; }

    /// <summary>分支最新提交（部分接口返回）</summary>
    public YunxiaoCodeupCommit? Commit { get; set; }
}
