using VideoOnDemand.Redis.Models.Abstract;
using System;

namespace VideoOnDemand.Redis.Models
{
    public class RedisTestModel : RedisRecord
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public DateTime Date { get; set; }
    }
}
