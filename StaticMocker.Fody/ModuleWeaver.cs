using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using Microsoft.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using StaticMocker.Fody;
using System;
using System.Linq;

public class ModuleWeaver
{
    private const string INTERCEPT_METHOD = "Intercept";
    private const string PARAM_OUT_METHOD = "Out";
    private const string PARAM_IN_METHOD = "In";
    private const string GET_OUT_VARIABLE_METHOD = "GetOutValue";
    private const string RETURN_VALUE_PROPERTY = "ReturnValue";

    private TypeDefinition _InterceptorType;
    private string _StaticMockerAssemblyPath;
    private CodeCompileUnit _CompileUnit;


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

            InterceptStatics( assemblyDefinition, ModuleDefinition.AssemblyResolver );
        }
    }

    private void InterceptStatics( AssemblyDefinition assemblyToMock, IAssemblyResolver assemblyResolver )
    {
        var interceptorAssembly = assemblyResolver.Resolve( "StaticMocker.Fody" );
        _StaticMockerAssemblyPath = interceptorAssembly.MainModule.FullyQualifiedName;

        _InterceptorType = interceptorAssembly.MainModule.GetType( typeof( Interceptor ).FullName );

        foreach ( TypeDefinition type in assemblyToMock.MainModule.GetTypes() )
        {
            foreach ( MethodDefinition method in type.GetMethods() )
            {
                CreateMockStaticCalls( method );
            }
        }

        if ( _CompileUnit != null )
        {
            string assemblyPath = assemblyToMock.MainModule.FullyQualifiedName;
            string staticMocksLib = Path.GetFullPath( Path.Combine( @".\bin\Debug",
                string.Format("{0}.StaticMocks{1}", Path.GetFileName(assemblyPath), Path.GetExtension(assemblyPath))));
            new FileInfo( staticMocksLib ).Delete();

            _CompileUnit.ReferencedAssemblies.Add( assemblyPath );

            var provider = new CSharpCodeProvider();

            //TODO: Change this to use DefineConstant
#if DEBUG
            // Create a TextWriter to a StreamWriter to the output file.
            using ( var sw = new StreamWriter( @"C:\Temp\Output.cs", false ) )
            using ( var tw = new IndentedTextWriter( sw, "    " ) )
            {
                provider.GenerateCodeFromCompileUnit( _CompileUnit, tw, new CodeGeneratorOptions() );

                tw.Close();
            }
#endif

            var compileParameters = new CompilerParameters();
            compileParameters.OutputAssembly = staticMocksLib;

            CompilerResults compileResults = provider.CompileAssemblyFromDom( compileParameters, _CompileUnit );
            if ( compileResults.Errors.HasErrors )
            {
                throw new StaticMockCompileException( string.Join( Environment.NewLine,
                    compileResults.Errors.Cast<CompilerError>().Select( x => x.ErrorText ) ) );
            }
            var staticMockAssembly = AssemblyDefinition.ReadAssembly( staticMocksLib );
            assemblyToMock.MainModule.AssemblyReferences.Add( staticMockAssembly.Name );

            foreach ( TypeDefinition type in assemblyToMock.MainModule.GetTypes() )
            {
                foreach ( MethodDefinition method in type.GetMethods() )
                {
                    ReplaceStaticCalls( method, staticMockAssembly );
                }
            }
            assemblyToMock.Write( assemblyPath );
        }
    }

    private void ReplaceStaticCalls( MethodDefinition method, AssemblyDefinition staticMockAssembly )
    {
        var body = method.Body;

        for ( int i = 0; i < body.Instructions.Count; i++ )
        {
            var call = body.Instructions[i];
            if ( call.OpCode == OpCodes.Call )
            {
                var originalMethodReference = (MethodReference)call.Operand;
                var originalMethodDefinition = originalMethodReference.Resolve();

                if ( originalMethodDefinition.IsStatic &&
                    originalMethodDefinition.DeclaringType.FullName != _InterceptorType.FullName )
                {
                    var mockNamespace = GetMockNamespace( originalMethodDefinition.DeclaringType.Namespace );
                    var mockTypeName = GetMockTypeName( originalMethodDefinition.DeclaringType.Name );
                    var mockType = staticMockAssembly.MainModule.GetType( mockNamespace, mockTypeName );
                    var mockMethod = mockType.GetMethods().Single( x => x.Name == originalMethodDefinition.Name );
                    //Method name
                    call.Operand = method.Module.Import( mockMethod );
                }
            }
        }
    }

    private void CreateMockStaticCalls( MethodDefinition method )
    {
        if ( method.HasBody == false ) return;
        var body = method.Body;

        for ( int i = 0; i < body.Instructions.Count; i++ )
        {
            var call = body.Instructions[i];
            if ( call.OpCode == OpCodes.Call )
            {
                var originalMethodReference = (MethodReference)call.Operand;
                var originalMethodDefinition = originalMethodReference.Resolve();

                if ( originalMethodDefinition != null && originalMethodDefinition.IsStatic &&
                    originalMethodDefinition.DeclaringType.FullName != _InterceptorType.FullName )
                {
                    CreateStaticMockMethod( originalMethodDefinition );
                }
            }
        }
    }

    private void CreateStaticMockMethod( MethodDefinition method )
    {
        if ( _CompileUnit == null )
        {
            _CompileUnit = new CodeCompileUnit();
            _CompileUnit.ReferencedAssemblies.Add( _StaticMockerAssemblyPath );
        }

        var mockNamespace = GetMockNamespace( method.DeclaringType.Namespace );
        var @namespace = _CompileUnit.Namespaces.Cast<CodeNamespace>()
                .FirstOrDefault( x => x.Name == mockNamespace );
        if ( @namespace == null )
        {
            @namespace = new CodeNamespace( mockNamespace );
            _CompileUnit.Namespaces.Add( @namespace );
        }

        var mockTypeName = GetMockTypeName( method.DeclaringType.Name );
        var mockType = @namespace.Types.Cast<CodeTypeDeclaration>().FirstOrDefault( x => x.Name == mockTypeName );
        if ( mockType == null )
        {
            mockType = new CodeTypeDeclaration( mockTypeName );
            @namespace.Types.Add( mockType );
        }

        var codeMethod = mockType.Members.OfType<CodeMemberMethod>().FirstOrDefault( x => x.Name == method.Name );
        if ( codeMethod == null )
        {
            codeMethod = new CodeMemberMethod();
            codeMethod.Attributes = MemberAttributes.Static | MemberAttributes.Public;
            codeMethod.Name = method.Name;
            codeMethod.ReturnType = new CodeTypeReference( method.ReturnType.FullName );

            var mockMethodParameters = new List<CodeExpression>
                {
                    new CodePrimitiveExpression(  method.DeclaringType.Module.Assembly.FullName ),
                    new CodePrimitiveExpression(  method.DeclaringType.FullName ),
                    new CodePrimitiveExpression(  method.Name )
                };
            foreach ( var parameter in method.Parameters )
            {
                var codeParameter = new CodeParameterDeclarationExpression( parameter.ParameterType.FullName,
                    parameter.Name );
                if ( parameter.IsOut )
                {
                    codeParameter.Type = new CodeTypeReference( parameter.ParameterType.FullName.TrimEnd( '&' ) );
                    codeParameter.Direction = FieldDirection.Out;
                    var outMethod = new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( Param ) ),
                        PARAM_OUT_METHOD, new CodePrimitiveExpression( parameter.Name ) );
                    outMethod.Method.TypeArguments.Add( codeParameter.Type );
                    mockMethodParameters.Add( outMethod );
                }
                else
                {
                    mockMethodParameters.Add( new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( Param ) ),
                        PARAM_IN_METHOD,
                        new CodePrimitiveExpression( parameter.Name ),
                        new CodeVariableReferenceExpression( parameter.Name ) ) );
                }
                codeMethod.Parameters.Add( codeParameter );
            }


            var mockMethod = new CodeObjectCreateExpression( typeof( MockMethod ), mockMethodParameters.ToArray() );
            var mockMethodVariable = new CodeVariableDeclarationStatement( typeof( MockMethod ), "mockMethod", mockMethod );

            codeMethod.Statements.Add( mockMethodVariable );

            var interceptCall = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression( _InterceptorType.FullName ),
                INTERCEPT_METHOD, new CodeVariableReferenceExpression( mockMethodVariable.Name ) );

            var conditional = new CodeConditionStatement( interceptCall );

            CodeExpression originalCall;
            if ( method.IsGetter )
            {
                var methodName = method.Name;
                if ( method.Name.StartsWith( "get_" ) )
                {
                    methodName = methodName.Substring( 4 );
                }
                originalCall = new CodePropertyReferenceExpression(
                    new CodeTypeReferenceExpression( method.DeclaringType.FullName ), methodName );
            }
            else
            {
                var callMethod = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression( method.DeclaringType.FullName ), method.Name );

                foreach ( var parameter in codeMethod.Parameters.Cast<CodeParameterDeclarationExpression>() )
                {
                    var variable = new CodeDirectionExpression( parameter.Direction, new CodeVariableReferenceExpression( parameter.Name ) );
                    callMethod.Parameters.Add( variable );

                    switch ( parameter.Direction )
                    {
                        case FieldDirection.Out:
                            var getOutValue = new CodeMethodInvokeExpression(
                                new CodeVariableReferenceExpression( mockMethodVariable.Name ),
                                GET_OUT_VARIABLE_METHOD,
                                new CodePrimitiveExpression( parameter.Name ) );
                            getOutValue.Method.TypeArguments.Add( parameter.Type );
                            conditional.TrueStatements.Add( new CodeAssignStatement(
                                new CodeVariableReferenceExpression( parameter.Name ), getOutValue ) );
                            break;
                    }
                }
                originalCall = callMethod;
            }

            if ( method.ReturnType.FullName == typeof( void ).FullName )
            {
                conditional.FalseStatements.Add( originalCall );
            }
            else
            {
                conditional.FalseStatements.Add( new CodeMethodReturnStatement( originalCall ) );
                var propertyGetter = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression( mockMethodVariable.Name ), RETURN_VALUE_PROPERTY );
                var caseExpression = new CodeCastExpression( method.ReturnType.FullName, propertyGetter );
                conditional.TrueStatements.Add( new CodeMethodReturnStatement( caseExpression ) );
            }

            codeMethod.Statements.Add( conditional );
            mockType.Members.Add( codeMethod );
        }
    }

    private static string GetMockNamespace( string @namespace )
    {
        return "StaticMock" + @namespace;
    }

    private static string GetMockTypeName( string typeName )
    {
        return "Mock" + typeName;
    }
}