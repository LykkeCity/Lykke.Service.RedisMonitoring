# Lykke.Service.RedisMonitoring

# Purpose

  - Monitors redis instances health.

# Contracts

Output (HTTP):
  - get monitored redis instances health ping history;

# Scaling
Current implementation is stateless service.
Resources: 

| Image | Resources | Default instances number | Max instances |
| ------ | ------ | ------ | ------ |
| Lykke.Service.Balances | C0-R0 | 1 | 10 |

# Dependencies
  - Azure Table Storage (logs and data);
  - Redis (cache);
