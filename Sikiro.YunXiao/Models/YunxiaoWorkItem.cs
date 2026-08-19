using Newtonsoft.Json;

namespace Sikiro.YunXiao.Models;

/// <summary>
/// 工作项（Bug / 需求 / 任务）。
/// 创建后自动回查详情、查询列表、查询单个详情均返回该实体。
/// </summary>
public sealed class YunxiaoWorkItem
{
    /// <summary>工作项 ID（查详情 / 更新 / 删除时使用）</summary>
    public string? Id { get; set; }

    /// <summary>业务编号（项目编码 + 序号，如 EYMV-1），项目内唯一且对用户可见</summary>
    public string? SerialNumber { get; set; }

    /// <summary>标题</summary>
    public string? Subject { get; set; }

    /// <summary>描述（Markdown）</summary>
    public string? Description { get; set; }

    /// <summary>描述格式：MARKDOWN 等</summary>
    public string? FormatType { get; set; }

    /// <summary>逻辑状态：NORMAL（正常）/ DELETED（已删除，回收站）</summary>
    public string? LogicalStatus { get; set; }

    /// <summary>所属大类：Req / Task / Bug</summary>
    public string? CategoryId { get; set; }

    /// <summary>状态阶段：1（进行中之前）/ 2（进行中）/ 3（已完成）</summary>
    public string? StatusStageId { get; set; }

    /// <summary>父工作项 ID（支持子工作项/需求拆解，无则为 null）</summary>
    public string? ParentId { get; set; }

    /// <summary>创建时间（Unix 毫秒时间戳）</summary>
    public long? GmtCreate { get; set; }

    /// <summary>最后修改时间（Unix 毫秒时间戳）</summary>
    public long? GmtModified { get; set; }

    /// <summary>负责人</summary>
    public YunxiaoUser? AssignedTo { get; set; }

    /// <summary>创建人</summary>
    public YunxiaoUser? Creator { get; set; }

    /// <summary>最后修改人</summary>
    public YunxiaoUser? Modifier { get; set; }

    /// <summary>当前状态</summary>
    public YunxiaoWorkItemStatus? Status { get; set; }

    /// <summary>所属项目（Id 即 spaceId）</summary>
    public YunxiaoNamedRef? Space { get; set; }

    /// <summary>工作项类型（如 缺陷 / 线上故障 / 任务）</summary>
    public YunxiaoNamedRef? WorkitemType { get; set; }

    /// <summary>所属迭代（未指定则为 null）</summary>
    public YunxiaoNamedRef? Sprint { get; set; }

    /// <summary>自定义字段取值（优先级、严重程度、标签等）</summary>
    public List<YunxiaoCustomFieldValue> CustomFieldValues { get; set; } = [];

    /// <summary>
    /// 工作项在云效 Projex 页面的地址（浏览器可直接打开），
    /// 由站点 + 项目 ID + 大类 + 工作项 ID 拼接而成。
    /// </summary>
    [JsonIgnore]
    public string? Url => Space?.Id is null || CategoryId is null || Id is null
        ? null
        : $"{YunxiaoOptions.WebEndpoint}/projex/project/{Space.Id}/{CategoryId.ToLowerInvariant()}/{Id}";
}
