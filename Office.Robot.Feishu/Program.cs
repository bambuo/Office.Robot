using Microsoft.Extensions.Hosting;
using Office.Platform.YunXiao.Extensions;
using Office.Robot.Feishu.Chat;
using Office.Robot.Feishu.Robot;

// 配置全部来自 appsettings.json（飞书应用凭证 / AI 接口 / 云效），密钥不进源码
// 以程序目录为 content root，确保从任何工作目录启动都能加载 appsettings.json
var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((ctx, services) => services
        .AddYunxiao(o =>
        {
            o.OrganizationId = ctx.Configuration["Yunxiao:OrganizationId"]!;
            o.PersonalAccessToken = ctx.Configuration["Yunxiao:PersonalAccessToken"]!;
        })
        .AddGitHub(o =>
        {
            o.Token = ctx.Configuration["GitHub:Token"]!;
        })
        .AddRobotAgent(ai =>
        {
            ai.ApiKey = ctx.Configuration["Ai:ApiKey"]!;
            ai.Endpoint = ctx.Configuration["Ai:Endpoint"]!;
            ai.Model = ctx.Configuration["Ai:Model"]!;
        })
        .AddFeishuRobot<FeishuRobotMessageHandler>(feishu =>
        {
            feishu.AppId = ctx.Configuration["Feishu:AppId"]!;
            feishu.AppSecret = ctx.Configuration["Feishu:AppSecret"]!;
        }))
    .Build();

Console.WriteLine("飞书机器人已启动（云效 AI Agent）");
await host.RunAsync();