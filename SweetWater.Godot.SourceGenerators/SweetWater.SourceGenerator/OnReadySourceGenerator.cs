using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SweetWater.Godot.SourceGenerator;

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
                                   || node is PropertyDeclarationSyntax;
                        },
                        static (context, _) => context
                    ).Collect())
                )
            .Combine(
                context.SyntaxProvider.ForAttributeWithMetadataName(
                        typeof(OnEventAttribute).FullName!,
                        (node, _) => node is MethodDeclarationSyntax,
                        static (context, _) => context
                    ).Collect()
                );

        context.RegisterSourceOutput(syntaxProvider, Execute);
    }

    private void Execute(SourceProductionContext spc,
        ((Compilation Left, (ImmutableArray<ClassDeclarationSyntax> Left, ImmutableArray<GeneratorAttributeSyntaxContext> Right) Right) Left, ImmutableArray<GeneratorAttributeSyntaxContext> Right) tuple)
    {
        var compilation = tuple.Left.Left;
        var classNodes = tuple.Left.Right.Left;
        var onReadyMembers = tuple.Left.Right.Right;
        var onEventMethods = tuple.Right;
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

        foreach (var gasc in onEventMethods)
        {
            var targetSymbol = (IMethodSymbol)gasc.TargetSymbol;
            var @namespace = gasc.TargetSymbol.ContainingNamespace.ToString();
            string @class = gasc.TargetSymbol.ContainingType.Name;

            if (!dict.TryGetValue((@namespace, @class), out OnReadyModel model)) continue;

            var onEventMethod = new OnReadyModel.OnEventMethod()
            {
                Name = targetSymbol.Name,
            };

            var attributeDatas = gasc.Attributes;
            foreach (var item in attributeDatas)
            {
                var eventName = (string)item.ConstructorArguments[0].Value!;
                var nodePath = (string)item.ConstructorArguments[1].Value!;
                var nodeType = (INamedTypeSymbol)item.ConstructorArguments[2].Value!;
                Console.WriteLine($"{eventName} {nodePath} {nodeType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                onEventMethod.OnEventList.Add(new OnReadyModel.OnEvent()
                {
                    EventName = eventName,
                    NodePath = string.IsNullOrEmpty(nodePath) ? "." : nodePath,
                    NodeType = nodeType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? @class,
                });
            }
            model.OnEventMethods.Add(onEventMethod);
        }
        // GetNodeOrNull<{{item.NodeType}}>("{{item.NodePath}}").{{item.EventName}} += {{method.Name}};
        foreach (KeyValuePair<(string, string), OnReadyModel> entry in dict)
        {
            var model = entry.Value;
            List<OnReadyModel.IFieldOrProperty> fieldOrPropertyList =
                model.Fields.Concat(model.Properties.Cast<OnReadyModel.IFieldOrProperty>()).ToList();

            string? filePrefix = string.IsNullOrEmpty(model.Namespace) ? string.Empty : $"{model.Namespace}.";
            var tpl = Template.Parse("""
                // <auto-generated/>
                using System;
                
                namespace {{Namespace}}
                {
                    {{Modifiers}} class {{ClassName}}
                    {
                #pragma warning disable CS8321
                        private void _OnReady()
                        {
                            System.Collections.Generic.Dictionary<string, object> dict = new();
                            void FromCache<T>(string key, Action<T> action) where T : class
                            {
                                if (!dict.TryGetValue(key, out object o)) o = GetNodeOrNull<T>(key);
                                if (o is T n)
                                {
                                    dict[key] = n;
                                    action.Invoke(n);
                                }
                            }
                {{for item in Fields}}
                            {{item.Name}} = GetNodeOrNull<{{item.Type}}>("{{item.NodePath}}");
                            dict["{{item.NodePath}}"] = {{item.Name}};
                {{end~}}
                {{for item in Properties}}
                            {{item.Name}} = GetNodeOrNull<{{item.Type}}>("{{item.NodePath}}");
                            dict["{{item.NodePath}}"] = {{item.Name}};
                {{end~}}
                {{for method in OnEventMethods-}}
                {{for item in method.OnEventList}}
                            FromCache<{{item.NodeType}}>("{{item.NodePath}}", v => v.{{item.EventName}} += {{method.Name}});
                {{end-}}
                {{end~}}
                        }
                #pragma warning disable CS8321
                    }
                }
                """);
            var source = tpl.Render(model, member => member.Name);
            Console.WriteLine(source);
            spc.AddSource($"{filePrefix}{model.ClassName}_OnReady.generated.cs", source);
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