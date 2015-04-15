// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeUtil.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace EventJournal.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using EventJournal.Metadata;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Utilities for working with types.
    /// </summary>
    internal static class TypeUtil
    {
        /// <summary>
        /// Returns all namespaces required by <paramref name="actors"/>.
        /// </summary>
        /// <param name="actors">
        /// The actors.
        /// </param>
        /// <param name="additionalTypes">
        /// The additional types to include.
        /// </param>
        /// <returns>
        /// All namespaces required by <paramref name="actors"/>.
        /// </returns>
        public static IEnumerable<NameSyntax> GetNamespaces(
            IEnumerable<ActorDescription> actors, 
            params Type[] additionalTypes)
        {
            var namespaces =
                actors.SelectMany(GetTypes)
                    .Concat(additionalTypes.SelectMany(GetTypes))
                    .Select(type => type.Namespace)
                    .Distinct();

            return namespaces.Select(ns => SyntaxFactory.ParseName(ns));
        }

        /// <summary>
        /// Returns the <see cref="MethodInfo"/> for the simple method call in the provided <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The containing type of the method.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The return type of the method.
        /// </typeparam>
        /// <param name="expression">
        /// The expression.
        /// </param>
        /// <returns>
        /// The <see cref="MethodInfo"/> for the simple method call in the provided <paramref name="expression"/>.
        /// </returns>
        public static MethodInfo Method<T, TResult>(Expression<Func<T, TResult>> expression)
        {
            var methodCall = expression.Body as MethodCallExpression;
            if (methodCall != null)
            {
                return methodCall.Method;
            }
            
            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns the <see cref="MethodInfo"/> for the simple method call in the provided <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The containing type of the method.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The return type of the method.
        /// </typeparam>
        /// <param name="expression">
        /// The expression.
        /// </param>
        /// <param name="typeParams">
        /// The type parameters to the declaring type.
        /// </param>
        /// <returns>
        /// The <see cref="MethodInfo"/> for the simple method call in the provided <paramref name="expression"/>.
        /// </returns>
        public static MethodInfo GenericTypeMethod<T, TResult>(Expression<Func<T, TResult>> expression, Type[] typeParams)
        {
            var methodCall = expression.Body as MethodCallExpression;
            if (methodCall != null)
            {
                var untypedMethod = methodCall.Method;
                Debug.Assert(untypedMethod.DeclaringType != null, "untypedMethod.DeclaringType != null");
                return untypedMethod.DeclaringType.GetGenericTypeDefinition().MakeGenericType(typeParams).GetMethod(untypedMethod.Name);
            }

            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns the <see cref="MethodInfo"/> for the simple method call in the provided <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The containing type of the method.
        /// </typeparam>
        /// <param name="expression">
        /// The expression.
        /// </param>
        /// <returns>
        /// The <see cref="MethodInfo"/> for the simple method call in the provided <paramref name="expression"/>.
        /// </returns>
        public static MethodInfo Method<T>(Expression<Action<T>> expression)
        {
            var methodCall = expression.Body as MethodCallExpression;
            if (methodCall != null)
            {
                return methodCall.Method;
            }

            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns the types referenced by the provided <paramref name="actor"/>.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// The types referenced by the provided <paramref name="actor"/>.
        /// </returns>
        public static IEnumerable<Type> GetTypes(ActorDescription actor)
        {
            foreach (var type in GetTypes(actor.Type))
            {
                yield return type;
            }

            foreach (var type in actor.Methods.Values.SelectMany(GetTypes))
            {
                yield return type;
            }
        }

        /// <summary>
        /// Returns the types referenced by the provided <paramref name="method"/>.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The types referenced by the provided <paramref name="method"/>.
        /// </returns>
        public static IEnumerable<Type> GetTypes(ActorMethodDescription method)
        {
            foreach (var type in GetTypes(method.MethodInfo.ReturnType))
            {
                yield return type;
            }

            foreach (
                var parameterType in
                    method.MethodInfo.GetParameters().SelectMany(parameter => GetTypes(parameter.ParameterType)))
            {
                yield return parameterType;
            }
        }

        /// <summary>
        /// Returns the types referenced by the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The types referenced by the provided <paramref name="type"/>.
        /// </returns>
        public static IEnumerable<Type> GetTypes(Type type)
        {
            yield return type;
            if (type.IsGenericType)
            {
                foreach (var generic in type.GetGenericArguments().SelectMany(GetTypes))
                {
                    yield return generic;
                }
            }
        }
    }
}
