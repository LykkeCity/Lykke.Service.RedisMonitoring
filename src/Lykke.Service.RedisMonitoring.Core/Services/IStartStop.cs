using Autofac;
using Common;

namespace Lykke.Service.RedisMonitoring.Core.Services
{
    public interface IStartStop : IStartable, IStopable
    {
    }
}
