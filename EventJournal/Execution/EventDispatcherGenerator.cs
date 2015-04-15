// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="EventDispatcherGenerator.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Execution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using EventJournal.Exceptions;
    using EventJournal.Metadata;
    using EventJournal.Utilities;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using Orleans;

    using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Code generator for routing method calls to actors.
    /// </summary>
    public static class EventDispatcherGenerator
    {
        /// <summary>
        /// The class suffix.
        /// </summary>
        private const string ClassSuffix = "EventDispatcher";

        /// <summary>
        /// Gets a dispatcher for the provided <paramref name="actors"/>.
        /// </summary>
        /// <param name="actors">
        /// The actors.
        /// </param>
        /// <param name="source">
        /// The generated source code.
        /// </param>
        /// <returns>
        /// A dispatcher for the provided <paramref name="actors"/>.
        /// </returns>
        /// <exception cref="CodeGenerationException">
        /// A code generation error occurred.
        /// </exception>
        public static IEventDispatcher GetDispatcher(IList<ActorDescription> actors, out string source)
        {
            var assemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asm => !asm.IsDynamic && !string.IsNullOrWhiteSpace(asm.Location))
                    .Select(MetadataReference.CreateFromAssembly)
                    .ToArray();

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var syntax = GenerateCompilationUnit(actors).NormalizeWhitespace();
            source = syntax.ToFullString();

            var compilation =
                CSharpCompilation.Create("CodeGen_" + ClassSuffix + DateTime.UtcNow.Ticks.ToString("X") + ".dll")
                    .AddSyntaxTrees(GenerateCompilationUnit(actors).SyntaxTree)
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

            var dispatcher = compiledAssembly.GetTypes().Single(typeof(IEventDispatcher).IsAssignableFrom);
            return (IEventDispatcher)Activator.CreateInstance(dispatcher);
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
        private static CompilationUnitSyntax GenerateCompilationUnit(IList<ActorDescription> actors)
        {
            var usings =
                TypeUtil.GetNamespaces(actors, typeof(TaskUtility), typeof(GrainFactory), typeof(IEventDispatcher))
                    .Select(SF.UsingDirective)
                    .ToList();
            var ns =
                SF.NamespaceDeclaration(SF.ParseName("Generated" + DateTime.UtcNow.Ticks))
                    .AddUsings(usings.ToArray())
                    .AddMembers(GenerateClass(actors));
            return SF.CompilationUnit().AddMembers(ns);
        }

        /// <summary>
        /// Returns class syntax for dispatching events to the provided <paramref name="actors"/>.
        /// </summary>
        /// <param name="actors">
        /// The actor descriptions.
        /// </param>
        /// <returns>
        /// Class syntax for dispatching events to the provided <paramref name="actors"/>.
        /// </returns>
        private static TypeDeclarationSyntax GenerateClass(IEnumerable<ActorDescription> actors)
        {
            var eventDispatcher = SF.SimpleBaseType(typeof(IEventDispatcher).GetTypeSyntax(false));
            return
                SF.ClassDeclaration(ClassSuffix)
                    .AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                    .AddBaseListTypes(eventDispatcher)
                    .AddMembers(GenerateMethod(actors));
        }

        /// <summary>
        /// Returns method syntax for dispatching events to the provided <paramref name="actors"/>.
        /// </summary>
        /// <param name="actors">
        /// The actor descriptions.
        /// </param>
        /// <returns>
        /// Method syntax for dispatching events to the provided <paramref name="actors"/>.
        /// </returns>
        private static MethodDeclarationSyntax GenerateMethod(IEnumerable<ActorDescription> actors)
        {
            // Types
            var eventType = typeof(Event).GetTypeSyntax();
            var returnType = typeof(Task<object>).GetTypeSyntax();

            // Local variables
            var @event = SF.IdentifierName("@event");

            // Parameters
            var eventParam = SF.Parameter(@event.Identifier).WithType(eventType);

            // Body statements
            var returnNull = SF.ReturnStatement(SF.LiteralExpression(SyntaxKind.NullLiteralExpression));
            var defaultSection = SF.SwitchSection().AddLabels(SF.DefaultSwitchLabel()).AddStatements(returnNull);
            var kindSwitch =
                SF.SwitchStatement(@event.Member("To").Member("Kind"))
                    .AddSections(
                        actors.Where(actor => actor.Methods.Any())
                            .Select(actor => GetActorSwitch(actor, @event))
                            .ToArray())
                    .AddSections(defaultSection);

            // Build and return the method.
            return
                SF.MethodDeclaration(returnType, "Dispatch")
                    .AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(eventParam)
                    .AddBodyStatements(kindSwitch);
        }

        /// <summary>
        /// Returns syntax for dispatching <paramref name="event"/> to <paramref name="actor"/>.
        /// </summary>
        /// <param name="actor">
        /// The actor description.
        /// </param>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <returns>
        /// Syntax for dispatching <paramref name="event"/> to <paramref name="actor"/>.
        /// </returns>
        private static SwitchSectionSyntax GetActorSwitch(
            ActorDescription actor, 
            ExpressionSyntax @event)
        {
            var label =
                SF.CaseSwitchLabel(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(actor.Kind)));
            return
                SF.SwitchSection()
                    .AddLabels(label)
                    .AddStatements(GenerateActorBlock(@event, actor));
        }

        /// <summary>
        /// Returns syntax for dispatching <paramref name="event"/> to <paramref name="actor"/>.
        /// </summary>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <param name="actor">
        /// The actor description.
        /// </param>
        /// <returns>
        /// Syntax for dispatching <paramref name="event"/> to <paramref name="actor"/>.
        /// </returns>
        private static StatementSyntax GenerateActorBlock(
            ExpressionSyntax @event, 
            ActorDescription actor)
        {
            var @var = SF.IdentifierName("var");
            var grainType = actor.Type.GetTypeSyntax();
            var getGrain =
                SF.InvocationExpression(SF.IdentifierName("GrainFactory").Member("GetGrain", grainType))
                    .AddArgumentListArguments(SF.Argument(@event.Member("To").Member("Id")));
            var grain = SF.VariableDeclarator("grain").WithInitializer(SF.EqualsValueClause(getGrain));
            var grainDeclaration = SF.LocalDeclarationStatement(SF.VariableDeclaration(@var).AddVariables(grain));

            var returnNull = SF.ReturnStatement(SF.LiteralExpression(SyntaxKind.NullLiteralExpression));
            
            var defaultSection = SF.SwitchSection().AddLabels(SF.DefaultSwitchLabel()).AddStatements(returnNull);
            var methodSwitch =
                SF.SwitchStatement(@event.Member("Type"))
                    .AddSections(
                        actor.Methods.Values.Where(_ => _.Visible)
                            .Select(method => GetMethodSwitchCase(@event, method))
                            .ToArray())
                    .AddSections(defaultSection);
            var methodDispatcher = SF.Block().AddStatements(grainDeclaration, methodSwitch);
            return methodDispatcher;
        }

        /// <summary>
        /// Returns syntax for dispatching <paramref name="event"/> to <paramref name="method"/>.
        /// </summary>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <param name="method">
        /// The actor description.
        /// </param>
        /// <returns>
        /// Syntax for dispatching <paramref name="event"/> to <paramref name="method"/>.
        /// </returns>
        private static SwitchSectionSyntax GetMethodSwitchCase(ExpressionSyntax @event, ActorMethodDescription method)
        {
            var label =
                SF.CaseSwitchLabel(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(method.Name)));
            return
                SF.SwitchSection()
                    .AddLabels(label)
                    .AddStatements(
                        GenerateMethodDispatcher(@event, SF.IdentifierName("grain"), method.MethodInfo));
        }

        /// <summary>
        /// Returns syntax for dispatching <paramref name="event"/> to <paramref name="method"/> on <paramref name="grain"/>.
        /// </summary>
        /// <param name="event">
        /// The event.
        /// </param>
        /// <param name="grain">
        /// The grain.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// Syntax for dispatching <paramref name="event"/> to <paramref name="method"/> on <paramref name="grain"/>.
        /// </returns>
        private static StatementSyntax GenerateMethodDispatcher(
            ExpressionSyntax @event, 
            ExpressionSyntax grain, 
            MethodInfo method)
        {
            // Construct expressions to retrieve each of the method's parameters, starting with the 'self' parameter.
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
                SF.InvocationExpression(grain.Member(method.Name))
                    .AddArgumentListArguments(parameters.Select(SF.Argument).ToArray());

            return SF.ReturnStatement(SF.InvocationExpression(grainMethodCall.Member("Box")));
        }
    }
}