// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   The JSON contract resolver.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Common
{
    using System;
    using System.Reflection;

    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    ///     The JSON contract resolver.
    /// </summary>
    public class JsonContractResolver : CamelCasePropertyNamesContractResolver
    {
        /// <summary>
        ///     The instance of this class.
        /// </summary>
        public static readonly JsonContractResolver Instance = new JsonContractResolver();

        /// <summary>
        ///     The string-enumeration converter.
        /// </summary>
        private static readonly StringEnumConverter StringEnumConverter = new StringEnumConverter
        {
            CamelCaseText = true
        };

        /// <summary>
        /// Determines which contract type is created for the given type.
        /// </summary>
        /// <param name="objectType">
        /// Type of the object.
        /// </param>
        /// <returns>
        /// A <see cref="T:Newtonsoft.Json.Serialization.JsonContract"/> for the given type.
        /// </returns>
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);
            if (objectType == typeof(Guid) || objectType == typeof(Guid?))
            {
                contract.Converter = GuidJsonConverter.Instance;
            }
            else if (IsEnum(objectType))
            {
                contract.Converter = StringEnumConverter;
            }

            return contract;
        }

        /// <summary>
        /// Returns a value indicating whether or not <paramref name="type"/> is an enumeration type.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// A value indicating whether or not <paramref name="type"/> is an enumeration type.
        /// </returns>
        private static bool IsEnum(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsEnum
                   || (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                       && type.GenericTypeArguments[0].GetTypeInfo().IsEnum);
        }
    }
}