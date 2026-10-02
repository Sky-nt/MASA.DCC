// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Dcc.Service.Admin.Infrastructure.StandaloneAuth;

public class StandaloneLoginRequest
{
    public string? UserName { get; set; }

    public string? Password { get; set; }
}

public class StandaloneLoginResult
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresIn { get; set; }

    public Guid UserId { get; set; }
}
