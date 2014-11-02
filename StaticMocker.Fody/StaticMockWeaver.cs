using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace StaticMocker.Fody
{
    public static class StaticMockWeaver
    {
        private static TypeDefinition _InterceptorType;
        private static MethodReference _InterceptMethod;
        private static MethodReference _ReturnValueInterceptMethod;

        public static void InterceptStatics( string assemblyPathToMock, IAssemblyResolver assemblyResolver )
        {
            //string originalBackup = assemblyPathToMock.Replace( ".dll", ".Original.dll" );
            //File.Delete( originalBackup );
            //File.Copy( assemblyPathToMock, originalBackup );

            var interceptorAssembly = assemblyResolver.Resolve( "StaticMocker.Fody" );

            _InterceptorType = interceptorAssembly.MainModule.GetType( typeof( Interceptor ).FullName );

            var assemblyDefinition = AssemblyDefinition.ReadAssembly( assemblyPathToMock );
            //assemblyDefinition.Write( assemblyPathToMock );
            var moduleDefinition = assemblyDefinition.MainModule;

            _InterceptMethod = moduleDefinition.Import( _InterceptorType.GetMethods().Single( x =>
            {
                return x.Name == "Intercept" &&
                       x.CallingConvention == MethodCallingConvention.Default &&
                       x.HasParameters &&
                       x.Parameters.Count == 1;
            } ) );

            foreach ( TypeDefinition type in assemblyDefinition.MainModule.GetTypes() )
            {
                if ( type.IsClass && type.Name == "Sut" )
                {
                    foreach ( MethodDefinition method in type.GetMethods() )
                    {
                        InterceptStaticCalls( method );
                    }
                }
            }

            if ( _CompileUnit != null )
            {
                string staticMocksLib = Path.GetFullPath( ".\\bin\\Debug\\" + Path.GetFileName( assemblyPathToMock.Replace( ".dll", ".StaticMocks.dll" ) ) );
                //string staticMocksLib = Path.GetFullPath( Path.GetFileName( assemblyPathToMock.Replace( ".dll", ".StaticMocks.dll" ) ) );
                new FileInfo( staticMocksLib ).Delete();

                _CompileUnit.ReferencedAssemblies.Add( assemblyPathToMock );

                var provider = new CSharpCodeProvider();

                // Create a TextWriter to a StreamWriter to the output file. 
                using ( var sw = new StreamWriter( @"C:\Temp\Output.cs", false ) )
                using ( var tw = new IndentedTextWriter( sw, "    " ) )
                {
                    provider.GenerateCodeFromCompileUnit( _CompileUnit, tw, new CodeGeneratorOptions() );

                    tw.Close();
                }

                var compileParameters = new CompilerParameters();
                compileParameters.OutputAssembly = Path.GetFullPath( staticMocksLib );

                CompilerResults compileResults = provider.CompileAssemblyFromDom( compileParameters, _CompileUnit );
                if ( compileResults.Errors.HasErrors )
                {
                    //throw new Exception( Path.GetFullPath( staticMocksLib ) );
                    throw new Exception( string.Join( Environment.NewLine, compileResults.Errors.Cast<CompilerError>().Select( x => x.ErrorText ) ) );
                }
                var staticMockAssembly = AssemblyDefinition.ReadAssembly( staticMocksLib );
                assemblyDefinition.MainModule.AssemblyReferences.Add( staticMockAssembly.Name );


                foreach ( TypeDefinition type in assemblyDefinition.MainModule.GetTypes() )
                {
                    if ( type.IsClass && type.Name == "Sut" )
                    {
                        foreach ( MethodDefinition method in type.GetMethods() )
                        {
                            InterceptStaticCalls( method, staticMockAssembly );
                        }
                    }
                }
            }



            assemblyDefinition.Write( assemblyPathToMock );
        }

        private static void InterceptStaticCalls( MethodDefinition method, AssemblyDefinition staticMockAssembly )
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
                        var mockNamespace = "StaticMock" + originalMethodDefinition.DeclaringType.Namespace;
                        var mockTypeName = "Mock" + originalMethodDefinition.DeclaringType.Name;
                        var mockType = staticMockAssembly.MainModule.GetType( mockNamespace, mockTypeName );
                        var mockMethod = mockType.GetMethods().Single( x => x.Name == originalMethodDefinition.Name );
                        //Method name
                        call.Operand = method.Module.Import( mockMethod );
                    }
                }
            }
        }

        private static void InterceptStaticCalls( MethodDefinition method )
        {
            if ( method.HasBody == false ) return;
            var body = method.Body;
            //body.SimplifyMacros();

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
                        MockStaticMethod( originalMethodDefinition );
                        //body.Instructions.RemoveAt( i-- );
                        //foreach ( var instruction in GetInstructions( originalMethodDefinition, body ) )
                        //{
                        //    body.Instructions.Insert( i++, instruction );
                        //}

                        //body.Instructions.Insert( i++, Instruction.Create( OpCodes.Brtrue_S, call.Next ) );
                    }
                }
            }
            //for ( int i = 0; i < body.Instructions.Count; i++ )
            //{
            //    if ( i > 0 )
            //    {
            //        body.Instructions[i].Previous = body.Instructions[i - 1];
            //    }
            //    if ( i < body.Instructions.Count - 1 )
            //    {
            //        body.Instructions[i].Next = body.Instructions[i + 1];
            //    }
            //}

            //body.InitLocals = true;
            //body.OptimizeMacros();
        }

        private static CodeCompileUnit _CompileUnit;
        private static void MockStaticMethod( MethodDefinition method )
        {
            if ( _CompileUnit == null )
            {
                _CompileUnit = new CodeCompileUnit();
                //TODO: Remove constant string
                _CompileUnit.ReferencedAssemblies.Add( @"C:\Dev\StaticMocker\packages\StaticMocker.Fody.1.0.0\StaticMocker.Fody.dll" );
            }

            var mockNamespace = "StaticMock" + method.DeclaringType.Namespace;
            var @namespace = _CompileUnit.Namespaces.Cast<CodeNamespace>()
                    .FirstOrDefault( x => x.Name == mockNamespace );
            if ( @namespace == null )
            {
                @namespace = new CodeNamespace( mockNamespace );
                _CompileUnit.Namespaces.Add( @namespace );
            }

            var mockTypeName = "Mock" + method.DeclaringType.Name;
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
                        var outMethod = new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( Param ) ), "Out",
                            new CodePrimitiveExpression( parameter.Name ) );
                        outMethod.Method.TypeArguments.Add( codeParameter.Type );
                        mockMethodParameters.Add( outMethod );
                    }
                    else
                    {
                        mockMethodParameters.Add( new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( typeof( Param ) ), "In",
                            new CodePrimitiveExpression( parameter.Name ),
                            new CodeVariableReferenceExpression( parameter.Name ) ) );
                    }
                    codeMethod.Parameters.Add( codeParameter );
                }


                var mockMethod = new CodeObjectCreateExpression( typeof( MockMethod ), mockMethodParameters.ToArray() );
                //TODO: Unique name issue?
                var mockMethodVariable = new CodeVariableDeclarationStatement( typeof( MockMethod ), "mockMethod", mockMethod );

                codeMethod.Statements.Add( mockMethodVariable );

                var interceptCall = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression( _InterceptorType.FullName ),
                    "Intercept", new CodeVariableReferenceExpression( mockMethodVariable.Name ) );

                //TODO set out/ref parameters

                var conditional = new CodeConditionStatement( interceptCall );

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
                                new CodeVariableReferenceExpression( mockMethodVariable.Name ), "GetOutValue",
                                new CodePrimitiveExpression( parameter.Name ) );
                            getOutValue.Method.TypeArguments.Add( parameter.Type );
                            conditional.TrueStatements.Add( new CodeAssignStatement( new CodeVariableReferenceExpression( parameter.Name ), getOutValue ) );
                            break;
                    }
                }

                if ( method.ReturnType.FullName == typeof( void ).FullName )
                {
                    conditional.FalseStatements.Add( callMethod );
                }
                else
                {
                    conditional.FalseStatements.Add( new CodeMethodReturnStatement( callMethod ) );
                    var propertyGetter = new CodePropertyReferenceExpression( new CodeVariableReferenceExpression( mockMethodVariable.Name ), "ReturnValue" );
                    var caseExpression = new CodeCastExpression( method.ReturnType.FullName, propertyGetter );
                    conditional.TrueStatements.Add( new CodeMethodReturnStatement( caseExpression ) );
                }


                codeMethod.Statements.Add( conditional );
                mockType.Members.Add( codeMethod );
            }
        }

        private static CodeNamespace GetNamespace( MethodDefinition method )
        {
            return new CodeNamespace( method.DeclaringType.Namespace );
        }

        //private static IEnumerable<Instruction> GetInstructions( MethodDefinition originalMethodDefinition,
        //    MethodBody body )
        //{
        //    yield return Instruction.Create( OpCodes.Ldstr,
        //        originalMethodDefinition.DeclaringType.Module.Assembly.FullName );
        //
        //    yield return Instruction.Create( OpCodes.Ldstr,
        //        originalMethodDefinition.DeclaringType.FullName );
        //
        //    yield return Instruction.Create( OpCodes.Ldstr,
        //                            originalMethodDefinition.Name );
        //
        //    MethodReference newMethod;
        //    if ( originalMethodDefinition.ReturnType.FullName == typeof( void ).FullName )
        //    {
        //        //newMethod = _VoidInterceptMethod;
        //    }
        //    else
        //    {
        //        //var type = body.Method.Module.Import( originalMethodDefinition.ReturnType );
        //        //var outVariable = new VariableDefinition( type );
        //        //body.Variables.Add( outVariable );
        //        //yield return Instruction.Create( OpCodes.Ldloca_S, outVariable );
        //        var genericMethod = new GenericInstanceMethod( _ReturnValueInterceptMethod );
        //        genericMethod.GenericArguments.Add( originalMethodDefinition.ReturnType );
        //        newMethod = genericMethod;
        //    }
        //
        //    yield return Instruction.Create( OpCodes.Call, newMethod );
        //}
    }
}