using System;
using System.Text;
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

        public static bool SetValue<T>( this Object obj, String setterName, T value )
        {
            return SetFieldValue( obj, setterName, value ) || SetPropertyValue( obj, setterName, value );
        }

        public static bool SetNull( this Object obj, String setterName )
        {
            foreach ( var accessor in obj.GetType().GetProperties() )
                if ( setterName == accessor.Name )
                {
                    accessor.SetValue( obj, null, null );
                    return true;
                }

            foreach ( var accessor in obj.GetType().GetFields() )
                if ( setterName == accessor.Name )
                {
                    accessor.SetValue( obj, null );
                    return true;
                }

            return false;
        }

        public static bool SetPropertyValue<T>( this Object obj, string setterName, T value )
        {
            foreach ( var accessor in obj.GetType().GetProperties() )
            {
                if ( setterName == accessor.Name )
                {
                    accessor.SetValue( obj, Convert.ChangeType( value, accessor.PropertyType ), null );
                    return true;
                }
            }

            return false;
        }

        public static bool SetFieldValue<T>( this Object obj, string setterName, T value )
        {
            foreach ( var accessor in obj.GetType().GetFields() )
            {
                if ( setterName == accessor.Name )
                {
                    accessor.SetValue( obj, Convert.ChangeType( value, accessor.FieldType ) );
                    return true;
                }
            }

            return false;
        }

        public static T GetValue<T>( this Object obj, string getterName )
        {
            T value;
            if ( GetPropertyValue( obj, getterName, out value ) )
                return value;
            if ( GetFieldValue( obj, getterName, out value ) )
                return value;

            return default( T );
        }

        public static bool GetPropertyValue<T>( this Object obj, string getterName, out T value )
        {
            foreach ( var accessor in obj.GetType().GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                if ( getterName == accessor.Name )
                {
                    value = (T) accessor.GetValue( obj, null );
                    return true;
                }
            }

            value = default( T );
            return false;
        }

        public static T GetPropertyValue<T>( this Object obj, string getterName )
        {
            T value;
            GetPropertyValue( obj, getterName, out value );
            return value;
        }

        public static bool GetFieldValue<T>( this Object obj, string getterName, out T value )
        {
            return GetGenericFieldValue<T>( obj, getterName, out value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
        }

        public static bool GetStaticFieldValue<T>( this Object obj, string getterName, out T value )
        {
            Profiler.Trace( ">>" );
            return GetGenericFieldValue<T>( obj, getterName, out value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static );
        }

        public static bool GetGenericFieldValue<T>( this Object obj, string getterName, out T value, BindingFlags flags )
        {
            foreach ( var accessor in obj.GetType().GetFields( flags ) )
            {
                if ( getterName == accessor.Name )
                {
                    value = (T) accessor.GetValue( obj );
                    return true;
                }
            }

            value = default( T );

            return false;
        }

        public static T GetFieldValue<T>( this Object obj, string getterName )
        {
            T value;
            GetFieldValue( obj, getterName, out value );
            return value;
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
                filter( source.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) ) :
                source.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

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

        public static string Dump( this Object obj )
        {
            var sb = new StringBuilder();
            sb.AppendFormat( "[{0}]", obj.GetType() );
            foreach ( var accessor in obj.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
                sb.AppendFormat( "{0}={1}\r\n", accessor.Name, accessor.GetValue( obj ) );

            return sb.ToString();
        }

        public static T Invoke<T>( this Object obj, string methodName, params object[] args )
        {
            var method = obj.GetType().GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            Profiler.Trace( "Invoking {0} = {1}", methodName, method );
            foreach ( var x in args )
                Profiler.Trace( "arg {0}", x );
            return method != null ? (T) method.Invoke( obj, args ) : default( T );
        }

        public static object Invoke( this Object obj, string methodName, params object[] args )
        {
            var method = obj.GetType().GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            return method != null ? method.Invoke( obj, args ) : null;
        }

        public static object InvokeStatic( this Object obj, string methodName, params object[] args )
        {
            var method = obj.GetType().GetMethod( methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return method != null ? method.Invoke( obj, args ) : null;
        }
    }
}

