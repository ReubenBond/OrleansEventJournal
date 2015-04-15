// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebApi.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Console.Startup
{
    using System.Linq;
    using System.Web.Http;

    using Console.Filters;

    using Owin;

    /// <summary>
    /// Methods for configuring ASP.NET Web API.
    /// </summary>
    internal partial class Initialization
    {
        /// <summary>
        /// Configures ASP.NET Web API
        /// </summary>
        /// <param name="app">
        /// The app builder.
        /// </param>
        /// <param name="config">
        /// The HTTP config.
        /// </param>
        public static void ConfigureWebApi(IAppBuilder app, HttpConfiguration config)
        {
            // Configure routing
            config.MapHttpAttributeRoutes();
            config.Formatters.JsonFormatter.SerializerSettings = Common.JsonSerializationSettings.JsonConfig;

            // Disable the XML formatter.
            var appXmlType =
                config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            if (appXmlType != null)
            {
                config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);
            }

            config.Filters.Add(new FlattenExceptionsFilterAttribute());
            
            config.EnsureInitialized();
            app.UseWebApi(config);
        }
    }
}
