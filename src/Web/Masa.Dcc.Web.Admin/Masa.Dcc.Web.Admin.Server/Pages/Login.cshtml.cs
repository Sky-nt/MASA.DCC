// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Masa.BuildingBlocks.StackSdks.Config;
using Masa.BuildingBlocks.StackSdks.Config.Consts;
using Masa.Dcc.ApiGateways.Caller;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Masa.Dcc.Web.Admin.Server.Pages;

/// <summary>
/// 独立部署模式下的自研登录页：校验内置管理员账号，调用 DCC API 换取本地 JWT，
/// 并用 Cookie 保存登录态与 access_token（供 Blazor 侧 Caller 使用）。
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DccApiGatewayOptions _gatewayOptions;
    private readonly IMasaStackConfig _masaStackConfig;
    private readonly StandaloneOptions _options;

    public LoginModel(
        IHttpClientFactory httpClientFactory,
        DccApiGatewayOptions gatewayOptions,
        IMasaStackConfig masaStackConfig,
        IOptions<StandaloneOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _gatewayOptions = gatewayOptions;
        _masaStackConfig = masaStackConfig;
        _options = options.Value;
    }

    [BindProperty]
    public string? UserName { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        if (!_options.Enabled)
            return Redirect("/");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return Redirect("/");

        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "请输入用户名和密码";
            return Page();
        }

        var requestUri = $"{_gatewayOptions.DccServiceAddress.TrimEnd('/')}/api/v1/standalone/login";
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(requestUri, new { UserName, Password }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Error = "用户名或密码错误";
            return Page();
        }

        var result = await response.Content.ReadFromJsonAsync<StandaloneLoginResponse>(cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken))
        {
            Error = "登录失败：服务端未返回令牌";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, UserName!),
            new(IdentityClaimConsts.USER_ID, result.UserId.ToString()),
            new(IdentityClaimConsts.USER_NAME, UserName!),
            new(IdentityClaimConsts.ACCOUNT, UserName!),
            new(IdentityClaimConsts.ROLES, JsonSerializer.Serialize(new[] { "admin" })),
            new(IdentityClaimConsts.ENVIRONMENT, _masaStackConfig.Environment ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties();
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = result.AccessToken }
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        return Redirect("/");
    }
}
