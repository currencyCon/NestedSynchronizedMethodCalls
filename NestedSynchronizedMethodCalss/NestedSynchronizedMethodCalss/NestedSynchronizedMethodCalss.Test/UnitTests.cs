using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using NestedSynchronizedMethodCalss;

namespace NestedSynchronizedMethodCalss.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {



        [TestMethod]
        public void AnalyzerFindsSimpleCase()
        {
            var test = @"
    class BankAccount
    {
        private int balance;
        public void Deposit(int amount)
        {
            lock (this) { balance += amount; }
        }
        public void Transfer(BankAccount target, int amount)
        {
            lock (this)
            {
                balance -= amount;
                target.Deposit(amount); // lock (target)
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = NestedSynchronizedMethodCalssAnalyzer.NestedLockingDiagnosticId,
                Message = "Possible Deadlock with double Locking",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 14, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

  }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new NestedSynchronizedMethodCalssCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NestedSynchronizedMethodCalssAnalyzer();
        }
    }
}