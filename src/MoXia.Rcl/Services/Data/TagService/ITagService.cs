using MoXia.Shared;

namespace MoXia.Rcl.Services
{
    public interface ITagService : IBaseDataService<TagModel>
    {
        Task<TagModel> FindIncludesAsync(Guid id);

        Task<Dictionary<Guid, int>> TagsDiaryCount();
    }
}
