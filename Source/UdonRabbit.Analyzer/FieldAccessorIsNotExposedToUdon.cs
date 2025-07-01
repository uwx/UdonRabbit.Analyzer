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
    public class FieldAccessorIsNotExposedToUdon : DiagnosticAnalyzer
    {
        public const string ComponentId = "URA0002";
        private const string Category = UdonConstants.UdonCategory;
        private const string HelpLinkUri = "https://github.com/uwx/UdonRabbit.Analyzer/blob/master/docs/analyzers/URA0002.md";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.URA0002Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.URA0002MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.URA0002Description), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor RuleSet = new(ComponentId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleSet);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(UdonRabbitLogger.Catching(AnalyzeMemberAccess), SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax) context.Node;

            if (!UdonSharpBehaviourUtility.ShouldAnalyzeSyntax(context.SemanticModel, memberAccess))
                return;

            if (!UdonAssemblyLoader.IsAssemblyLoaded)
                UdonAssemblyLoader.LoadUdonAssemblies(context.Compilation.ExternalReferences);

            if (UdonSymbols.Instance == null)
                UdonSymbols.Initialize();

            var isAssignment = memberAccess.Parent is AssignmentExpressionSyntax assignment && assignment.Right != memberAccess;

            var t = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
            var fieldSymbol = context.SemanticModel.GetSymbolInfo(memberAccess);
            
            UdonRabbitLogger.Log($"Analyzing Member Access");
            UdonRabbitLogger.Log($"Type: {t.Type.ToDisplayString()}");
            UdonRabbitLogger.Log($"Field Symbol: {fieldSymbol.Symbol.ToDisplayString()}");
            
            switch (fieldSymbol.Symbol)
            {
                case IFieldSymbol field:
                {
                    if (field.Name == "Length" && t.Type is IArrayTypeSymbol)
                        return;
                    
                    if (UdonSymbols.Instance?.FindUdonVariableName(context.SemanticModel, t.Type, field, isAssignment) == false)
                        UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, memberAccess, field.Name);
                    return;
                }

                case IPropertySymbol props:
                {
                    if (props.Name == "Length" && t.Type is IArrayTypeSymbol)
                        return;

                    if (UdonSymbols.Instance?.FindUdonVariableName(context.SemanticModel, t.Type, props, isAssignment) == false)
                        UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, memberAccess, props.Name);
                    return;
                }
            }
        }
    }
}