using System.ComponentModel;
using Microsoft.Extensions.AI;
using Sikiro.YunXiao;
using Sikiro.YunXiao.Enums;
using Sikiro.YunXiao.Models;

namespace Sky.DingTalk.AI.Chat;

/// <summary>
/// 云效 Agent 工具集：把 YunxiaoClient 的能力包装成 AIFunction，供大模型在对话中自动调用。
/// 所有工具均返回中文文本（含失败原因），便于模型直接组织钉钉回复。
/// </summary>
public static class YunxiaoAgentTools
{
    /// <summary>基于云效客户端构造 Agent 可用的工具列表</summary>
    public static IList<AITool> Create(YunxiaoClient client)
    {
        var functions = new YunxiaoToolFunctions(client);
        return
        [
            AIFunctionFactory.Create(functions.ListProjects),
            AIFunctionFactory.Create(functions.ListProjectMembers),
            AIFunctionFactory.Create(functions.CreateWorkItem),
            AIFunctionFactory.Create(functions.ListWorkItems),
        ];
    }

    /// <summary>工具的具体实现（持有一个 YunxiaoClient）</summary>
    private sealed class YunxiaoToolFunctions(YunxiaoClient client)
    {
        [Description("列出云效组织下的全部项目名称。当用户没说清是哪个项目、或不确定项目名是否正确时调用。")]
        public async Task<string> ListProjects()
        {
            var projects = await client.ListProjectsAsync(perPage: 100);
            return projects.Count == 0
                ? "组织下没有查询到任何项目"
                : "项目列表：" + string.Join("、", projects.Select(p => p.Name));
        }

        [Description("列出指定项目的全部成员姓名。当用户想指定负责人、但不确定成员姓名时调用。")]
        public async Task<string> ListProjectMembers(
            [Description("项目名称")] string projectName)
        {
            var (error, project) = await FindProjectAsync(projectName);
            if (error is not null) return error;

            var members = await client.ListProjectMembersAsync(project!.Id!);
            return members.Count == 0
                ? $"项目「{projectName}」没有成员"
                : $"项目「{projectName}」成员：" + string.Join("、", members.Select(m => m.UserName));
        }

        [Description("在云效创建工作项（Bug 缺陷 / Req 需求 / Task 任务），成功后返回可直接打开的工作项地址。")]
        public async Task<string> CreateWorkItem(
            [Description("工作项类型：Bug=缺陷、Req=需求、Task=任务（也兼容中文：缺陷/需求/任务）")] string category,
            [Description("标题：一句话概括问题或需求")] string subject,
            [Description("项目名称：必须是云效中真实存在的项目名，不确定时先用 ListProjects 查询")] string projectName,
            [Description("负责人姓名（项目成员的真实姓名），可不传，默认取消息标注的发起人（@机器人的用户）")] string? assignedTo = null,
            [Description("详细描述（Markdown），可不传")] string? description = null)
        {
            if (!TryParseCategory(category, out var parsed))
                return $"创建失败：无法识别的工作项类型「{category}」，仅支持 Bug（缺陷）/ Req（需求）/ Task（任务）";

            try
            {
                var url = await client.QuickCreateWorkItemAsync(parsed, subject, projectName, assignedTo, description);
                return $"创建成功，工作项地址：{url}";
            }
            catch (YunxiaoException ex)
            {
                return $"创建失败：{ex.Message}";
            }
        }

        [Description("查询指定项目某类工作项的最近列表（编号、标题、状态），默认查缺陷。")]
        public async Task<string> ListWorkItems(
            [Description("项目名称")] string projectName,
            [Description("工作项类型：Bug=缺陷、Req=需求、Task=任务，可不传默认 Bug")] string category = "Bug")
        {
            var (error, project) = await FindProjectAsync(projectName);
            if (error is not null) return error;
            if (!TryParseCategory(category, out var parsed))
                return $"查询失败：无法识别的工作项类型「{category}」";

            var items = await client.ListWorkItemsAsync(parsed, project!.Id!, maxResults: 10);
            if (items.Count == 0)
                return $"项目「{projectName}」暂未查到该类型的工作项（刚创建的可能有延迟）";

            return string.Join("\n", items.Select(i =>
                $"- {i.SerialNumber} {i.Subject}（{i.Status?.Name ?? "未知状态"}，负责人：{i.AssignedTo?.Name ?? "未分配"}）"));
        }

        /// <summary>按名称在组织下查找项目，找不到时返回错误提示（供模型转告用户）</summary>
        private async Task<(string? Error, YunxiaoProject? Project)> FindProjectAsync(string projectName)
        {
            var projects = await client.ListProjectsAsync(perPage: 100);
            var project = projects.FirstOrDefault(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
            return project is null
                ? ($"未找到项目「{projectName}」，可用项目：{string.Join("、", projects.Select(p => p.Name))}", null)
                : (null, project);
        }

        /// <summary>工作项类型别名表：英文枚举值（不区分大小写）与中文</summary>
        private static readonly Dictionary<string, YunxiaoWorkItemCategory> CategoryAliases = new()
        {
            ["bug"] = YunxiaoWorkItemCategory.Bug,
            ["缺陷"] = YunxiaoWorkItemCategory.Bug,
            ["req"] = YunxiaoWorkItemCategory.Requirement,
            ["需求"] = YunxiaoWorkItemCategory.Requirement,
            ["task"] = YunxiaoWorkItemCategory.Task,
            ["任务"] = YunxiaoWorkItemCategory.Task,
        };

        /// <summary>解析工作项类型，别名见表 CategoryAliases；无法识别返回 false</summary>
        private static bool TryParseCategory(string category, out YunxiaoWorkItemCategory result)
        {
            if (CategoryAliases.TryGetValue(category.Trim().ToLowerInvariant(), out var matched))
            {
                result = matched;
                return true;
            }
            result = default;
            return false;
        }
    }
}
