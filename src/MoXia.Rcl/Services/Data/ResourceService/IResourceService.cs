using MoXia.Shared;
using System.Linq.Expressions;

namespace MoXia.Rcl.Services
{
    public interface IResourceService : IBaseDataService<ResourceModel>
    {
        Task DeleteUnusedResourcesAsync(Expression<Func<ResourceModel, bool>> func);

        Task DeleteAllUnusedResourcesWithFilesAsync();

        Task<ResourceModel> FindIncludesAsync(string id);

        Task<List<Guid>> GetDiaryIdsAsync(string id);
    }
}
