// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Masa.BuildingBlocks.StackSdks.Config.Consts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Masa.Dcc.Service.Admin.Infrastructure.StandaloneAuth;

/// <summary>
/// 独立部署模式下自研的令牌签发服务（HS256 对称密钥）。
/// 令牌中的声明名与 MASA Identity 默认映射保持一致，
/// 以便 AddMasaIdentity / IUserContext.GetUser&lt;MasaUser&gt;() 能正确解析。
/// </summary>
public class StandaloneTokenService
{
    private readonly StandaloneAuthOptions _options;

    public StandaloneTokenService(IOptions<StandaloneAuthOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(string userName, string environment, Guid userId)
    {
        var claims = new List<Claim>
        {
            new(IdentityClaimConsts.USER_ID, userId.ToString()),
            new(IdentityClaimConsts.USER_NAME, userName),
            new(IdentityClaimConsts.ACCOUNT, userName),
            new(IdentityClaimConsts.ROLES, JsonSerializer.Serialize(new[] { "admin" })),
            new(IdentityClaimConsts.ENVIRONMENT, environment ?? string.Empty),
            new(IdentityClaimConsts.CURRENT_TEAM, Guid.Empty.ToString()),
            new(IdentityClaimConsts.STAFF, Guid.Empty.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.TokenExpireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
