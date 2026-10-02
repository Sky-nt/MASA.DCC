// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Dcc.Service.Admin.Infrastructure.StandaloneAuth;

/// <summary>
/// 独立部署模式（不依赖 MASA Auth/SSO）下的自研鉴权配置。
/// 通过环境变量 Standalone__Enabled / Standalone__JwtSecret 等开启。
/// </summary>
public class StandaloneAuthOptions
{
    public const string SectionName = "Standalone";

    /// <summary>是否启用独立鉴权模式</summary>
    public bool Enabled { get; set; }

    /// <summary>签发/校验 JWT 的对称密钥（HS256），Web 后台与 API 必须一致</summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>JWT 签发者</summary>
    public string Issuer { get; set; } = "masa-dcc-standalone";

    /// <summary>内置管理员账号</summary>
    public string AdminUserName { get; set; } = "admin";

    /// <summary>内置管理员密码</summary>
    public string AdminPassword { get; set; } = "admin123";

    /// <summary>令牌有效期（分钟）</summary>
    public int TokenExpireMinutes { get; set; } = 1440;
}
