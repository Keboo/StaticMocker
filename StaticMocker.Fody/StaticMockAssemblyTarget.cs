using System;

namespace StaticMocker.Fody
{
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )]
    public class StaticMockAssemblyTargetAttribute : Attribute
    {
        public string AssemblyName { get; set; }

        public StaticMockAssemblyTargetAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }
    }
}