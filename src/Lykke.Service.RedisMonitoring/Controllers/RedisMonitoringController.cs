using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.RedisMonitoring.Client;
using Lykke.Service.RedisMonitoring.Client.Models;
using Lykke.Service.RedisMonitoring.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.RedisMonitoring.Controllers
{
    public class RedisMonitoringController : Controller, IRedisMonitoringApi
    {
        private readonly ICachedRedisHealthRepository _redisHealthRepository;

        public RedisMonitoringController(ICachedRedisHealthRepository redisHealthRepository)
        {
            _redisHealthRepository = redisHealthRepository;
        }

        [Route("api/redismonitoring/Health")]
        [HttpGet]
        [SwaggerOperation("Health")]
        public async Task<List<RedisHealth>> GetHealth()
        {
            return await _redisHealthRepository.GetAll();
        }
    }
}
