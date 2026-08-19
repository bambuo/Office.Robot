namespace Sikiro.YunXiao.Models;

/// <summary>
/// 云效用户引用。
/// 出现在工作项的负责人（AssignedTo）、创建人（Creator）、修改人（Modifier）等位置；
/// Id 可直接作为创建工作项时的 assignedTo 参数。
/// </summary>
public sealed class YunxiaoUser
{
    /// <summary>用户 ID</summary>
    public string? Id { get; set; }

    /// <summary>用户名称（未设置昵称时为绑定邮箱/手机号）</summary>
    public string? Name { get; set; }
}
