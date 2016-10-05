using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace NestedSynchronizedMethodCalss
{
    internal class HierarchieChecker
    {
        readonly List<string> _inheritanceClasses = new List<string>();

        public HierarchieChecker(ITypeSymbol type)
        {
            var baseTypes = type.GetBaseTypesAndThis();
            var interfaces = type.AllInterfaces;
            foreach (var interfacee in interfaces)
            {
                _inheritanceClasses.Add(interfacee.Name);
            }
            foreach (var baseType in baseTypes)
            {
                _inheritanceClasses.Add(baseType.Name);
            }
        }

        public bool IsSubClass(ITypeSymbol baseType)
        {
            return _inheritanceClasses.Contains(baseType.Name);
        }
    }
}
