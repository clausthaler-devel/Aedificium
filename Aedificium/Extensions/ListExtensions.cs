using System.Collections.Generic;

namespace Aedificium
{
    public static class ListExtensions
    {
        public static List<T> ToList<T>( this T[] array, int count )
        {
            List<T> ret = new List<T>(count + 5);

            for ( int i = 0 ; i < count ; i++ )
                ret.Add( array[i] );

            return ret;
        }
    }
}

