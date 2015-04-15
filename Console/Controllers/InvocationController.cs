// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="InvocationController.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// <summary>
//   The command invocation controller.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Console.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Actor.Interfaces;

    using EventJournal;
    using EventJournal.Execution;
    using EventJournal.Metadata;

    using Newtonsoft.Json;

    using Orleans.Runtime;

    /// <summary>
    /// The command invocation controller.
    /// </summary>
    [RoutePrefix("invoke")]
    public class InvocationController : ApiController
    {
        /// <summary>
        /// The actors.
        /// </summary>
        public static readonly Lazy<Dictionary<string, ActorDescription>> Actors;

        /// <summary>
        /// The dispatcher.
        /// </summary>
        public static readonly Task<IEventDispatcher> Dispatcher;

        /// <summary>
        /// The dispatcher source code.
        /// </summary>
        public static readonly Task<string> DispatcherSource;

        /// <summary>
        /// The JSON serializer settings.
        /// </summary>
        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings();

        /// <summary>
        /// Initializes static members of the <see cref="InvocationController"/> class.
        /// </summary>
        static InvocationController()
        {
            var assemblies = new List<Assembly> { typeof(ICalculatorActor).Assembly, typeof(IManagementGrain).Assembly };

            Actors =
                new Lazy<Dictionary<string, ActorDescription>>(
                    () => ActorDescriptionGenerator.GetActorDescriptions(assemblies));
            var dispatcherCompletion = new TaskCompletionSource<IEventDispatcher>();
            var sourceCompletion = new TaskCompletionSource<string>();
            Dispatcher = dispatcherCompletion.Task;
            DispatcherSource = sourceCompletion.Task;

            // Generate the dispatchers asynchronously.
            Task.Run(
                () =>
                {
                    try
                    {
                        string source;
                        var dispatcher = EventDispatcherGenerator.GetDispatcher(Actors.Value.Values.ToList(), out source);
                        dispatcherCompletion.TrySetResult(dispatcher);
                        sourceCompletion.TrySetResult(source);
                    }
                    catch (Exception e)
                    {
                        dispatcherCompletion.TrySetException(e);
                        sourceCompletion.TrySetException(e);
                    }
                });
        }

        /// <summary>
        /// Initializes this class.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Invokes the command and returns the results.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        /// <returns>
        /// The result of invoking the command.
        /// </returns>
        [HttpPost]
        [Route]
        public Task<object> InvokeMethod([FromBody] Event command)
        {
            return this.DispatchEvent(Guid.Empty, command);
        }

        /// <summary>
        /// Dispatches the provided <paramref name="event"/>, returning the response.
        /// </summary>
        /// <param name="userId">
        /// The user id.
        /// </param>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        private async Task<object> DispatchEvent(Guid userId, Event @event)
        {
            try
            {
                Trace.CorrelationManager.StartLogicalOperation();
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                Orleans.Runtime.RequestContext.PropagateActivityId = true;
                
                ActorDescription actor;
                if (@event.To == null || @event.To.Kind == null || !Actors.Value.TryGetValue(@event.To.Kind, out actor))
                {
                    throw new ArgumentException("Actor kind not specified.");
                }

                ActorMethodDescription method;
                if (!actor.Methods.TryGetValue(@event.Type, out method))
                {
                    throw new ArgumentException(
                        string.Format("Method \"{0}\" not found on actor \"{1}\".", @event.Type, @event.To.Kind));
                }

                var inArgs = @event.Arguments == null ? 0 : @event.Arguments.Length;
                var reqArgs = method.Args == null ? 0 : method.Args.Count;
                if (inArgs != reqArgs)
                {
                    throw new ArgumentException(
                        string.Format(
                            "Incorrect number of arguments. Received {0} but expected {1}. Method: {2}", 
                            inArgs, 
                            reqArgs, 
                            method));
                }

                var resultTask = (await Dispatcher).Dispatch(@event);
                if (resultTask == null)
                {
                    throw new InvalidOperationException(
                        "The specified method does not exist. Input: "
                        + JsonConvert.SerializeObject(@event, this.jsonSettings));
                }

                return await resultTask;
            }
            finally
            {
                Trace.CorrelationManager.StopLogicalOperation();
            }
        }
    }
}