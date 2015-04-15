// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompletionController.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Console.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;

    using EventJournal.Metadata;

    using Newtonsoft.Json;

    /// <summary>
    /// The command completion controller.
    /// </summary>
    [RoutePrefix("complete")]
    public class CompletionController : ApiController
    {
        /// <summary>
        /// Returns a stream of tab-completion suggestions for each of the provided <see cref="PartialCommand"/>s.
        /// </summary>
        /// <param name="command">
        /// The stream of partially typed commands.
        /// </param>
        /// <returns>
        /// A stream of tab-completion suggestions for each of the provided <see cref="PartialCommand"/>s.
        /// </returns>
        [Route("command")]
        [HttpPost]
        public IEnumerable<string> CompleteCommand([FromBody]PartialCommand command)
        {
            if (command.Args != null && command.Args.Count != 0)
            {
                return Enumerable.Empty<string>();
            }

            // Method completion.
            ActorDescription actor;
            if (!InvocationController.Actors.Value.TryGetValue(command.Kind, out actor))
            {
                return Enumerable.Empty<string>();
            }

            var results = new List<string>();
            var method = command.Method ?? string.Empty;
            results.AddRange(actor.Methods.Where(_ => _.Value.Visible && _.Key.StartsWith(method, StringComparison.OrdinalIgnoreCase)).Select(_ => _.Key));

            return results;
        }

        /// <summary>
        /// Returns a stream of tab-completion suggestions for each of the provided actor kinds.
        /// </summary>
        /// <param name="kind">
        /// The stream of partially typed actor kinds.
        /// </param>
        /// <returns>
        /// A stream of tab-completion suggestions for each of the provided actor kinds.
        /// </returns>
        [Route("kind/{kind?}")]
        [HttpGet]
        public List<string> CompleteKind([FromUri] string kind = null)
        {
            var result = new List<string>();
            kind = kind ?? string.Empty;
            var actors = InvocationController.Actors;
            if (kind.EndsWith("/"))
            {
                ActorDescription actor;
                if (actors.Value.TryGetValue(kind.Substring(0, kind.IndexOf('/')), out actor))
                {
                    result.Add(kind + Guid.Empty.ToString("N"));
                }
            }
            else
            {
                result.AddRange(actors.Value.Keys.Where(_ => _.StartsWith(kind, StringComparison.OrdinalIgnoreCase)).Select(_ => "to " + _ + "/"));
            }

            return result;
        }

        /// <summary>
        /// Returns the actor definitions.
        /// </summary>
        /// <returns>The actor definitions.</returns>
        [Route("actors")]
        [HttpGet]
        public Dictionary<string, ActorDescription> GetActors()
        {
            return InvocationController.Actors.Value;
        }

        /// <summary>
        /// Describes a partially completed console command.
        /// </summary>
        public class PartialCommand
        {
            /// <summary>
            /// Gets or sets the kind.
            /// </summary>
            public string Kind { get; set; }

            /// <summary>
            /// Gets or sets the command.
            /// </summary>
            [JsonProperty("cmd")]
            public string Method { get; set; }

            /// <summary>
            /// Gets or sets the args.
            /// </summary>
            public List<string> Args { get; set; }
        }
    }
}