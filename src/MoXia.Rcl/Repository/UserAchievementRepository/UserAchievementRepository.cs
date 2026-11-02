using SqlSugar;
using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Rcl.Repository
{
    public class UserAchievementRepository : BaseRepository<UserAchievementModel>, IUserAchievementRepository
    {
        public UserAchievementRepository(ISqlSugarClient context,
            ISettingService settingService,
            IDatabaseScopeAccessor databaseScope) : base(context, settingService, databaseScope)
        {
        }
    }
}
