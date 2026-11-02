using SqlSugar;
using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Rcl.Repository
{
    public class LogRepository : BaseRepository<LogModel>, ILogRepository
    {
        public LogRepository(ISqlSugarClient context,
            ISettingService settingService,
            IDatabaseScopeAccessor databaseScope) : base(context, settingService, databaseScope)
        {
        }

        public override ISqlSugarClient Context
        {
            get => Itenant.GetConnection(SQLiteConstants.LogDatabaseFilename);
            set => base.Context = value;
        }
    }
}
