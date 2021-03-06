﻿using System;

namespace StaticMocker.Fody
{
    public static class Param<T>
    {
        public static T Any;
    }

    public class Param
    {
        public static Param In<T>( string name, T value )
        {
            return new Param( name, typeof( T ), ParameterType.In ) { Value = value };
        }

        public static Param In( string name, Type type, object value )
        {
            return new Param( name, type, ParameterType.In ) { Value = value };
        }

        public static Param Out<T>( string name )
        {
            return new Param( name, typeof( T ), ParameterType.Out );
        }

        public static Param Out( string name, Type type )
        {
            return new Param( name, type, ParameterType.Out );
        }

        internal static Param CreateAny( string name, Type type, ParameterType parameterType )
        {
            return new Param( name, type, parameterType )
            {
                MatchesAnyValue = true
            };
        }

        private Param( string name, Type type, ParameterType parameterType )
        {
            if ( name == null ) throw new ArgumentNullException( "name" );
            if ( type == null ) throw new ArgumentNullException( "type" );

            Name = name;
            Type = type;
            ParameterType = parameterType;
        }

        private bool MatchesAnyValue { get; set; }

        public string Name { get; private set; }
        public Type Type { get; private set; }
        public ParameterType ParameterType { get; private set; }

        public object Value { get; set; }

        public override bool Equals( object obj )
        {
            if ( ReferenceEquals( null, obj ) ) return false;
            if ( ReferenceEquals( this, obj ) ) return true;
            if ( obj.GetType() != GetType() ) return false;
            return Equals( (Param)obj );
        }

        private bool Equals( Param other )
        {
            return string.Equals( Name, other.Name ) &&
                   Type == other.Type &&
                   Equals( Value, other.Value );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ( Name != null ? Name.GetHashCode() : 0 );
                hashCode = ( hashCode * 397 ) ^ ( Type != null ? Type.GetHashCode() : 0 );
                hashCode = ( hashCode * 397 ) ^ ( Value != null ? Value.GetHashCode() : 0 );
                return hashCode;
            }
        }

        public override string ToString()
        {
            return string.Format( "{0} {1} = {{{2}}}", Type.Name, Name, Value ?? "" );
        }

        public virtual bool Matches( Param other )
        {
            return string.Equals( Name, other.Name ) &&
                   Type == other.Type &&
                   ( MatchesAnyValue || Equals( Value, other.Value ) );
        }
    }

    public enum ParameterType
    {
        In,
        Out,
        //Ref
    }
}