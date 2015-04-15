// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonSerializationSettings.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Common
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Global constants.
    /// </summary>
    public static class JsonSerializationSettings
    {
        /// <summary>
        ///     The JSON config instance.
        /// </summary>
        private static readonly JsonSerializerSettings JsonConfigInstance;

        /// <summary>
        ///     Initializes static members of the <see cref="JsonSerializationSettings"/> class.
        /// </summary>
        static JsonSerializationSettings()
        {
            JsonConfigInstance = new JsonSerializerSettings
            {
                ContractResolver = JsonContractResolver.Instance,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                CheckAdditionalContent = false
            };

            JsonSerializer = JsonSerializer.Create(JsonConfig);

            // Set the default JSON serializer.
            JsonConvert.DefaultSettings = () => JsonConfig;
        }

        /// <summary>
        ///     Gets the JSON serializer settings.
        /// </summary>
        public static JsonSerializerSettings JsonConfig
        {
            get
            {
                return JsonConfigInstance;
            }
        }

        /// <summary>
        ///     Gets the JSON serializer.
        /// </summary>
        public static JsonSerializer JsonSerializer { get; private set; }
    }
}