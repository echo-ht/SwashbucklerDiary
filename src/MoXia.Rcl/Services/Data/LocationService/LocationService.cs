using MoXia.Rcl.Repository;
using MoXia.Shared;

namespace MoXia.Rcl.Services
{
    public class LocationService : BaseDataService<LocationModel>, ILocationService
    {
        private readonly ILocationRepository _iLocationRepository;

        public LocationService(ILocationRepository iLocationRepository)
        {
            base._iBaseRepository = iLocationRepository;
            _iLocationRepository = iLocationRepository;
        }
    }
}
