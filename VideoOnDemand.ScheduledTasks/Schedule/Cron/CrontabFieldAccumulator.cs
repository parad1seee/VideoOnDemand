using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.ScheduledTasks.Schedule.Cron
{
    public delegate void CrontabFieldAccumulator(int start, int end, int interval);
}
