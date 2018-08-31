using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Lykke.Sdk;
using Lykke.Service.RedisMonitoring.Core.Services;

namespace Lykke.Service.RedisMonitoring.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly List<IStartable> _startables = new List<IStartable>();

        public StartupManager(IEnumerable<IStartStop> startables)
        {
            _startables.AddRange(startables);
        }

        public Task StartAsync()
        {
            Parallel.ForEach(_startables, i => i.Start());

            return Task.CompletedTask;
        }
    }
}
