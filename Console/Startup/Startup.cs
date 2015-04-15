// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Startup.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// <summary>
//   Methods for service initialization.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Console.Startup
{
    using System.Web.Http;

    using Console.Controllers;

    using Owin;

    /// <summary>
    /// Methods for service initialization.
    /// </summary>
    internal partial class Initialization
    {
        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="app">
        /// The app builder.
        /// </param>
        public static void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            
            ConfigureWebApi(app, config);
            
            ConfigureConsoleSite(app);
             
            // Warm up controllers.
            InvocationController.Initialize();
        }
    }
}
