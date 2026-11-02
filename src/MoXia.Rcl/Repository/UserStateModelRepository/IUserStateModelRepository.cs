using MoXia.Shared;

namespace MoXia.Rcl.Repository
{
    public interface IUserStateModelRepository : IBaseRepository<UserStateModel>
    {
        Task<UserStateModel> InsertOrUpdateAsync(Achievement achievement);

        Task<UserStateModel> InsertOrUpdateAsync(Achievement achievement, int count);
    }
}
