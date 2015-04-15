// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="SyntaxFactoryExtensions.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// The syntax factory extensions.
    /// </summary>
    internal static class SyntaxFactoryExtensions
    {
        /// <summary>
        /// Returns the provided string as a literal expression.
        /// </summary>
        /// <param name="str">
        /// The string.
        /// </param>
        /// <returns>
        /// The literal expression.
        /// </returns>
        public static LiteralExpressionSyntax GetLiteralExpression(this string str)
        {
            var syntaxToken = SyntaxFactory.Literal(
                SyntaxFactory.TriviaList(), 
                @"""" + str + @"""", 
                str, 
                SyntaxFactory.TriviaList());
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, syntaxToken);
        }

        /// <summary>
        /// Returns <see cref="NameSyntax"/> representing the namespace of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// <see cref="NameSyntax"/> representing the namespace of <paramref name="type"/>.
        /// </returns>
        public static NameSyntax GetNamespaceSyntax(this Type type)
        {
            return SyntaxFactory.ParseName(type.Namespace);
        }

        /// <summary>
        /// Returns <see cref="TypeSyntax"/> specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        /// <returns>
        /// <see cref="TypeSyntax"/> specified <paramref name="type"/>.
        /// </returns>
        public static TypeSyntax GetTypeSyntax(this Type type, bool includeNamespace = true)
        {
            return SyntaxFactory.ParseTypeName(type.GetParseableName(includeNamespace));
        }

        /// <summary>
        /// Returns <see cref="ArrayTypeSyntax"/> representing the array form of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        /// <returns>
        /// <see cref="ArrayTypeSyntax"/> representing the array form of <paramref name="type"/>.
        /// </returns>
        public static ArrayTypeSyntax GetArrayTypeSyntax(this Type type, bool includeNamespace = true)
        {
            return
                SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(type.GetParseableName(includeNamespace)))
                    .AddRankSpecifiers(
                        SyntaxFactory.ArrayRankSpecifier().AddSizes(SyntaxFactory.OmittedArraySizeExpression()));
        }

        /// <summary>
        /// Returns the method declaration syntax for the provided method.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The method declaration syntax for the provided method.
        /// </returns>
        public static MethodDeclarationSyntax GetMethodDeclarationSyntax(this MethodInfo method)
        {
            var syntax =
                SyntaxFactory.MethodDeclaration(method.ReturnType.GetTypeSyntax(), method.Name)
                    .WithParameterList(SyntaxFactory.ParameterList().AddParameters(method.GetParameterListSyntax()));
            if (method.IsPublic)
            {
                syntax = syntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }
            else if (method.IsPrivate)
            {
                syntax = syntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
            }

            return syntax;
        }

        /// <summary>
        /// Returns the parameter list syntax for the provided method.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The parameter list syntax for the provided method.
        /// </returns>
        public static ParameterSyntax[] GetParameterListSyntax(this MethodInfo method)
        {
            return
                method.GetParameters()
                    .Select(
                        parameter =>
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Name))
                            .WithType(parameter.ParameterType.GetTypeSyntax()))
                    .ToArray();
        }

        /// <summary>
        /// Returns a string representation of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        /// <returns>
        /// <see cref="NameSyntax"/> representing the namespace of <paramref name="type"/>.
        /// </returns>
        public static string GetParseableName(this Type type, bool includeNamespace = true)
        {
            var builder = new StringBuilder();
            GetParseableName(type, builder, new Queue<Type>(type.GenericTypeArguments), includeNamespace);
            return builder.ToString();
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <param name="typeParameters">
        /// The type parameters.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(
            this ExpressionSyntax instance, 
            string member, 
            params TypeSyntax[] typeParameters)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression, 
                instance, 
                SyntaxFactory.GenericName(member).AddTypeArgumentListArguments(typeParameters));
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, string member)
        {
            return instance.Member(SyntaxFactory.IdentifierName(member));
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <typeparam name="TInstance">
        /// The class type.
        /// </typeparam>
        /// <typeparam name="T">
        /// The member return type.
        /// </typeparam>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member<TInstance, T>(this ExpressionSyntax instance, Expression<Func<TInstance, T>> member)
        {
            var methodCall = member.Body as MethodCallExpression;
            if (methodCall != null)
            {
                return instance.Member(SyntaxFactory.IdentifierName(methodCall.Method.Name));
            }

            var memberAccess = member.Body as MemberExpression;
            if (memberAccess != null)
            {
                return instance.Member(SyntaxFactory.IdentifierName(memberAccess.Member.Name));
            }

            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, IdentifierNameSyntax member)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, member);
        }

        /// <summary>
        /// Returns the non-generic type name without any special characters.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The non-generic type name without any special characters.
        /// </returns>
        public static string GetUnadornedTypeName(this Type type)
        {
            var index = type.Name.IndexOf('`');

            return index > 0 ? type.Name.Substring(0, index) : type.Name;
        }

        /// <summary>
        /// Returns a string representation of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> to append results to.
        /// </param>
        /// <param name="typeArguments">
        /// The type arguments of <paramref name="type"/>.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        private static void GetParseableName(
            Type type, 
            StringBuilder builder, 
            Queue<Type> typeArguments, 
            bool includeNamespace = true)
        {
            if (type.DeclaringType != null)
            {
                // This is not the root type.
                GetParseableName(type.DeclaringType, builder, typeArguments, includeNamespace);
                builder.Append('.');
            }
            else if (!string.IsNullOrWhiteSpace(type.Namespace) && includeNamespace)
            {
                // This is the root type.
                builder.Append(type.Namespace + '.');
            }

            if (type.IsGenericType)
            {
                // Get the unadorned name, the generic parameters, and add them together.
                var unadornedTypeName = type.GetUnadornedTypeName();

                var generics =
                    Enumerable.Range(0, Math.Min(type.GetGenericArguments().Count(), typeArguments.Count))
                        .Select(_ => typeArguments.Dequeue());
                var genericParameters = string.Join(
                    ",", 
                    generics.Select(generic => GetParseableName(generic, includeNamespace)));
                builder.AppendFormat("{0}<{1}>", unadornedTypeName, genericParameters);
            }
            else
            {
                builder.Append(type.Name);
            }
        }
    }
}