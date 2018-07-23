using System;
using Microsoft.Azure.WebJobs.Extensions.Timers;

namespace AzureDocsUpdatesFnApp.Tests.Stubs
{
    public class TimerScheduleStub : TimerSchedule
    {
        public override DateTime GetNextOccurrence(DateTime now)
        {
            return now.AddDays(1);
        }
    }

}
