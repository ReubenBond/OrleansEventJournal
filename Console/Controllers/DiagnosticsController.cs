// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="DiagnosticsController.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Console.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Http;

    using EventJournal.Execution;

    /// <summary>
    /// The diagnostics controller.
    /// </summary>
    [RoutePrefix("diagnostics")]
    public class DiagnosticsController : ApiController
    {
        /// <summary>
        /// The get dispatchers.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        [HttpGet]
        [Route("dispatchers")]
        public Task<string> GetDispatchers()
        {
            return InvocationController.DispatcherSource;
        }

        /// <summary>
        /// The get producer.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        [HttpGet]
        [Route("producer/{kind}")]
        public string GetProducer(string kind)
        {
            return EventProducerGenerator.GetSource(InvocationController.Actors.Value[kind]);
        }

        /// <summary>
        /// The get replay.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        [HttpGet]
        [Route("replay/{kind}")]
        public string GetReplay(string kind)
        {
            return EventInvokerGenerator.GetSource(InvocationController.Actors.Value[kind]);
        }
    }
}