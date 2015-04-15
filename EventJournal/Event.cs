// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="Event.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// Gets or sets the event.
    /// </summary>
    [Serializable]
    [DataContract]
    public class Event
    {
        /// <summary>
        /// Gets or sets the recipient.
        /// </summary>
        [DataMember]
        public Address To { get; set; }

        /// <summary>
        /// Gets or sets the identifier for this event.
        /// </summary>
        [DataMember]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        [DataMember]
        [JsonProperty("args")]
        public object[] Arguments { get; set; }

        /// <summary>
        /// Gets or sets the time at which the event occurred.
        /// </summary>
        [DataMember]
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets extra info.
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Extras { get; set; }

        /// <summary>
        /// Gets or sets the correlation id.
        /// </summary>
        [DataMember]
        [JsonProperty("cid")]
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the user id of the calling user.
        /// </summary>
        [DataMember]
        [JsonProperty("uid")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the additional data.
        /// </summary>
        /// <remarks>This is used for backwards and forwards compatibility.</remarks>
        [DataMember]
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; }

        /// <summary>
        /// Converts the specified argument to the provided type, <typeparamref name="T"/>.
        /// </summary>
        /// <param name="arg">
        /// The argument index.
        /// </param>
        /// <typeparam name="T">
        /// The return type.
        /// </typeparam>
        /// <returns>
        /// The specified argument converted to type <typeparamref name="T"/>.
        /// </returns>
        public T Arg<T>(int arg = 0)
        {
            if (arg > this.Arguments.Length)
            {
                throw new IndexOutOfRangeException("Tried to get argument " + arg + ", but only have " + this.Arguments.Length + " args.");
            }

            var value = this.Arguments[arg];
            if (value is T)
            {
                return (T)value;
            }

            if (value is JToken)
            {
                return (value as JToken).ToObject<T>();
            }

            return JToken.FromObject(value).ToObject<T>();
        }

        /// <summary>
        /// Converts the specified argument to the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type to convert the argument to.
        /// </param>
        /// <param name="arg">
        /// The argument index.
        /// </param>
        /// <returns>
        /// The specified argument converted to <paramref name="type"/>.
        /// </returns>
        public object Arg(Type type, int arg = 0)
        {
            if (arg > this.Arguments.Length)
            {
                throw new IndexOutOfRangeException("Tried to get argument " + arg + ", but only have " + this.Arguments.Length + " args.");
            }

            var value = this.Arguments[arg];
            if (value == null)
            {
                return null;
            }

            if (value.GetType() == type)
            {
                return value;
            }

            if (value is JToken)
            {
                return (value as JToken).ToObject(type);
            }

            return JToken.FromObject(value).ToObject(type);
        }

        /// <summary>
        /// Returns <see cref="Arguments"/> each cast <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to cast <see cref="Arguments"/> to.</typeparam>
        /// <returns><see cref="Arguments"/> each cast <typeparamref name="T"/>.</returns>
        public IEnumerable<T> ArgsAs<T>()
        {
            if (this.Arguments == null)
            {
                yield break;
            }

            for (int i = 0; i < this.Arguments.Length; ++i)
            {
                yield return this.Arg<T>(i);
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var result = new List<string> { string.Format("Id: {0}", this.Id) };
            
            if (this.To != null)
            {
                result.Add(string.Format("To: {0}", this.To));
            }

            if (!string.IsNullOrWhiteSpace(this.Type))
            {
                result.Add(string.Format("Type: {0}", this.Type));
            }

            if (this.Arguments != null && this.Arguments.Length > 0)
            {
                result.Add(string.Format("Args: {0}", string.Join(", ", this.Arguments.Select(_ => _ == null ? "null" : _.ToString()))));
            }

            return string.Join(", ", result);
        }
    }
}