using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using UdonRabbit.Analyzer.Udon;
using UdonRabbit.Analyzer.Utils;

namespace UdonRabbit.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotSupportInheritingFromInterfaces : DiagnosticAnalyzer
    {
        public const string ComponentId = "URA0005";
        private const string Category = UdonConstants.UdonSharpCategory;
        private const string HelpLinkUri = "https://github.com/uwx/UdonRabbit.Analyzer/blob/master/docs/analyzers/URA0005.md";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.URA0005Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.URA0005MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.URA0005Description), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor RuleSet = new(ComponentId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleSet);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (ClassDeclarationSyntax) context.Node;
            if (!UdonSharpBehaviourUtility.ShouldAnalyzeSyntax(context.SemanticModel, declaration))
                return;

            var declSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);
            var interfaces = declSymbol.AllInterfaces;
            if (interfaces.Length == 0 ||
                interfaces.All(w =>
                    w.Equals(context.SemanticModel.Compilation.GetTypeByMetadataName("UnityEngine.ISerializationCallbackReceiver"), SymbolEqualityComparer.Default) ||
                    w.ToDisplayString() == "VRC.Udon.Serialization.OdinSerializer.ISupportsPrefabSerialization"))
                return;

            UdonRabbitLogger.Log($"{declSymbol.ToDisplayString()} interfaces: {string.Join(", ", interfaces.Select(w => w.ToDisplayString()))}");

            UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, declaration, interfaces.Select(w => w.ToDisplayString()).First());
        }
    }
}