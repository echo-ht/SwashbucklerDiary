using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Services
{
    public class LANDeviceInfo
    {
        public string? DeviceName { get; set; }

        public string? IPAddress { get; set; }

        public AppOperatingSystem DevicePlatform { get; set; }

        public DateTime LastSeen { get; set; }
    }
}
