// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   The worker role.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Silo
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Common;

    using EventJournal.Journal;
    using EventJournal.Journal.AzureTable;

    using Microsoft.WindowsAzure.ServiceRuntime;

    using Newtonsoft.Json;

    using Orleans.Runtime.Host;

    /// <summary>
    /// The worker role.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The run complete event.
        /// </summary>
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        /// <summary>
        /// The silo.
        /// </summary>
        private AzureSilo silo;

        /// <summary>
        /// The run.
        /// </summary>
        public override void Run()
        {
            Trace.TraceInformation("Silo is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        /// <summary>
        /// The on start.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool OnStart()
        {
            // Note that you need to do this :)
            // Note that you need to do this :)
            // Note that you need to do this :)
            // Also check out AzureTableJournalProvider, for actual use.
            JournalProviderManager.Manager.GetProviderDelegate =
                (_, __) => new AzureTableJournalProvider("UseDevelopmentStorage=true", JsonSerializationSettings.JsonConfig);

            this.silo = new AzureSilo();

            var ok = base.OnStart();
            if (ok)
            {
                ok = this.silo.Start();
            }

            return ok;
        }

        /// <summary>
        /// The on stop.
        /// </summary>
        public override void OnStop()
        {
            Trace.TraceInformation("Silo is stopping");

            this.silo.Stop();
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Silo has stopped");
        }

        /// <summary>
        /// The run async.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
