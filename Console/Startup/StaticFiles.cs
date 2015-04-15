// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StaticFiles.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Console.Startup
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;

    using Owin;

    /// <summary>
    /// Initialization methods.
    /// </summary>
    internal partial class Initialization
    {
        /// <summary>
        /// The assembly name.
        /// </summary>
        private static readonly string AssemblyName = Assembly.GetCallingAssembly().GetName().Name;

        /// <summary>
        /// Configures the static site.
        /// </summary>
        /// <param name="app">
        /// The app builder.
        /// </param>
        public static void ConfigureConsoleSite(IAppBuilder app)
        {
            var webServerRoot = GetAssemblyPath();
            Trace.TraceInformation("Configuring {0} at physical path \"{1}\".", AssemblyName, webServerRoot);
            try
            {
                var mappings = new[]
                                   {
                                       new FileServerOptions
                                           {
                                               RequestPath = new PathString(), 
                                               FileSystem = new PhysicalFileSystem(Path.Combine(webServerRoot, "console"))
                                           }, 
                                       new FileServerOptions
                                           {
                                               RequestPath = new PathString("/js"), 
                                               FileSystem = new PhysicalFileSystem(Path.Combine(webServerRoot, "Scripts"))
                                           }, 
                                       new FileServerOptions
                                           {
                                               RequestPath = new PathString("/css"), 
                                               FileSystem = new PhysicalFileSystem(Path.Combine(webServerRoot, "Content"))
                                           }, 
                                       new FileServerOptions
                                           {
                                               RequestPath = new PathString("/img"), 
                                               FileSystem = new PhysicalFileSystem(Path.Combine(webServerRoot, "img"))
                                           }
                                   };
                foreach (var mapping in mappings)
                {
                    app.UseFileServer(mapping);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("Exception configuring {0}: {1}.", AssemblyName, exception);
                throw;
            }

            Trace.TraceInformation("Done configuring {0} at path \"{1}\".", AssemblyName, webServerRoot);
        }

        /// <summary>
        /// Returns the path of the executing assembly.
        /// </summary>
        /// <returns>The path of the executing assembly.</returns>
        private static string GetAssemblyPath()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }
    }
}
