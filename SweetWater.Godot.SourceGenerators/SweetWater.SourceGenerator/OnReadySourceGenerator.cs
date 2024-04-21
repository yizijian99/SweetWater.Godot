using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Godot.SweetWater.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class OnReadySourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider =
            context.CompilationProvider.Combine(
                context.SyntaxProvider.CreateSyntaxProvider(
                        (node, _) => CheckClassDeclaration(node),
                        static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
                    ).Collect()
                    .Combine(context.SyntaxProvider.ForAttributeWithMetadataName(
                        typeof(OnReadyAttribute).FullName!,
                        static (node, _) =>
                        {
                            return node is VariableDeclaratorSyntax
                                   || node is PropertyDeclarationSyntax
                                   || node is MethodDeclarationSyntax;
                        },
                        static (context, _) => context
                    ).Collect())
                )
            .Combine(
                context.SyntaxProvider.CreateSyntaxProvider(
                        (node, _) => node is EventDeclarationSyntax || node is EventFieldDeclarationSyntax,
                        static (ctx, _) => (MemberDeclarationSyntax)ctx.Node
                    ).Collect()
                );

        context.RegisterSourceOutput(syntaxProvider, Execute);
    }

    private void Execute(SourceProductionContext spc,
        ((Compilation Left, (ImmutableArray<ClassDeclarationSyntax> Left, ImmutableArray<GeneratorAttributeSyntaxContext> Right) Right) Left, ImmutableArray<MemberDeclarationSyntax> Right) tuple)
    {
        var compilation = tuple.Left.Left;
        var classNodes = tuple.Left.Right.Left;
        var onReadyMembers = tuple.Left.Right.Right;
        var eventNodes = tuple.Right;
        Dictionary<(string, string), OnReadyModel> dict = new();

        foreach (var classNode in classNodes)
        {
            var semanticModel = compilation.GetSemanticModel(classNode.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classNode);
            if (!IsInheritedFromGodotNode(classSymbol)) continue;

            var namespaceNode = classNode.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            string @namespace = namespaceNode == null ? string.Empty : namespaceNode.Name.ToString();
            string @class = classNode.Identifier.ValueText;

            if (!dict.TryGetValue((@namespace, @class), out OnReadyModel model))
            {
                model = new OnReadyModel()
                {
                    Namespace = @namespace,
                    ClassName = @class,
                    Modifiers = string.Join(" ", classNode.Modifiers.Select(v => v.ValueText))
                };
                dict[(@namespace, @class)] = model;
            }
        }

        foreach (var gasc in onReadyMembers)
        {
            var targetSymbol = gasc.TargetSymbol;
            var @namespace = gasc.TargetSymbol.ContainingNamespace.ToString();
            string @class = gasc.TargetSymbol.ContainingType.Name;

            if (!dict.TryGetValue((@namespace, @class), out OnReadyModel model)) continue;

            var attributeData = gasc.Attributes.First();
            var nodePath = (string)attributeData.ConstructorArguments[0].Value!;
            if (targetSymbol is IPropertySymbol
                {
                    GetMethod: not null,
                    SetMethod: not null,
                } propertySymbol)
            {
                if (!IsInheritedFromGodotNode(propertySymbol.Type)) continue;
                model.Properties.Add(new OnReadyModel.Property()
                {
                    Name = propertySymbol.Name,
                    NodePath = nodePath,
                    Type = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                });
            }
            else if (targetSymbol is IFieldSymbol fieldSymbol)
            {
                if (!IsInheritedFromGodotNode(fieldSymbol.Type)) continue;
                model.Fields.Add(new OnReadyModel.Field()
                {
                    Name = fieldSymbol.Name,
                    NodePath = nodePath,
                    Type = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                });
            }
        }

        foreach (var memberNode in eventNodes)
        {
            var classNode = memberNode.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classNode == null) continue;

            var namespaceNode = classNode.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            string @namespace = namespaceNode == null ? string.Empty : namespaceNode.Name.ToString();
            string @class = classNode.Identifier.ValueText;
            if (!dict.TryGetValue((@namespace, @class), out OnReadyModel model)) continue;

            var semanticModel = compilation.GetSemanticModel(classNode.SyntaxTree);
            ISymbol? symbol = null;
            if (memberNode is EventFieldDeclarationSyntax eventFieldNode)
            {
                symbol = semanticModel.GetDeclaredSymbol(eventFieldNode.Declaration.Variables[0]);
            }
            else if (memberNode is EventDeclarationSyntax eventNode)
            {
                symbol = semanticModel.GetDeclaredSymbol(eventNode);
            }
            if (symbol is IEventSymbol
                {
                    AddMethod: not null,
                    RemoveMethod: not null,
                } eventSymbol)
            {
                model.Events.Add(new OnReadyModel.Event()
                {
                    Name = eventSymbol.Name
                });
            }
        }

        foreach (KeyValuePair<(string, string), OnReadyModel> entry in dict)
        {
            var model = entry.Value;
            StringBuilder fieldOrPropertyStmts = new();
            List<OnReadyModel.IFieldOrProperty> fieldOrPropertyList =
                model.Fields.Concat(model.Properties.Cast<OnReadyModel.IFieldOrProperty>()).ToList();
            foreach (var item in fieldOrPropertyList)
            {
                var getNodeStmt = $"\t\t\t{item.Name} = GetNodeOrNull<{item.Type}>(\"{item.NodePath}\");";
                fieldOrPropertyStmts.AppendLine(getNodeStmt);
                var pushDictStmt = $"\t\t\tdict[\"{item.NodePath}\"] = {item.Name};";
                fieldOrPropertyStmts.AppendLine(pushDictStmt);
            }

            StringBuilder eventStmts = new();
            foreach (var item in model.Events)
            {
                var eventNameStmt = $"\t\t\tpublic const string {item.Name} = \"{item.Name}\";";
                eventStmts.AppendLine(eventNameStmt);
            }
            string? filePrefix = string.IsNullOrEmpty(model.Namespace) ? string.Empty : $"{model.Namespace}.";
            spc.AddSource($"{filePrefix}{model.ClassName}_OnReady.generated.cs",
                $@"// <auto-generated/>
using System;

namespace {model.Namespace}
{{
    {model.Modifiers} class {model.ClassName}
    {{
#pragma warning disable CS8321
        private void _OnReady()
        {{
            System.Collections.Generic.Dictionary<string, object> dict = new({fieldOrPropertyList.Count});
            void Fn<T>(string key, Action<T> action) where T : class
            {{
                object o = dict[key];
                o ??= GetNodeOrNull<T>(key);
                if (o is T n) action.Invoke(n);
            }}
{fieldOrPropertyStmts.ToString().TrimEnd('\n')}
        }}
#pragma warning disable CS8321

#pragma warning disable CS0109
        public new class EventName
        {{
{eventStmts.ToString().TrimEnd('\n')}
        }}
#pragma warning restore CS0109
    }}
}}
");
        }
    }

    private bool CheckClassDeclaration(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax
            {
                Modifiers: var classModifiers and not [],
                BaseList.Types: not []
            })
        {
            return false;
        }

        if (!classModifiers.Any(SyntaxKind.PartialKeyword)) return false;
        if (classModifiers.Any(SyntaxKind.StaticKeyword)) return false;

        return true;
    }

    private bool IsInheritedFromGodotNode(ISymbol? symbol)
    {
        while (symbol != null)
        {
            if (symbol is ITypeSymbol typeSymbol && typeSymbol.TypeKind == TypeKind.Class)
            {
                if (typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::Godot.")
                    && typeSymbol.GetMembers().OfType<IMethodSymbol>().Any(v =>
                    {
                        if (v is
                            {
                                Name: "GetNodeOrNull",
                                ReturnsVoid: false,
                                Parameters.Length: 1
                            } methodSymbol
                            && methodSymbol.Parameters[0].Type
                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Godot.NodePath")
                        {
                            return true;
                        }

                        return false;
                    })) return true;
                symbol = typeSymbol.BaseType;
            }
            else
                break;
        }

        return false;
    }
}