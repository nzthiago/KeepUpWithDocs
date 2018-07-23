using AzureDocsUpdates.Shared.Model;
using AzureDocsUpdatesFnApp.Model;
using AzureDocsUpdatesFnApp.Tests.Stubs;
using Lohmann.DotEnv;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AzureDocsUpdatesFnApp.Tests
{
    public class GitDataImportFnTest
    {
        public GitDataImportFnTest()
        {
        }

        [Fact]
        public async Task Verify()
        {
            await EnvFile.Default.LoadAsync(throwWhenFileNotExists: true);
            
            var changesPerDay = new AsyncCollectorStub<DayInfo>();
            var titleWorkerStorage = new AsyncCollectorStub<ProcessDayInfoForTitlesJobWorkerData>();
            var titleQueue = new AsyncCollectorStub<ProcessDayInfoForTitlesJob>();

            var timerScheduleStub = new TimerScheduleStub();
            var loggerMock = new LoggerMock();
            var dueTimer = new TimerInfo(timerScheduleStub, new ScheduleStatus(), true);

            await GitDataImportFn.RunAsync(
                dueTimer, 
                changesPerDay, 
                titleWorkerStorage,
                titleQueue,
                new TraceWriterStub());

            Assert.NotEmpty(changesPerDay.Items);
        }
    }
}
