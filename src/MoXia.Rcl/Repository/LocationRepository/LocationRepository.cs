using SqlSugar;
using MoXia.Rcl.Services;
using MoXia.Shared;
using System.Linq.Expressions;

namespace MoXia.Rcl.Repository
{
    public class LocationRepository : BaseRepository<LocationModel>, ILocationRepository
    {
        public LocationRepository(ISqlSugarClient context,
            ISettingService settingService,
            IDatabaseScopeAccessor databaseScope) : base(context, settingService, databaseScope)
        {
        }

        public override Task<List<LocationModel>> GetListAsync()
        {
            return Context.Queryable<LocationModel>()
                .OrderByDescending(it => it.CreateTime)
                .ToListAsync();
        }

        public override Task<List<LocationModel>> GetListAsync(Expression<Func<LocationModel, bool>> expression)
        {
            return Context.Queryable<LocationModel>()
                .Where(expression)
                .OrderByDescending(it => it.CreateTime)
                .ToListAsync();
        }
    }
}
