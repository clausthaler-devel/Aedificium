using System;
using System.Text.RegularExpressions;

namespace Aedificium
{

    public static class StringExtensions
    {
        public static string reReplace( this string str, string regex, int group )
        {
            return Regex.Replace( str, regex, m => m.Groups[ group ].Value );
        }

        public static string reReplace( this string str, string regex, MatchEvaluator evaluator )
        {
            return Regex.Replace( str, regex, evaluator );
        }

        public static string reReplace( this string str, string regex, string replacement )
        {
            return Regex.Replace( str, regex, replacement );
        }

        public static bool matches( this String str, String regex )
        {
            return regex.matches( new Regex( regex ) );
         }

        public static bool matches( this String str, Regex regex )
        {
            return regex.IsMatch( str );
        }

        public static string ucFirst( this String str )
        {
            return str.Length < 2 ? str : str.Substring( 0, 1 ).ToUpper() + str.Substring( 1 );
        }


        public static string camelCase( this String str )
        {
            return Regex.Replace( str, @"(\w)-(\w)", m => string.Format( "{0}{1}", m.Groups[1].Value, m.Groups[2].Value.ToUpper() ) );
        }

        public static string reverseCamelCase( this String str )
        {
            return Regex.Replace( str, @"([a-z])([A-Z])", m => string.Format( "{0}-{1}", m.Groups[1].Value, m.Groups[2].Value.ToUpper() ) );
        }


        public static int WordCount( this String str )
        {
            return str.Split( new char[] { ' ', '.', '?' },
                             StringSplitOptions.RemoveEmptyEntries ).Length;
        }

        public static string format( this String str, params object[] args )
        {
            return String.Format( str, args );
        }
    }

}

