using MoXia.Rcl.Repository;
using MoXia.Shared;

namespace MoXia.Rcl.Services
{
    public class TagService : BaseDataService<TagModel>, ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            base._iBaseRepository = tagRepository;
            _tagRepository = tagRepository;
        }

        public Task<TagModel> FindIncludesAsync(Guid id)
        {
            return _tagRepository.GetByIdIncludesAsync(id);
        }

        public Task<Dictionary<Guid, int>> TagsDiaryCount()
        {
            return _tagRepository.TagsDiaryCount();
        }
    }
}
