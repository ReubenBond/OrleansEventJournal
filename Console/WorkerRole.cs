// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   The worker role entry point.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Console
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Reflection;
    using System.Threading;

    using Console.Startup;

    using Microsoft.Owin.Hosting;
    using Microsoft.WindowsAzure.ServiceRuntime;

    using Orleans.Runtime.Host;

    /// <summary>
    /// The worker role entry point.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        /// <summary>
        /// The assembly name.
        /// </summary>
        private static readonly string AssemblyName = Assembly.GetCallingAssembly().GetName().Name;

        /// <summary>
        /// The app.
        /// </summary>
        private IDisposable app;

        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private CancellationTokenSource cancellation;

        /// <summary>
        /// The run method.
        /// </summary>
        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("{0} entry point called", AssemblyName);

            while (!this.cancellation.IsCancellationRequested)
            {
                this.cancellation.Token.WaitHandle.WaitOne(TimeSpan.FromMinutes(5));
            }
        }

        /// <summary>
        /// The startup handler.
        /// </summary>
        /// <returns>
        /// A value indicating success or failure.
        /// </returns>
        public override bool OnStart()
        {
            try
            {
                this.cancellation = new CancellationTokenSource();
                Trace.TraceInformation("{0} starting.", AssemblyName);

                // Set the maximum number of concurrent connections 
                ServicePointManager.DefaultConnectionLimit = 12 * Environment.ProcessorCount;

                var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HTTP"];
                var uri = string.Format("{0}://{1}", endpoint.Protocol, endpoint.IPEndpoint);

                Trace.TraceInformation(string.Format("Starting Web service at {0}", uri), "Information");

                // Initialize Orleans.
                InitializeOrleansClient();

                this.cancellation = new CancellationTokenSource();

                Trace.TraceInformation("Starting {0} server at {1}.", AssemblyName, uri);
                this.app = WebApp.Start<Initialization>(new StartOptions(uri));

                Trace.TraceInformation("{0} started.", AssemblyName);
            }
            catch (Exception exception)
            {
                Trace.TraceError("Exception starting {0}: {1}", AssemblyName, exception);
            }

            return base.OnStart();
        }

        /// <summary>
        /// The shutdown handler.
        /// </summary>
        public override void OnStop()
        {
            Trace.TraceInformation("{0} stopping.", AssemblyName);
            this.cancellation.Cancel();
            if (this.app != null)
            {
                this.app.Dispose();
            }

            Trace.TraceInformation("{0} stopped.", AssemblyName);
            base.OnStop();
        }

        /// <summary>
        /// Initialize the Orleans client.
        /// </summary>
        private static void InitializeOrleansClient()
        {
            Trace.TraceInformation("Initializing Orleans client.");
            for (var i = 0; i < 20; i++)
            {
                try
                {
                    AzureClient.Initialize();
                    break;
                }
                catch (Exception exception)
                {
                    Trace.TraceWarning("Exception initializing Orleans client: {0}.", exception);

                    if (i + 1 < 20)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                    else
                    {
                        // Too many retries, roll the role.
                        throw;
                    }
                }
            }

            Trace.TraceInformation("Done initializing Orleans client.");
        }
    }
}
