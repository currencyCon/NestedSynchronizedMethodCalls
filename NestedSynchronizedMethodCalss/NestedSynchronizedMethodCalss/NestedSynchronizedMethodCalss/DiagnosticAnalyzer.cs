using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NestedSynchronizedMethodCalss
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NestedSynchronizedMethodCalssAnalyzer : DiagnosticAnalyzer
    {
        public static string NestedLockingDiagnosticId = "NSMC001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Synchronization";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(NestedLockingDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            //context.RegisterSyntaxNodeAction(, SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var root = context.Node;
            if (!(root is MethodDeclarationSyntax))
            {
                return;
            }
            var method = (MethodDeclarationSyntax) root;
            var lockStatements = GetLockStatements(method).ToList();
            if (!lockStatements.Any())
            {
                return;
            }

            var parametersOfOwnKind = ParametersOfOwnType(method);
            if (!parametersOfOwnKind.Any())
            {
                return;
            }
            foreach (var lockStatementSyntax in lockStatements)
            {
                var lockObject = lockStatementSyntax.Expression;
                var memberAccessExpression =
                    lockStatementSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
                foreach (var memberAccessExpressionSyntax in memberAccessExpression)
                {
                    foreach (var parameter in parametersOfOwnKind)
                    {
                        if (memberAccessExpressionSyntax.Expression.ToString() == parameter.ToString())
                        {
                            if (CheckIfAquiresSameLock(lockObject, memberAccessExpressionSyntax.Name, method))
                            {
                                var diagn = Diagnostic.Create(Rule, memberAccessExpressionSyntax.GetLocation());
                                context.ReportDiagnostic(diagn);
                            }
                        }
                    }
                }
            }



        }

        private static bool CheckIfAquiresSameLock(ExpressionSyntax lockObject, SimpleNameSyntax name, MethodDeclarationSyntax method)
        {
            var clazz = method.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var calledMethod =
                clazz
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>().FirstOrDefault(e => e.Identifier.Text == name.ToString());
            var lockStatements = GetLockStatements(calledMethod);
            foreach (var lockStatementSyntax in lockStatements)
            {
                if (lockStatementSyntax.Expression.ToString() == lockObject.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<LockStatementSyntax> GetLockStatements(SyntaxNode node)
        {
            return node.DescendantNodesAndSelf().OfType<LockStatementSyntax>();
        }

        private static List<SyntaxToken> ParametersOfOwnType(BaseMethodDeclarationSyntax node)
        {
            var classDecl = GetTypeOfClass(node);
            if (classDecl == null)
            {
                return null;
            }
            var parametersOfOwnType = new List<SyntaxToken>();
            foreach (var parameterSyntax in node.ParameterList.Parameters)
            {
                if (parameterSyntax.Type.ToString() == classDecl)
                {
                    parametersOfOwnType.Add(parameterSyntax.Identifier);
                }
            }
            return parametersOfOwnType;
        }

        private static string GetTypeOfClass(SyntaxNode method)
        {
            var classDeclarations = method.AncestorsAndSelf().OfType<ClassDeclarationSyntax>();
            var classDeclaration = classDeclarations.FirstOrDefault();
            return classDeclaration?.Identifier.ToString();
        }
    }
}
