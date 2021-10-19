using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.Redis
{
    public class RedisConfig
    {
        public string SubscribeChannel { get; set; }

        public string SendChannel { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string KeyPrefix { get; set; }
    }
}
