// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using Masa.BuildingBlocks.Isolation;
using Masa.Contrib.Service.Caller.HttpClient;
using Microsoft.Extensions.DependencyInjection;
using DiCallerBuilderExtensions = Microsoft.Extensions.DependencyInjection.MasaCallerClientBuilderExtensions;
using BbCallerBuilderExtensions = Masa.BuildingBlocks.Service.Caller.MasaCallerClientBuilderExtensions;

namespace Masa.Dcc.Caller;

public abstract class DccHttpClientCallerBase : HttpClientCallerBase
{
    /// <summary>
    /// 独立部署模式开关。开启后调用方不再用 MASA Auth(SSO) 校验令牌，
    /// 而是直接携带由 <see cref="ITokenGenerater"/> 提供的本地令牌（HTTP Bearer 头）。
    /// 需在注册 Caller 之前设置。
    /// </summary>
    public static bool UseStandaloneAuth { get; set; }

    protected DccHttpClientCallerBase(DccApiGatewayOptions options)
    {
        BaseAddress = options.DccServiceAddress;
    }

    protected override string BaseAddress { get; set; }

    protected override void UseHttpClientPost(MasaHttpClientBuilder masaHttpClientBuilder)
    {
        var builder = (IMasaCallerClientBuilder)masaHttpClientBuilder;

        if (UseStandaloneAuth)
        {
            // 空校验器：直接把 ITokenGenerater 生成的令牌放进 Authorization 头
            DiCallerBuilderExtensions.UseAuthentication(builder);
        }
        else
        {
            BbCallerBuilderExtensions.UseAuthentication(builder, serviceProvider => new AuthenticationService(
                serviceProvider.GetRequiredService<TokenProvider>(),
                serviceProvider.GetRequiredService<JwtTokenValidator>(),
                serviceProvider.GetRequiredService<IMultiEnvironmentContext>()));
        }

        base.UseHttpClientPost(masaHttpClientBuilder);
    }
}
