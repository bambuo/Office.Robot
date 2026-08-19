using Microsoft.Extensions.DependencyInjection;

namespace Sikiro.YunXiao.Extensions;

/// <summary>云效服务注册扩展</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册云效客户端：
    ///   - YunxiaoOptions（单例，AK / PAT 二选一配置）
    ///   - YunxiaoClient（单例，线程安全可复用）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">云效配置（组织 ID + AccessKey 或 个人访问令牌）</param>
    public static IServiceCollection AddYunxiao(this IServiceCollection services, Action<YunxiaoOptions> configure)
    {
        var options = new YunxiaoOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton(sp => new YunxiaoClient(sp.GetRequiredService<YunxiaoOptions>()));
        return services;
    }
}
