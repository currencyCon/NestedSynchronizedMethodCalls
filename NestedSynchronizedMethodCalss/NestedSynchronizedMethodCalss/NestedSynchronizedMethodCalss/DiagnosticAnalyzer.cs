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

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Synchronization";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(NestedLockingDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
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

            var parametersOfOwnKind = ParametersOfOwnType(method, context);
            if (!parametersOfOwnKind.Any())
            {
                return;
            }
            foreach (var lockStatementSyntax in lockStatements)
            {
                var lockObject = lockStatementSyntax.Expression;
                var memberAccessExpression =
                    lockStatementSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();
                
                List<MemberAccessExpressionSyntax> methodsToCheck = FindMethodsToCheck(parametersOfOwnKind,
                    memberAccessExpression);

                foreach (var memberAccessExpressionSyntax in methodsToCheck)
                {
                    if (CheckIfAquiresSameLock(lockObject, memberAccessExpressionSyntax.Name, root))
                    {
                        var diagn = Diagnostic.Create(Rule, memberAccessExpressionSyntax.GetLocation());
                        context.ReportDiagnostic(diagn);
                    }
                }
            }
            
        }

        private static List<MemberAccessExpressionSyntax> FindMethodsToCheck(List<SyntaxToken> parametersOfOwnKind, List<MemberAccessExpressionSyntax> memberAccessExpression)
        {
            var methodList = new List<MemberAccessExpressionSyntax>();
            foreach (var parameter in parametersOfOwnKind)
            {
                foreach (var memberAccessExpressionSyntax in memberAccessExpression)
                {
                    if (memberAccessExpressionSyntax.Expression.ToString() == parameter.ToString())
                    {
                        methodList.Add(memberAccessExpressionSyntax);
                    }
                }
            }
            return methodList;
        }

        private static bool CheckIfAquiresSameLock(ExpressionSyntax lockObject, SimpleNameSyntax methodName, SyntaxNode root)
        {
            var clazz = root.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var calledMethod =
                clazz
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>().FirstOrDefault(e => e.Identifier.Text == methodName.ToString());
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

        private static List<SyntaxToken> ParametersOfOwnType(BaseMethodDeclarationSyntax node, SyntaxNodeAnalysisContext context)
        {
            var clazz = GetClass(node);
            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(clazz);
            var parametersOfOwnType = new List<SyntaxToken>();
            HierarchieChecker hierarchieChecker = new HierarchieChecker(classTypeSymbol);
            
            foreach (var parameterSyntax in node.ParameterList.Parameters)
            {
                var baseTypeSymbol = context.SemanticModel.GetDeclaredSymbol(parameterSyntax).Type;
                if (hierarchieChecker.IsSubClass(baseTypeSymbol))
                {
                    parametersOfOwnType.Add(parameterSyntax.Identifier);
                }
            }
            return parametersOfOwnType;
        }

        private static ClassDeclarationSyntax GetClass(SyntaxNode method)
        {
            var classDeclarations = method.AncestorsAndSelf().OfType<ClassDeclarationSyntax>();
            var classDeclaration = classDeclarations.FirstOrDefault();
            return classDeclaration;
        }

        private static string GetTypeOfClass(SyntaxNode method)
        {
            var classDeclarations = method.AncestorsAndSelf().OfType<ClassDeclarationSyntax>();
            var classDeclaration = classDeclarations.FirstOrDefault();
            return classDeclaration?.Identifier.ToString();
        }
    }
}
