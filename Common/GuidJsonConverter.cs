// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   JSON converter for <see cref="Guid" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Common
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    ///     JSON converter for <see cref="Guid"/>.
    /// </summary>
    public class GuidJsonConverter : JsonConverter
    {
        /// <summary>
        /// Initializes static members of the <see cref="GuidJsonConverter"/> class.
        /// </summary>
        static GuidJsonConverter()
        {
            Instance = new GuidJsonConverter();
        }

        /// <summary>
        ///     Gets the instance.
        /// </summary>
        public static GuidJsonConverter Instance { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter"/> can read JSON.
        /// </summary>
        /// <value>
        ///     <c>
        ///         true
        ///     </c>
        ///     if this <see cref="T:Newtonsoft.Json.JsonConverter"/> can read JSON; otherwise,
        ///     <c>
        ///         false
        ///     </c>
        ///     .
        /// </value>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        ///     <c>
        ///         true
        ///     </c>
        ///     if this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON; otherwise,
        ///     <c>
        ///         false
        ///     </c>
        ///     .
        /// </value>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">
        /// Type of the object.
        /// </param>
        /// <returns>
        /// <c>
        ///         true
        ///     </c>
        ///     if this instance can convert the specified object type; otherwise,
        ///     <c>
        ///         false
        ///     </c>
        ///     .
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Guid) || objectType == typeof(Guid?);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="serializer">
        /// The calling serializer.
        /// </param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteValue(default(string));
            }
            else if (value is Guid)
            {
                var guid = (Guid)value;
                writer.WriteValue(guid.ToString("N"));
            }
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.
        /// </param>
        /// <param name="objectType">
        /// Type of the object.
        /// </param>
        /// <param name="existingValue">
        /// The existing value of object being read.
        /// </param>
        /// <param name="serializer">
        /// The calling serializer.
        /// </param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var str = reader.Value as string;
            return str != null ? Guid.Parse(str) : default(Guid);
        }
    }
}