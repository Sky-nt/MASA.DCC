// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Dcc.Contracts.Admin.PublicConfig;

public record OssSecurityTokenDto(
    string RegionId,
    string AccessKeyId,
    string AccessKeySecret,
    string SessionToken,
    string Bucket,
    string CdnEndpoint);
