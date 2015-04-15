// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventInvokerGenerator.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Execution
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using EventJournal.Exceptions;
    using EventJournal.Metadata;
    using EventJournal.Utilities;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using Orleans;

    using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// The event replay dispatcher generator.
    /// </summary>
    public static class EventInvokerGenerator
    {
        /// <summary>
        /// The class suffix.
        /// </summary>
        private const string ClassSuffix = "EventInvoker";

        /// <summary>
        /// The compiled assemblies.
        /// </summary>
        private static readonly ConcurrentDictionary<Assembly, Tuple<Assembly, string>> CompiledAssemblies =
            new ConcurrentDictionary<Assembly, Tuple<Assembly, string>>();

        /// <summary>
        /// Gets a dispatcher for the provided type.
        /// </summary>
        /// <typeparam name="T">
        /// The actor interface type.
        /// </typeparam>
        /// <returns>
        /// A dispatcher for the provided type.
        /// </returns>
        public static IEventInvoker<T> Create<T>() where T : IGrain
        {
            var assembly = GetOrCompileAssembly(typeof(T).Assembly).Item1;

            var dispatcher = assembly.GetTypes().First(typeof(IEventInvoker<T>).IsAssignableFrom);
            return (IEventInvoker<T>)Activator.CreateInstance(dispatcher);
        }

        /// <summary>
        /// Returns all currently compiled source code.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// All currently compiled source code.
        /// </returns>
        public static string GetSource(ActorDescription actor)
        {
            return GetOrCompileAssembly(actor.Type.Assembly).Item2;
        }

        /// <summary>
        /// Gets the compiled assembly for the provided actor assembly.
        /// </summary>
        /// <param name="actorAssembly">
        /// The actor assembly.
        /// </param>
        /// <returns>
        /// The compiled assembly for the provided actor assembly.
        /// </returns>
        private static Tuple<Assembly, string> GetOrCompileAssembly(Assembly actorAssembly)
        {
            return CompiledAssemblies.GetOrAdd(
                actorAssembly, 
                assembly =>
                {
                    string source;
                    var asm =
                        CompileAssembly(
                            ActorDescriptionGenerator.GetActorDescriptions(actorAssembly).Values.ToList(), 
                            out source);
                    return Tuple.Create(asm, source);
                });
        }

        /// <summary>
        /// The compile assembly.
        /// </summary>
        /// <param name="actors">
        /// The actors.
        /// </param>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <returns>
        /// The <see cref="Assembly"/>.
        /// </returns>
        /// <exception cref="CodeGenerationException">
        /// An error occurred generating code.
        /// </exception>
        private static Assembly CompileAssembly(IList<ActorDescription> actors, out string source)
        {
            var assemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asm => !asm.IsDynamic && !string.IsNullOrWhiteSpace(asm.Location))
                    .Select(MetadataReference.CreateFromAssembly)
                    .ToArray();

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var generateCompilationUnit = GenerateCompilationUnit(actors);
            var syntax = generateCompilationUnit.NormalizeWhitespace();
            source = syntax.ToFullString();

            var compilation =
                CSharpCompilation.Create("CodeGen_" + ClassSuffix + "_" + DateTime.UtcNow.Ticks.ToString("X") + ".dll")
                    .AddSyntaxTrees(generateCompilationUnit.SyntaxTree)
                    .AddReferences(assemblies)
                    .WithOptions(options);

            Assembly compiledAssembly;
            using (var stream = new MemoryStream())
            {
                var compilationResult = compilation.Emit(stream);
                if (!compilationResult.Success)
                {
                    throw new CodeGenerationException(
                        string.Join("\n", compilationResult.Diagnostics.Select(_ => _.ToString())));
                }

                compiledAssembly = Assembly.Load(stream.GetBuffer());
            }

            return compiledAssembly;
        }

        /// <summary>
        /// Returns compilation unit syntax for dispatching events to the provided <paramref name="actors"/>.
        /// </summary>
        /// <param name="actors">
        /// The actor descriptions.
        /// </param>
        /// <returns>
        /// Compilation unit syntax for dispatching events to the provided <paramref name="actors"/>.
        /// </returns>
        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Array is never mutated.")]
        private static CompilationUnitSyntax GenerateCompilationUnit(IList<ActorDescription> actors)
        {
            var usings =
                TypeUtil.GetNamespaces(actors, typeof(TaskUtility)).Select(SF.UsingDirective).ToList();
            var ns =
                SF.NamespaceDeclaration(SF.ParseName("Generated" + DateTime.UtcNow.Ticks))
                    .AddUsings(usings.ToArray())
                    .AddMembers(actors.Select(GenerateClass).ToArray());
            return SF.CompilationUnit().AddMembers(ns);
        }

        /// <summary>
        /// Generates the class for the provided actor.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// The generated class.
        /// </returns>
        private static TypeDeclarationSyntax GenerateClass(ActorDescription actor)
        {
            var replayType = SF.SimpleBaseType(typeof(IEventInvoker<>).MakeGenericType(actor.Type).GetTypeSyntax());

            return
                SF.ClassDeclaration(GetClassName(actor))
                    .AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                    .AddBaseListTypes(replayType)
                    .AddMembers(GenerateInvokeMethod(actor))
                    .AddMembers(GenerateGetTypeArgumentsMethod(actor));
        }

        /// <summary>
        /// The generate method.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// The <see cref="MethodDeclarationSyntax"/>.
        /// </returns>
        private static MethodDeclarationSyntax GenerateInvokeMethod(ActorDescription actor)
        {
            // Get the method with the correct type.
            var method =
                TypeUtil.GenericTypeMethod(
                    (IEventInvoker<IGrain> x) => x.Invoke(default(IGrain), default(Event)), 
                    new[] { actor.Type });

            var methodDeclaration = method.GetMethodDeclarationSyntax();
            var parameters = method.GetParameters();

            var @event =
                SF.IdentifierName(parameters.First(_ => typeof(Event) == _.ParameterType).Name);

            var instance =
                SF.IdentifierName(parameters.First(_ => actor.Type == _.ParameterType).Name);

            var switchCases = GenerateSwitchCases(actor, m => GenerateDispatchBlockForMethod(m, instance, @event)).ToArray();
            var returnNull = SF.ReturnStatement(SF.LiteralExpression(SyntaxKind.NullLiteralExpression));
            var defaultSection = SF.SwitchSection().AddLabels(SF.DefaultSwitchLabel()).AddStatements(returnNull);
            var kindSwitch =
                SF.SwitchStatement(@event.Member((Event _) => _.Type))
                    .AddSections(switchCases)
                    .AddSections(defaultSection);
            return
                methodDeclaration
                    .AddBodyStatements(kindSwitch);
        }

        /// <summary>
        /// The get producer class name.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetClassName(ActorDescription actor)
        {
            var plainName = actor.Type.GetUnadornedTypeName();
            string cleanName;
            if (actor.Type.IsInterface && plainName.StartsWith("i", StringComparison.OrdinalIgnoreCase))
            {
                cleanName = plainName.Substring(1);
            }
            else
            {
                cleanName = plainName;
            }

            return cleanName + ClassSuffix;
        }

        /// <summary>
        /// Generates switch cases for the provided actor.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <param name="generateSwitchBlock">
        /// The function used to generate switch block statements for each method.
        /// </param>
        /// <returns>
        /// The switch cases for the provided actor.
        /// </returns>
        private static IEnumerable<SwitchSectionSyntax> GenerateSwitchCases(ActorDescription actor, Func<ActorMethodDescription, StatementSyntax> generateSwitchBlock)
        {
            return actor.Methods.Values.Select(
                m =>
                {
                    var label =
                        SF.CaseSwitchLabel(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(m.Name)));
                    return
                        SF.SwitchSection()
                            .AddLabels(label)
                            .AddStatements(generateSwitchBlock(m));
                });
        }

        /// <summary>
        /// The generate dispatch block for method.
        /// </summary>
        /// <param name="methodDescription">
        /// The method description.
        /// </param>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <returns>
        /// The <see cref="StatementSyntax"/>.
        /// </returns>
        private static StatementSyntax GenerateDispatchBlockForMethod(ActorMethodDescription methodDescription, IdentifierNameSyntax instance, IdentifierNameSyntax @event)
        {
            // Construct expressions to retrieve each of the method's parameters.
            var method = methodDescription.MethodInfo;
            var parameters = new List<ExpressionSyntax>();
            var methodParameters = method.GetParameters().ToList();
            for (var i = 0; i < methodParameters.Count; i++)
            {
                var parameter = methodParameters[i];
                var parameterType = parameter.ParameterType.GetTypeSyntax();
                var indexArg =
                    SF.Argument(
                        SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(i)));
                var getArg =
                    SF.InvocationExpression(@event.Member("Arg", parameterType)).AddArgumentListArguments(indexArg);
                parameters.Add(getArg);
            }

            var grainMethodCall =
                SF.InvocationExpression(instance.Member(method.Name))
                    .AddArgumentListArguments(parameters.Select(SF.Argument).ToArray());

            return SF.ReturnStatement(SF.InvocationExpression(grainMethodCall.Member("Box")));
        }

        /// <summary>
        /// Returns syntax for the <see cref="IEventInvoker{T}.GetArgumentTypes"/> method.
        /// </summary>
        /// <param name="actor">
        /// The actor description.
        /// </param>
        /// <returns>
        /// Syntax for the <see cref="IEventInvoker{T}.GetArgumentTypes"/> method.
        /// </returns>
        private static MemberDeclarationSyntax GenerateGetTypeArgumentsMethod(ActorDescription actor)
        {
            var method = TypeUtil.GenericTypeMethod(
                (IEventInvoker<IGrain> m) => m.GetArgumentTypes(default(string)), 
                new[] { actor.Type });
            var methodDeclaration = method.GetMethodDeclarationSyntax();
            var parameters = method.GetParameters();

            var eventTypeArg =
                SF.IdentifierName(parameters.First(_ => typeof(string) == _.ParameterType).Name);

            var switchCases = GenerateSwitchCases(actor, GenerateGetTypeArgumentsSwitchBlock).ToArray();
            var returnNull = SF.ReturnStatement(SF.LiteralExpression(SyntaxKind.NullLiteralExpression));
            var defaultSection = SF.SwitchSection().AddLabels(SF.DefaultSwitchLabel()).AddStatements(returnNull);
            var kindSwitch =
                SF.SwitchStatement(eventTypeArg)
                    .AddSections(switchCases)
                    .AddSections(defaultSection);
            return methodDeclaration.AddBodyStatements(kindSwitch);
        }

        /// <summary>
        /// Returns syntax for individual switch cases within the <see cref="IEventInvoker{T}.GetArgumentTypes"/> method.
        /// </summary>
        /// <param name="methodDescription">
        /// The method description.
        /// </param>
        /// <returns>
        /// Syntax for individual switch cases within the <see cref="IEventInvoker{T}.GetArgumentTypes"/> method.
        /// </returns>
        private static StatementSyntax GenerateGetTypeArgumentsSwitchBlock(ActorMethodDescription methodDescription)
        {
            var method = methodDescription.MethodInfo;
            var parameters = method.GetParameters().ToList();

            var argumentTypes =
                parameters.Select(p => SF.TypeOfExpression(p.ParameterType.GetTypeSyntax()))
                    .Cast<ExpressionSyntax>()
                    .ToArray();

            var argumentTypeArray = SF.ArrayCreationExpression(typeof(Type).GetArrayTypeSyntax())
                .WithInitializer(
                    SF.InitializerExpression(SyntaxKind.ArrayInitializerExpression).AddExpressions(argumentTypes));

            return SF.ReturnStatement(argumentTypeArray);
        }
    }
}
