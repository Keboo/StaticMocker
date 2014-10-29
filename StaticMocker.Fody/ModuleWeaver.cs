using Mono.Cecil;
using StaticMocker.Fody;
using System;
using System.Linq;

public class ModuleWeaver
{
    public Action<string> LogInfo { get; set; }
    public Action<string> LogError { get; set; }
    public ModuleDefinition ModuleDefinition { get; set; }
    public IAssemblyResolver AssemblyResolver { get; set; }
    public string[] DefineConstants { get; set; }

    public ModuleWeaver()
    {
        LogInfo = s => { };
        LogError = s => { };
        DefineConstants = new string[0];
    }

    public void Execute()
    {
        var targetAssemblyAttributes =
            ModuleDefinition.Assembly.CustomAttributes.Where(
                x => x.AttributeType.FullName == typeof( StaticMockAssemblyTargetAttribute ).FullName );
        foreach ( var attribute in targetAssemblyAttributes )
        {
            var assembly = attribute.ConstructorArguments.Single();
            AssemblyDefinition assemblyDefinition = ModuleDefinition.AssemblyResolver.Resolve( (string)assembly.Value );
            if ( assemblyDefinition == null )
            {
                LogError( string.Format( "Failed to find assembly '{0}' to mock static calls", assembly.Value ) );
                return;
            }
            var assemblyPath = assemblyDefinition.MainModule.FullyQualifiedName;
            LogInfo( string.Format( "Intercepting static calls in assembly '{0}' ({1})", assembly.Value, assemblyPath ) );

            StaticMockWeaver.InterceptStatics( assemblyPath, ModuleDefinition.AssemblyResolver, LogError );
        }
    }
}