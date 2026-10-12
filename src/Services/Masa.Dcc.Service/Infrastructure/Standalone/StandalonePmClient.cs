// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Dcc.Service.Admin.Infrastructure.Standalone;

/// <summary>
/// 独立部署模式下的本地 IPmClient 实现，彻底替代对 PM 服务的依赖。
/// 环境与集群取自 MASA_ENVIRONMENT / MASA_CLUSTER（缺省 Production / Default），
/// 应用数据来自本地注册表（open-api 初始化时自动登记）。
/// </summary>
public class StandalonePmClient : IPmClient
{
    public const int EnvironmentId = 1;
    public const int ClusterId = 1;
    public const int EnvironmentClusterId = 1;
    public const int DefaultProjectId = 1;
    public const string DefaultProjectIdentity = "standalone";

    private readonly StandaloneAppRegistry _registry;
    private readonly IMasaStackConfig _masaStackConfig;

    public IEnvironmentService EnvironmentService { get; init; }
    public IClusterService ClusterService { get; init; }
    public IProjectService ProjectService { get; init; }
    public IAppService AppService { get; init; }

    public StandalonePmClient(StandaloneAppRegistry registry, IMasaStackConfig masaStackConfig)
    {
        _registry = registry;
        _masaStackConfig = masaStackConfig;
        EnvironmentService = new StandaloneEnvironmentService(this);
        ClusterService = new StandaloneClusterService(this);
        ProjectService = new StandaloneProjectService(this);
        AppService = new StandaloneAppService(this);
    }

    private string EnvironmentName => string.IsNullOrWhiteSpace(_masaStackConfig.Environment) ? "Production" : _masaStackConfig.Environment;
    private string ClusterName => string.IsNullOrWhiteSpace(_masaStackConfig.Cluster) ? "Default" : _masaStackConfig.Cluster;

    private EnvironmentClusterModel BuildEnvironmentCluster() => new()
    {
        Id = EnvironmentClusterId,
        EnvironmentName = EnvironmentName,
        ClusterName = ClusterName
    };

    private ProjectModel BuildProject() => new()
    {
        Id = DefaultProjectId,
        Identity = DefaultProjectIdentity,
        Name = "Standalone",
        LabelCode = "Other"
    };

    private async Task<ProjectAppsModel> BuildProjectAppsAsync()
    {
        var model = new ProjectAppsModel(DefaultProjectId, DefaultProjectIdentity, "Standalone", "Other", Guid.Empty);
        model.Apps = (await _registry.GetAllAsync())
            .Select(a => new AppModel { Id = a.Id, Identity = a.Identity, Name = a.Name, ProjectId = DefaultProjectId })
            .ToList();
        return model;
    }

    private class StandaloneEnvironmentService(StandalonePmClient owner) : IEnvironmentService
    {
        public Task<List<EnvironmentModel>> GetListAsync()
        {
            return Task.FromResult(new List<EnvironmentModel>
            {
                new() { Id = EnvironmentId, Name = owner.EnvironmentName, Color = "#3778F6" }
            });
        }

        public Task<EnvironmentDetailModel> GetAsync(int Id)
        {
            return Task.FromResult(new EnvironmentDetailModel
            {
                Id = Id,
                Name = owner.EnvironmentName,
                Description = "Standalone environment",
                Color = "#3778F6",
                ClusterIds = new List<int> { ClusterId }
            });
        }
    }

    private class StandaloneClusterService(StandalonePmClient owner) : IClusterService
    {
        public Task<List<ClusterModel>> GetListAsync()
        {
            return Task.FromResult(new List<ClusterModel>
            {
                new() { Id = ClusterId, Name = owner.ClusterName, EnvironmentClusterId = EnvironmentClusterId }
            });
        }

        public Task<List<ClusterModel>> GetListByEnvIdAsync(int envId) => GetListAsync();

        public Task<ClusterDetailModel> GetAsync(int Id)
        {
            return Task.FromResult(new ClusterDetailModel
            {
                Id = Id,
                Name = owner.ClusterName,
                Description = "Standalone cluster",
                EnvironmentIds = new List<int> { EnvironmentId }
            });
        }

        public Task<List<EnvironmentClusterModel>> GetEnvironmentClustersAsync()
        {
            return Task.FromResult(new List<EnvironmentClusterModel> { owner.BuildEnvironmentCluster() });
        }
    }

    private class StandaloneProjectService(StandalonePmClient owner) : IProjectService
    {
        public async Task<List<ProjectAppsModel>> GetProjectAppsAsync(string envName)
            => new() { await owner.BuildProjectAppsAsync() };

        public Task<List<ProjectModel>> GetListByEnvironmentClusterIdAsync(int envClusterId)
            => Task.FromResult(new List<ProjectModel> { owner.BuildProject() });

        public Task<List<ProjectModel>> GetListByTeamIdsAsync(List<Guid> teamIds, string environment)
            => Task.FromResult(new List<ProjectModel> { owner.BuildProject() });

        public Task<ProjectDetailModel> GetAsync(int id)
        {
            var project = owner.BuildProject();
            return Task.FromResult(new ProjectDetailModel
            {
                Id = project.Id,
                Identity = project.Identity,
                Name = project.Name,
                LabelCode = project.LabelCode,
                TeamId = project.TeamId
            });
        }

        public Task<ProjectDetailModel> GetByIdentityAsync(string identity)
            => GetAsync(DefaultProjectId);

        public Task<List<ProjectTypeModel>> GetProjectTypesAsync()
        {
            return Task.FromResult(new List<ProjectTypeModel>
            {
                new() { Id = 1, Name = "其他" }
            });
        }

        public Task<List<ProjectModel>> GetListAsync()
            => Task.FromResult(new List<ProjectModel> { owner.BuildProject() });
    }

    private class StandaloneAppService(StandalonePmClient owner) : IAppService
    {
        public async Task<List<AppDetailModel>> GetListAsync()
        {
            var records = await owner._registry.GetAllAsync();
            return records.Select(r => BuildAppDetail(r.Id, r.Identity, r.Name)).ToList();
        }

        public Task<List<AppDetailModel>> GetListByProjectIdsAsync(List<int> projectIds)
            => GetListAsync();

        public Task<AppDetailModel> GetWithEnvironmentClusterAsync(int Id)
            => GetAsync(Id);

        public async Task<AppDetailModel> GetAsync(int Id)
        {
            var record = await owner._registry.FindByIdAsync(Id);
            return record == null ? null! : BuildAppDetail(record.Id, record.Identity, record.Name);
        }

        public async Task<AppDetailModel> GetByIdentityAsync(string identity)
        {
            var record = await owner._registry.GetOrCreateAsync(identity);
            return BuildAppDetail(record.Id, record.Identity, record.Name);
        }

        public Task<List<AppDetailModel>> GetListByAppTypes(params AppTypes[] appTypes)
            => GetListAsync();

        private AppDetailModel BuildAppDetail(int id, string identity, string name) => new()
        {
            Id = id,
            Identity = identity,
            Name = name,
            ProjectId = DefaultProjectId,
            EnvironmentClusters = new List<EnvironmentClusterModel> { owner.BuildEnvironmentCluster() }
        };
    }
}
