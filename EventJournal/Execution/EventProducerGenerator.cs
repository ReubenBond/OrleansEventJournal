// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventProducerGenerator.cs" company="Dapr Labs">
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
    using System.Threading.Tasks;

    using EventJournal.Exceptions;
    using EventJournal.Journal;
    using EventJournal.Metadata;
    using EventJournal.Utilities;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using Orleans;

    using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// The event producer generator.
    /// </summary>
    public static class EventProducerGenerator
    {
        /// <summary>
        /// The class suffix.
        /// </summary>
        private const string ClassSuffix = "EventProducer";

        /// <summary>
        /// The compiled assemblies.
        /// </summary>
        private static readonly ConcurrentDictionary<Assembly, Tuple<Assembly, string>> CompiledAssemblies =
            new ConcurrentDictionary<Assembly, Tuple<Assembly, string>>();

        /// <summary>
        /// Gets a dispatcher for the provided type.
        /// </summary>
        /// <param name="journal">
        /// The journal.
        /// </param>
        /// <param name="nextEventId">
        /// The id of the next event to be written.
        /// </param>
        /// <typeparam name="T">
        /// The actor interface type.
        /// </typeparam>
        /// <returns>
        /// A dispatcher for the provided type.
        /// </returns>
        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Satisfying type is dynamically generated.")]
        public static IEventProducer<T> Create<T>(IJournal journal, long nextEventId) where T : class, IGrain
        {
            var assembly = GetOrCompileAssembly(typeof(T).Assembly).Item1;

            var dispatcher = assembly.GetTypes().Single(typeof(T).IsAssignableFrom);
            var instance = (IEventProducer<T>)Activator.CreateInstance(dispatcher);
            
            // Set the fields required for the event producer to work.
            instance.Journal = journal;
            instance.NextEventId = nextEventId;
            
            return instance;
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
                    .Concat(new[] { typeof(Event).Assembly })
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
        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Array is never mutated.")]
        private static TypeDeclarationSyntax GenerateClass(ActorDescription actor)
        {
            var producerType = SF.SimpleBaseType(actor.Type.GetTypeSyntax());
            var helperType = SF.SimpleBaseType(typeof(EventProducerBase<>).MakeGenericType(actor.Type).GetTypeSyntax());
            return
                SF.ClassDeclaration(GetClassName(actor))
                    .AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
                    .AddBaseListTypes(helperType, producerType)
                    .AddMembers(GenerateMethods(actor).ToArray());
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
        /// The generate producer methods.
        /// </summary>
        /// <param name="actor">
        /// The actor.
        /// </param>
        /// <returns>
        /// The producer methods.
        /// </returns>
        private static IEnumerable<MethodDeclarationSyntax> GenerateMethods(ActorDescription actor)
        {
            return actor.Methods.Values.Select(GenerateMethod);
        }

        /// <summary>
        /// Generates a method.
        /// </summary>
        /// <param name="methodDescription">
        /// The method description.
        /// </param>
        /// <returns>
        /// The generated method.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The return type of the provided method is not supported.
        /// </exception>
        private static MethodDeclarationSyntax GenerateMethod(ActorMethodDescription methodDescription)
        {
            // Types
            var method = methodDescription.MethodInfo;

            Type asyncReturnType;
            if (!method.ReturnType.IsGenericType && (method.ReturnType == typeof(Task)))
            {
                asyncReturnType = typeof(void);
            }
            else if (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                asyncReturnType = method.ReturnType.GenericTypeArguments[0];
            }
            else
            {
                throw new ArgumentException("Method return type is not Task or Task<T>.");
            }

            // Body statements
            var parameterReferences =
                method.GetParameters()
                    .Select(
                        p => SF.Argument(SF.CastExpression(typeof(object).GetTypeSyntax(), SF.IdentifierName(p.Name))))
                    .ToArray();
            var writeEventMethod = SF.ThisExpression().Member((EventProducerBase<object> _) => _.WriteEvent(default(string), default(object), default(object)));
            var writeEvent =
                SF.ExpressionStatement(
                    SF.AwaitExpression(
                        SF.InvocationExpression(writeEventMethod)
                            .AddArgumentListArguments(SF.Argument(methodDescription.Name.GetLiteralExpression()))
                            .AddArgumentListArguments(parameterReferences)));

            var returnValue = asyncReturnType == typeof(void) ? null : SF.DefaultExpression(asyncReturnType.GetTypeSyntax());

            // Build and return the method.
            return
                SF.MethodDeclaration(method.ReturnType.GetTypeSyntax(), methodDescription.MethodInfo.Name)
                    .AddModifiers(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.AsyncKeyword))
                    .AddParameterListParameters(method.GetParameterListSyntax())
                    .AddBodyStatements(writeEvent, SF.ReturnStatement(returnValue));
        }

        /// <summary>
        /// The code analysis fix.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Fix for Code Analysis bug.")]
        [ExcludeFromCodeCoverage]
        private class CodeAnalysisFix : IEventProducer<IGrain>, IGrain
        {
            /// <summary>
            /// Gets or sets the journal.
            /// </summary>
            public IJournal Journal { get; set; }

            /// <summary>
            /// Gets or sets the id of the next event to be written.
            /// </summary>
            public long NextEventId { get; set; }
            

            /// <summary>
            /// Gets the event producer interface implementation.
            /// </summary>
            public IGrain Interface
            {
                get
                {
                    return this;
                }
            }

            /// <summary>
            /// Appends the specified event to the journal.
            /// </summary>
            /// <param name="type">
            /// The event type.
            /// </param>
            /// <param name="args">
            /// The event arguments.
            /// </param>
            /// <returns>
            /// A <see cref="Task"/> representing the work performed.
            /// </returns>
            public Task WriteEvent(string type, params object[] args)
            {
                return default(Task);
            }
        }

        /// <summary>
        /// The code analysis fix for event producer base.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Fix for Code Analysis bug.")]
        private class CodeAnalysisFixForEventProducerBase : EventProducerBase<IGrain>, IGrain
        {
        }
    }
}
