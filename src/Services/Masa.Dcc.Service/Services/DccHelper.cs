// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Dcc.Service.Admin.Services;

public static class DccHelper
{
    /// <summary>
    /// 独立部署模式下不再依赖 MASA Auth 服务回填用户名，
    /// Standalone 只有内置管理员一个账号，发布人直接取当前请求用户（缺省 system）。
    /// </summary>
    public static Task<List<LatestReleaseConfigModel>> FillUserNameAsync(
        this List<LatestReleaseConfigModel> data, string userName)
    {
        foreach (var t in data)
        {
            t.LastPublisher = userName;
        }

        return Task.FromResult(data);
    }

    public static Task<List<ConfigObjectDto>> FillUserNameAsync(
        this List<ConfigObjectDto> data, string userName)
    {
        foreach (var t in data)
        {
            if (t.LatestRelease != null)
            {
                t.LatestRelease.LastPublisher = userName;
            }
        }

        return Task.FromResult(data);
    }
}
