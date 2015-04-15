// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddressJsonConverter.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Utilities
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// The address serializer.
    /// </summary>
    public class AddressJsonConverter : JsonConverter
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static readonly AddressJsonConverter Converter = new AddressJsonConverter();

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static AddressJsonConverter Instance
        {
            get
            {
                return Converter;
            }
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
            var addr = (Address)value;
            var addrString = addr == null ? string.Empty : addr.ToString();
            writer.WriteValue(addrString);
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
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return Address.FromString(reader.Value as string);
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">
        /// Type of the object.
        /// </param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Address) == objectType;
        }
    }
}