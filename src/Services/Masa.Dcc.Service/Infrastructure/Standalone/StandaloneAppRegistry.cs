// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

using System.Collections.Concurrent;

namespace Masa.Dcc.Service.Admin.Infrastructure.Standalone;

/// <summary>
/// 独立部署模式下的本地应用注册表。
/// 不依赖 PM 服务，通过 open-api 初始化应用配置时自动登记应用 Identity，
/// 并持久化到 App_Data/standalone-apps.json，保证重启后 AppId 与 Identity 的映射不丢失。
/// </summary>
public class StandaloneAppRegistry
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, AppRecord> _appsByIdentity;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public StandaloneAppRegistry(IHostEnvironment environment)
    {
        var dataDir = Path.Combine(environment.ContentRootPath, "App_Data");
        _filePath = Path.Combine(dataDir, "standalone-apps.json");
        _appsByIdentity = Load(dataDir);
    }

    /// <summary>按 Identity 获取应用，不存在则用确定性 Id 登记并持久化</summary>
    public async Task<AppRecord> GetOrCreateAsync(string identity)
    {
        if (_appsByIdentity.TryGetValue(identity, out var record))
            return record;

        await _lock.WaitAsync();
        try
        {
            if (_appsByIdentity.TryGetValue(identity, out record))
                return record;

            record = new AppRecord(GetStableId(identity), identity, identity);
            _appsByIdentity[identity] = record;
            await SaveAsync();
            return record;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<AppRecord?> FindByIdentityAsync(string identity)
    {
        _appsByIdentity.TryGetValue(identity, out var record);
        return Task.FromResult<AppRecord?>(record);
    }

    public Task<List<AppRecord>> GetAllAsync()
        => Task.FromResult(_appsByIdentity.Values.ToList());

    public Task<AppRecord?> FindByIdAsync(int id)
        => Task.FromResult(_appsByIdentity.Values.FirstOrDefault(a => a.Id == id));

    /// <summary>identity 的稳定正整数 Id（同一 Identity 跨重启保持一致）</summary>
    private static int GetStableId(string identity)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToLowerInvariant()));
        var id = BitConverter.ToInt32(hash, 0) & 0x7FFFFFFF;
        return id == 0 ? 1 : id;
    }

    private ConcurrentDictionary<string, AppRecord> Load(string dataDir)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var records = JsonSerializer.Deserialize<List<AppRecord>>(File.ReadAllText(_filePath)) ?? new();
                return new ConcurrentDictionary<string, AppRecord>(
                    records.Select(r => new KeyValuePair<string, AppRecord>(r.Identity, r)),
                    StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // 注册表文件损坏时重建，不影响服务启动
        }
        return new ConcurrentDictionary<string, AppRecord>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(_appsByIdentity.Values.ToList(), _serializerOptions));
    }

    public record AppRecord(int Id, string Identity, string Name);
}
