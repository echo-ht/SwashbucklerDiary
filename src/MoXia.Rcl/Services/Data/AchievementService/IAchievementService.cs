using MoXia.Shared;

namespace MoXia.Rcl.Services
{
    public interface IAchievementService
    {
        event Action<UserStateModel>? UserStateChanged;

        Task<List<string>> UpdateUserState(Achievement achievement);

        Task<List<string>> UpdateUserState(Achievement achievement, int count);

        Task<List<AchievementModel>> GetAchievements();
    }
}
