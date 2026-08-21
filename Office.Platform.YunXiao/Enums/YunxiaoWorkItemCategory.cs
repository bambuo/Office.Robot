namespace Office.Platform.YunXiao.Enums;

/// <summary>
/// 云效工作项大类：需求 / 任务 / 缺陷。
/// 创建、查询工作项时用于指定所属大类，序列化为接口要求的 Req / Task / Bug。
/// </summary>
public enum YunxiaoWorkItemCategory
{
    /// <summary>需求（Req）</summary>
    Requirement,

    /// <summary>任务（Task）</summary>
    Task,

    /// <summary>缺陷（Bug）</summary>
    Bug,
}

/// <summary>大类到接口取值的映射扩展</summary>
internal static class YunxiaoWorkItemCategoryExtensions
{
    /// <summary>转换为云效 OpenAPI 认识的取值：Req / Task / Bug</summary>
    public static string ToApiValue(this YunxiaoWorkItemCategory category) => category switch
    {
        YunxiaoWorkItemCategory.Requirement => "Req",
        YunxiaoWorkItemCategory.Task => "Task",
        YunxiaoWorkItemCategory.Bug => "Bug",
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };
}
