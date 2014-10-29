using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace StaticMocker.Fody
{
    public static class StaticMockWeaver
    {
        private static TypeDefinition _InterceptorType;
        private static MethodReference _VoidInterceptMethod;
        private static MethodReference _ReturnValueInterceptMethod;

        public static void InterceptStatics( string assemblyPathToMock , IAssemblyResolver assemblyResolver, Action<string> logMethod )
        {
            string originalBackup = assemblyPathToMock.Replace( ".dll", ".Original.dll" );
            File.Delete( originalBackup );
            File.Copy( assemblyPathToMock, originalBackup );

            var interceptorAssembly = assemblyResolver.Resolve( "StaticMocker.Fody" );

            //var testClass = interceptorAssembly.MainModule.GetType( typeof( TestClass ).FullName );
            //var expectedMethod = testClass.Methods.Single( x => x.Name == "ExpectedCallsMethodWithReturn" );
            //var instructions = expectedMethod.Body.Instructions.ToList();

            _InterceptorType = interceptorAssembly.MainModule.GetType( typeof( Interceptor ).FullName );

            var assemblyDefinition = AssemblyDefinition.ReadAssembly( assemblyPathToMock );
            var moduleDefinition = assemblyDefinition.MainModule;

            _VoidInterceptMethod = moduleDefinition.Import( _InterceptorType.GetMethods().Single( x =>
            {
                return x.Name == "Intercept" &&
                       x.CallingConvention == MethodCallingConvention.Default &&
                       x.HasParameters &&
                       x.Parameters.Count == 3;
            } ) );
            _ReturnValueInterceptMethod = moduleDefinition.Import( _InterceptorType.GetMethods().Single( x =>
            {
                return x.Name == "Intercept" &&
                       x.CallingConvention == MethodCallingConvention.Generic &&
                       x.HasParameters &&
                       x.Parameters.Count == 3;
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

            assemblyDefinition.Write( assemblyPathToMock );
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
                        body.Instructions.RemoveAt( i-- );
                        foreach ( var instruction in GetInstructions( originalMethodDefinition, body ) )
                        {
                            body.Instructions.Insert( i++, instruction );
                        }

                        //body.Instructions.Insert( i++, Instruction.Create( OpCodes.Brtrue_S, call.Next ) );
                    }
                }
            }
            for ( int i = 0; i < body.Instructions.Count; i++ )
            {
                if ( i > 0 )
                {
                    body.Instructions[i].Previous = body.Instructions[i - 1];
                }
                if ( i < body.Instructions.Count - 1 )
                {
                    body.Instructions[i].Next = body.Instructions[i + 1];
                }
            }

            //body.InitLocals = true;
            //body.OptimizeMacros();
        }

        private static IEnumerable<Instruction> GetInstructions( MethodDefinition originalMethodDefinition,
            MethodBody body )
        {
            yield return Instruction.Create( OpCodes.Ldstr,
                originalMethodDefinition.DeclaringType.Module.Assembly.FullName );

            yield return Instruction.Create( OpCodes.Ldstr,
                originalMethodDefinition.DeclaringType.FullName );

            yield return Instruction.Create( OpCodes.Ldstr,
                                    originalMethodDefinition.Name );

            MethodReference newMethod;
            if ( originalMethodDefinition.ReturnType.FullName == typeof( void ).FullName )
            {
                newMethod = _VoidInterceptMethod;
            }
            else
            {
                //var type = body.Method.Module.Import( originalMethodDefinition.ReturnType );
                //var outVariable = new VariableDefinition( type );
                //body.Variables.Add( outVariable );
                //yield return Instruction.Create( OpCodes.Ldloca_S, outVariable );
                var genericMethod = new GenericInstanceMethod( _ReturnValueInterceptMethod );
                genericMethod.GenericArguments.Add( originalMethodDefinition.ReturnType );
                newMethod = genericMethod;
            }

            yield return Instruction.Create( OpCodes.Call, newMethod );
        }
    }
}