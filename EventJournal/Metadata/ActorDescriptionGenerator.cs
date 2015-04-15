// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="ActorDescriptionGenerator.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Metadata
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using ActorMetadataAttributes;

    using EventJournal.Utilities;

    using Orleans;

    /// <summary>
    /// The actor description generator.
    /// </summary>
    public static class ActorDescriptionGenerator
    {
        /// <summary>
        /// The cache of actor descriptions.
        /// </summary>
        private static readonly ConcurrentDictionary<Assembly, Dictionary<string, ActorDescription>> ActorDescriptions =
            new ConcurrentDictionary<Assembly, Dictionary<string, ActorDescription>>();

        /// <summary>
        /// Returns all actor descriptions for the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly.
        /// </param>
        /// <returns>
        /// All actor descriptions for the specified assembly.
        /// </returns>
        public static Dictionary<string, ActorDescription> GetActorDescriptions(Assembly assembly)
        {
            return ActorDescriptions.GetOrAdd(
                assembly, 
                asm =>
                asm.GetTypes()
                    .Where(ShouldGenerateActorDescription)
                    .Select(GetActorDescription)
                    .ToDictionary(_ => _.Kind, _ => _));
        }

        /// <summary>
        /// Returns all actor descriptions for the specified assemblies.
        /// </summary>
        /// <param name="assemblies">
        /// The actor assemblies.
        /// </param>
        /// <returns>
        /// All actor descriptions for the specified assembly.
        /// </returns>
        public static Dictionary<string, ActorDescription> GetActorDescriptions(IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(GetActorDescriptions).ToDictionary(_ => _.Key, _ => _.Value);
        }

        /// <summary>
        /// Returns a value indicating whether or not an actor description should be generated for the provided type.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// A value indicating whether or not an actor description should be generated for the provided type.
        /// </returns>
        private static bool ShouldGenerateActorDescription(Type type)
        {
            // If the type is concrete, a description should not be generated.
            if (!type.IsInterface)
            {
                return false;
            }

            // If the interface has an Actor attribute, a description should be generated only if it is not marked as
            // abstract.
            var attr = type.GetCustomAttribute<ActorAttribute>();
            if (attr != null)
            {
                return !attr.IsAbstract;
            }

            // By default, all actor interfaces should have a description generated.
            return typeof(IGrainWithGuidKey).IsAssignableFrom(type) && typeof(IGrainWithGuidKey) != type
                   && type.IsPublic;
        }

        /// <summary>
        /// Returns the description of the provided actor type.
        /// </summary>
        /// <param name="type">
        /// The actor type.
        /// </param>
        /// <returns>
        /// The description of the provided actor type.
        /// </returns>
        private static ActorDescription GetActorDescription(Type type)
        {
            var actorAttr = type.GetCustomAttribute<ActorAttribute>() ?? new ActorAttribute(type);
            var result = new ActorDescription
                         {
                             Kind = actorAttr.Kind, 
                             IsSingleton = actorAttr.IsSingleton, 
                             Methods = new Dictionary<string, ActorMethodDescription>(), 
                             Type = type
                         };

            // Get all server-visible methods from the type.
            var methods = type.GetInterfaces().SelectMany(iface => iface.GetMethods()).Concat(type.GetMethods());
            foreach (var method in methods)
            {
                var methodDescription = GetMethodDescription(method);
                var ev = method.GetCustomAttribute<EventAttribute>();
                var name = ev != null ? ev.Type : ProxyGenerationUtility.ToCanonicalName(method.Name);

                result.Methods[name] = methodDescription;
            }

            return result;
        }

        /// <summary>
        /// Returns the description of the provided actor method.
        /// </summary>
        /// <param name="method">
        /// The actor method.
        /// </param>
        /// <returns>
        /// The description of the provided actor method.
        /// </returns>
        private static ActorMethodDescription GetMethodDescription(MethodInfo method)
        {
            var methodParameters = method.GetParameters().ToList();
            string returnTypeName;
            if (method.ReturnType == typeof(Task))
            {
                returnTypeName = string.Empty;
            }
            else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                returnTypeName = GetDisplayTypeName(method.ReturnType.GenericTypeArguments[0]);
            }
            else
            {
                returnTypeName = GetDisplayTypeName(method.ReturnType);
            }

            return new ActorMethodDescription
            {
                ReturnType = returnTypeName, 
                Name = GetEventName(method), 
                Args = new List<ActorMethodArgumentDescription>(methodParameters.Select(GetParameterDescription)), 
                MethodInfo = method, 
                Visible = ProxyGenerationUtility.IsVisible(method.DeclaringType, method)
            };
        }

        /// <summary>
        /// Returns a string representing the provided <paramref name="type"/>, suitable for display.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// A string representing the provided <paramref name="type"/>, suitable for display.
        /// </returns>
        private static string GetDisplayTypeName(Type type)
        {
            string name;
            if (type.IsGenericType)
            {
                var typeName = type.Name.Substring(0, type.Name.IndexOf('`'));
                name = string.Format("{0}<{1}>", typeName, string.Join(", ", type.GenericTypeArguments.Select(GetDisplayTypeName)));
            }
            else
            {
                name = type.Name;
            }

            return ProxyGenerationUtility.ToCanonicalName(name);
        }

        /// <summary>
        /// Returns the description of the provided parameter.
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <returns>
        /// The description of the provided parameter.
        /// </returns>
        private static ActorMethodArgumentDescription GetParameterDescription(ParameterInfo parameter)
        {
            return new ActorMethodArgumentDescription
            {
                Name = ProxyGenerationUtility.ToCanonicalName(parameter.Name), 
                Type = GetDisplayTypeName(parameter.ParameterType)
            };
        }

        /// <summary>
        /// Returns the event name for the provided <paramref name="method"/>.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The event name for the provided <paramref name="method"/>.
        /// </returns>
        private static string GetEventName(MemberInfo method)
        {
            var ev = method.GetCustomAttribute<EventAttribute>();
            return ev != null ? ev.Type : ProxyGenerationUtility.ToCanonicalName(method.Name);
        }
    }
}