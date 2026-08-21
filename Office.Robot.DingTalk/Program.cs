using Microsoft.Extensions.Hosting;
using Office.Platform.YunXiao.Extensions;
using Office.Robot.DingTalk.Chat;
using Office.Robot.DingTalk.Robot;

// 配置全部来自 appsettings.json（钉钉应用凭证 / AI 接口 / 云效），密钥不进源码
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
        .AddDingTalkRobot<DingTalkRobotMessageHandler>(dingTalk =>
        {
            dingTalk.ClientId = ctx.Configuration["DingTalk:ClientId"]!;
            dingTalk.ClientSecret = ctx.Configuration["DingTalk:ClientSecret"]!;
        }))
    .Build();

Console.WriteLine("钉钉 Stream 机器人已启动（云效 AI Agent）");
await host.RunAsync();
