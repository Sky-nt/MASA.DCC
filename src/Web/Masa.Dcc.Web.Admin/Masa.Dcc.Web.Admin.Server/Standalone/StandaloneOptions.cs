// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Dcc.Web.Admin.Server.Standalone;

/// <summary>
/// 独立部署模式（不依赖 MASA Auth/SSO）配置，取值需与 DCC API 的 Standalone 配置一致。
/// </summary>
public class StandaloneOptions
{
    public const string SectionName = "Standalone";

    public bool Enabled { get; set; }

    public string JwtSecret { get; set; } = string.Empty;

    public int TokenExpireMinutes { get; set; } = 1440;
}

public class StandaloneLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }

    public Guid UserId { get; set; }
}
