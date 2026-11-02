using MoXia.Shared;
using System.Linq.Expressions;

namespace MoXia.Rcl.Repository
{
    public interface ITagRepository : IBaseRepository<TagModel>
    {
        Task<TagModel> GetByIdIncludesAsync(dynamic id);

        Task<Dictionary<Guid, int>> TagsDiaryCount();
    }
}
