using System;

namespace StaticMocker.Fody
{
    public class Param
    {
        public static T Any<T>()
        {
            return default( T );
        }

        public static int Is( int i )
        {
            return i;
        }

        public static Param In<T>( string name, T value )
        {
            return new Param( name, typeof( T ) ) { Value = value };
        }

        public static Param Out<T>( string name )
        {
            return new Param( name, typeof( T ) );
        }

        private Param( string name, Type type )
        {
            if ( name == null ) throw new ArgumentNullException( "name" );
            if ( type == null ) throw new ArgumentNullException( "type" );

            Name = name;
            Type = type;
        }

        public string Name { get; private set; }
        public Type Type { get; private set; }

        public object Value { get; set; }
    }


}