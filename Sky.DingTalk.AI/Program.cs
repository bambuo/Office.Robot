using Microsoft.Extensions.Hosting;
using Sikiro.YunXiao.Extensions;
using Sky.DingTalk.AI.Chat;
using Sky.DingTalk.AI.DingTalk;

// 配置全部来自 appsettings.json（钉钉应用凭证 / AI 接口 / 云效），密钥不进源码
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) => services
        .AddYunxiao(o =>
        {
            o.OrganizationId = ctx.Configuration["Yunxiao:OrganizationId"]!;
            o.PersonalAccessToken = ctx.Configuration["Yunxiao:PersonalAccessToken"]!;
        })
        .AddDingTalkRobotAgent(ai =>
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

Console.WriteLine("DingTalk Stream 机器人已启动（云效 AI Agent）");
await host.RunAsync();
