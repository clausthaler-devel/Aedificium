using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Aedificium
{
    public static class ReflectionExtensions
    {

        public static T GetAttribute<T>( this MemberInfo obj )
        {
            foreach ( var attribute in obj.GetCustomAttributes( false ) )
                if ( attribute.GetType().Name == typeof( T ).Name )
                    return (T) attribute;

            return default( T );
        }

        public static bool HasAttribute( this MemberInfo obj, string attributeName )
        {
            foreach ( var attribute in obj.GetCustomAttributes( false ) )
                if ( attribute.GetType().Name == attributeName )
                    return true;

            return false;
        }

        public static Dictionary<String, Object> GetValues( this Object obj )
        {
            var values = new Dictionary<String, Object>();

            foreach ( var setter in obj.GetType().GetProperties() )
                values.Add( setter.Name, setter.GetValue( obj, null ) );

            foreach ( var setter in obj.GetType().GetFields() )
                values.Add( setter.Name, setter.GetValue( obj ) );

            return values;
        }

        /// <summary>
        /// Returns a list of all types obj has.
        /// </summary>
        /// <param name="obj">The object to be inspected</param>
        /// <returns>A list of all types obj has</returns>
        public static List<Type> GetTypes( this Object obj )
        {
            var types = new List<Type>();
            var currentType = obj.GetType();

            while ( currentType != null )
            {
                types.Add( currentType );
                currentType = currentType.BaseType;
            }

            return types;
        }

        public static List<MemberInfo> GetMembers( this Object obj, bool publicMembers = true, bool privateMembers = true )
        {
            var members = new List<MemberInfo>();
            var seen = new HashSet<String>();

            var bindingFlags = BindingFlags.Instance;

            if ( publicMembers )
                bindingFlags = bindingFlags | BindingFlags.Public;

            if ( privateMembers )
                bindingFlags = bindingFlags | BindingFlags.NonPublic;

            foreach ( var currentType in obj.GetTypes() )
            {
                foreach ( var memberInfo in currentType.GetFields( bindingFlags ) )
                {
                    if ( !memberInfo.Name.Contains( "_BackingField" ) && !seen.Contains( memberInfo.Name ) )
                    {
                        seen.Add( memberInfo.Name );
                        members.Add( memberInfo );
                    }
                }

                foreach ( var memberInfo in currentType.GetProperties( bindingFlags ) )
                {
                    if ( !memberInfo.Name.Contains( "_BackingField" ) && !seen.Contains( memberInfo.Name ) )
                    {
                        seen.Add( memberInfo.Name );
                        members.Add( memberInfo );
                    }
                }
            }

            return members;
        }


        public static void SetValue<T>( this Object obj, String setterName, T value )
        {

            foreach ( var property in obj.GetType().GetProperties() )
            {
                if ( setterName == property.Name )
                {
                    property.SetValue( obj, Convert.ChangeType( value, property.PropertyType ), null );
                    return;
                }
            }

            foreach ( var setter in obj.GetType().GetFields() )
            {
                if ( setterName == setter.Name )
                {
                    setter.SetValue( obj, Convert.ChangeType( value, setter.FieldType ) );
                    return;
                }
            }
        }

        public static T GetValue<T>( this Object obj, string getterName )
        {
            foreach ( var setter in obj.GetType().GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                if ( getterName == setter.Name )
                {
                    return (T) setter.GetValue( obj, null );
                }
            }

            foreach ( var setter in obj.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                if ( getterName == setter.Name )
                {
                    return (T) setter.GetValue( obj );
                }
            }

            return default( T );
        }



        public delegate String AccessorNameTransform( MemberInfo accessor );
        public delegate MemberInfo[] AccessorFilter( MemberInfo[] fields );

        public static void CopyValuesTo<T>( this Object source, T target, AccessorNameTransform nameTransform = null, AccessorFilter filter = null )
        {
            target.CopyValuesFrom( source, nameTransform, filter );
        }

        public static void CopyValuesFrom<T>( this Object obj, T source, AccessorNameTransform nameTransform = null, AccessorFilter filter = null )
        {
            CopyFieldValuesFrom( obj, source, nameTransform, filter );
            CopyPropertyValuesFrom( obj, source, nameTransform, filter );
        }

        public static void CopyFieldValuesFrom<T>( this Object obj, T source, AccessorNameTransform nameTransform = null, AccessorFilter filter = null )
        {
            var fields = filter != null ?
                filter( source.GetType().GetFields() ) :
                source.GetType().GetFields();

            foreach ( var getter in fields )
            {
                if ( nameTransform != null )
                    obj.SetValue( nameTransform( getter ), ( (FieldInfo) getter ).GetValue( source ) );
                else
                    obj.SetValue( getter.Name, ( (FieldInfo) getter ).GetValue( source ) );
            }
        }

        public static void CopyPropertyValuesFrom<T>( this Object obj, T source, AccessorNameTransform nameTransform = null, AccessorFilter filter = null )
        {
            var properties = filter != null ?
                filter( source.GetType().GetProperties() ) :
                source.GetType().GetProperties();

            foreach ( var getter in properties )
            {
                if ( nameTransform != null )
                    obj.SetValue( nameTransform( getter ), ( (PropertyInfo) getter ).GetValue( source, null ) );
                else
                    obj.SetValue( getter.Name, ( (PropertyInfo) getter ).GetValue( source, null ) );
            }
        }
    }


    public static class StringExtensions
    {
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

