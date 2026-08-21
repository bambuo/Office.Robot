namespace Office.Platform.YunXiao.Models;

/// <summary>
/// 云效项目（Projex）。
/// Id 即各工作项接口中的 spaceId / projectId。
/// </summary>
public sealed class YunxiaoProject
{
    /// <summary>项目 ID（即 spaceId）</summary>
    public string? Id { get; set; }

    /// <summary>项目名称</summary>
    public string? Name { get; set; }

    /// <summary>项目业务编码（工作项编号前缀，如 EYMV）</summary>
    public string? CustomCode { get; set; }

    /// <summary>项目描述</summary>
    public string? Description { get; set; }

    /// <summary>可见范围：public（组织内公开）/ private（私有）</summary>
    public string? Scope { get; set; }

    /// <summary>逻辑状态：NORMAL（正常）/ DELETED（已归档）</summary>
    public string? LogicalStatus { get; set; }

    /// <summary>项目状态（如「未开始」）</summary>
    public YunxiaoWorkItemStatus? Status { get; set; }

    /// <summary>创建人</summary>
    public YunxiaoUser? Creator { get; set; }

    /// <summary>创建时间（Unix 毫秒时间戳）</summary>
    public long? GmtCreate { get; set; }
}
