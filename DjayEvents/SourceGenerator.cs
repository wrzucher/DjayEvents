using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace DjayEvents;

[Generator]
public sealed class EventBinderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidateMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsMethodWithAttributes(s),
                transform: static (ctx, _) => GetMethodSymbol(ctx))
            .Where(m => m is not null)!;

        context.RegisterSourceOutput(candidateMethods.Collect(), Execute);
    }

    private static bool IsMethodWithAttributes(SyntaxNode node)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    private static IMethodSymbol? GetMethodSymbol(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDecl)
            return null;

        return context.SemanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<IMethodSymbol?> methods)
    {
        if (methods.IsDefaultOrEmpty) return;

        var grouped = methods
            .Where(_ => _ is not null)
            .GroupBy(_ => (Publisher: FindPublisher(_!), Subscriber: _!.ContainingType))
            .Where(g => g.Key.Publisher is not null);

        foreach (var group in grouped)
        {
            var publisher = group.Key.Publisher!;
            var subscriber = group.Key.Subscriber;
            var className = $"{publisher.Name}_{subscriber.Name}_EventBinder";

            var sb = new StringBuilder();
            sb.AppendLine("using EventBinding.Core;");
            sb.AppendLine("using EventBinding.Extensions;");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace EventBinding.Generated");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : IEventBinder");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly {publisher.ToDisplayString()} _publisher;");
            sb.AppendLine($"        private readonly {subscriber.ToDisplayString()} _subscriber;");
            sb.AppendLine("        private readonly IInMemoryEventBus _bus;");
            sb.AppendLine("        private readonly EventBindingMode _mode;");
            sb.AppendLine();
            sb.AppendLine($"        public {className}({publisher.ToDisplayString()} pub, {subscriber.ToDisplayString()} sub, IInMemoryEventBus bus = null, EventBindingMode mode = EventBindingMode.Direct)");
            sb.AppendLine("        {");
            sb.AppendLine("            _publisher = pub;");
            sb.AppendLine("            _subscriber = sub;");
            sb.AppendLine("            _bus = bus;");
            sb.AppendLine("            _mode = mode;");
            sb.AppendLine();
            sb.AppendLine("            if (_mode == EventBindingMode.Direct)");
            sb.AppendLine("            {");

            foreach (var method in group)
            {
                var evt = FindMatchingEvent(publisher, method!);
                if (evt is null) continue;
                sb.AppendLine($"                _publisher.{evt.Name} += _subscriber.{method!.Name};");
            }

            sb.AppendLine("            }");
            sb.AppendLine("            else if (_mode == EventBindingMode.InMemoryQueue)");
            sb.AppendLine("            {");

            foreach (var method in group)
            {
                var evt = FindMatchingEvent(publisher, method!);
                if (evt is null) continue;
                var evtType = ((INamedTypeSymbol)evt.Type).TypeArguments[0];
                sb.AppendLine($"                _bus.Subscribe<{evtType.ToDisplayString()}>(_subscriber.{method!.Name});");
            }

            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        public void Bind() { }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static INamedTypeSymbol? FindPublisher(IMethodSymbol method)
    {
        var param = method.Parameters.FirstOrDefault();
        if (param == null) return null;

        // ищем в той же сборке и во всех referenced assemblies
        var assemblies = method.ContainingAssembly.Modules
            .SelectMany(m => m.ReferencedAssemblySymbols)
            .Concat([method.ContainingAssembly]);

        foreach (var asm in assemblies)
        {
            foreach (var type in GetAllTypes(asm.GlobalNamespace))
            {
                foreach (var evt in type.GetMembers().OfType<IEventSymbol>())
                {
                    if (evt.Type is INamedTypeSymbol evtType &&
                        evtType.IsGenericType &&
                        SymbolEqualityComparer.Default.Equals(evtType.TypeArguments[0], param.Type))
                    {
                        return type;
                    }
                }
            }
        }
        return null;
    }

    private static IEventSymbol? FindMatchingEvent(INamedTypeSymbol publisher, IMethodSymbol method)
    {
        var param = method.Parameters.FirstOrDefault();
        if (param == null) return null;

        return publisher.GetMembers()
            .OfType<IEventSymbol>()
            .FirstOrDefault(evt =>
                evt.Type is INamedTypeSymbol evtType &&
                evtType.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(evtType.TypeArguments[0], param.Type));
    }

    /// <summary>
    /// Рекурсивный обход namespace → классы
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
            yield return type;

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(nestedNs))
                yield return type;
        }
    }
}
