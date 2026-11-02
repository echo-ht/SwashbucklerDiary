using SqlSugar;
using MoXia.Rcl.Services;
using MoXia.Shared;
using System.Linq.Expressions;

namespace MoXia.Rcl.Repository
{
    public class BaseRepository<T> : SimpleClient<T>, IBaseRepository<T> where T : class, new()
    {
        private readonly ISettingService _settingService;
        private readonly IDatabaseScopeAccessor _databaseScope;

        public BaseRepository(ISqlSugarClient context,
            ISettingService settingService,
            IDatabaseScopeAccessor databaseScope) : base(context)
        {
            _settingService = settingService;
            _databaseScope = databaseScope;
        }

        public override ISqlSugarClient Context
        {
            get => Itenant.GetConnection(ConfigId);
            set => base.Context = value;
        }

        private string ConfigId
        {
            get
            {
                LibraryKind library = _databaseScope.Current
                    ?? (_settingService.GetTemp(it => it.PrivacyMode) ? LibraryKind.Private : LibraryKind.Normal);
                return library == LibraryKind.Private ? SQLiteConstants.PrivacyDatabaseFilename : SQLiteConstants.MainDatabaseFilename;
            }
        }

        public ITenant Itenant => base.Context.CopyNew();

        public virtual Task<int> CountAsync()
        {
            return Context.Queryable<T>().CountAsync();
        }

        public Task<bool> DeleteAsync()
        {
            return Context.Deleteable<T>().ExecuteCommandHasChangeAsync();
        }

        public Task<bool> UpdateAsync(T entity, Expression<Func<T, object>> columns)
        {
            return Context.Updateable(entity).UpdateColumns(columns).ExecuteCommandHasChangeAsync();
        }

        public Task<bool> UpdateAsync(List<T> updateObjs, Expression<Func<T, object>> columns)
        {
            return Context.Updateable(updateObjs).UpdateColumns(columns).ExecuteCommandHasChangeAsync();
        }
    }
}
