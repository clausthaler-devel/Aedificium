using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

#if DEBUG
using PostSharp.Aspects;
#endif

namespace Aedificium
{
#if DEBUG
    [Serializable]
    public class MethodTraceAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry( MethodExecutionArgs args )
        {
            Console.WriteLine( args.Method.Name + " started" );
        }

        public override void OnExit( MethodExecutionArgs args )
        {
            Console.WriteLine( args.Method.Name + " finished" );
        }
    }

    [Serializable]
    [ProfilerAspect( AttributeExclude = true )]
    public class ProfilerAspect : PostSharp.Aspects.OnMethodBoundaryAspect
    {
        public override void OnEntry( MethodExecutionArgs args )
        {
            string output = string.Format(
                "Executing {0}.{1} ( {2} )",
                args.Instance?.GetType().ToString(),
                args.Method.Name.Replace(".ctor", "ctor"),
                String.Join( ",", args.Method.GetParameters().Select( n => n.ParameterType.Name + " " + n.Name ).ToArray() )
            );
            Profiler.Trace( output );
            Profiler.indentation++;

            args.MethodExecutionTag = Stopwatch.StartNew();
        }

        public override void OnExit( MethodExecutionArgs args )
        {
            Stopwatch sw = (Stopwatch)args.MethodExecutionTag;
            sw.Stop();

            string output = string.Format("{0} Executed in {1} milliseconds",
                             args.Method.Name, sw.ElapsedMilliseconds);

            Profiler.indentation--;
            Profiler.Trace( output );

        }
    }

    public class Profiler
    {
        public enum DebugLevels
        {
            Error,
            Warning,
            Info,
            Trace,
            Undetermined
        }

        private static DebugLevels _DebugLevel = DebugLevels.Undetermined;
        public static DebugLevels DebugLevel
        {
            get
            {
                if ( _DebugLevel == DebugLevels.Undetermined )
                {
                    //var v = Environment.GetEnvironmentVariable("RICOTRACE");
                    //if ( v != null && v != "" && v != "0" )
                    //    _DebugLevel = DebugLevels.Trace;
                    _DebugLevel = DebugLevels.Trace;
                }

                return _DebugLevel;
            }
        }

        public static Profiler Instance;
        public static String OutputFileName;
        public static int indentation = 0;
        private DateTime StartTime;
        private DateTime EndTime;
        private Dictionary<string, DateTime> Timers = new Dictionary<string, DateTime>();

        private static void Write( string s, string mode )
        {
            if ( OutputFileName == null )
                OutputFileName = Path.Combine(
                    Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
                    @"Colossal Order\Cities_Skylines\Aedificium.trace"
                );

            if ( Instance == null )
                Instance = new Profiler( OutputFileName, ClearLogfileAtStart: true );

            Instance.WriteString( s, mode, indentation );
        }

        public static void Message( string msg )
        {
            msg += "\r\n";
            Profiler.Write( msg, null );
        }

        public static void Trace( string format, params object[] args )
        {
            var message = string.Format( format, args);

            if ( (int) DebugLevel >= 3 )
                Profiler.Write( "{TRACE}", message + "\r\n" );
        }

        public static void Info( string format, params object[] args )
        {
            var message = string.Format( format, args);

            if ( (int) DebugLevel >= 1 )
                Profiler.Write( "{INFO}", message + "\r\n" );
        }

        public static void Warning( string format, params object[] args )
        {
            var message = string.Format( format, args);

            if ( (int) DebugLevel >= 2 )
                Profiler.Write( "{WARNING}", message + "\r\n" );
        }

        public static void Error( string error, Exception e )
        {
            Profiler.Error( "{0}: {1}\r\n{2}", error, e.Message, e.StackTrace );
        }

        public static void Error( string format, params object[] args )
        {
            var message = string.Format( format, args);

            if ( (int) DebugLevel >= 1 )
                Profiler.Write( "{ERROR}", message + "\r\n" );
        }


        public void WriteString( string mode, string s, int indentation )
        {
            if ( DebugLevel != DebugLevels.Trace )
                indentation = 0;

            var b = System.Text.Encoding.Unicode.GetBytes(
                mode != null ?
                String.Format("{0:MM-dd-yyyy H:mm:ss} {1} {2}{3}", DateTime.Now, mode, new string( ' ', indentation ), s ) :
                String.Format("{0:MM-dd-yyyy H:mm:ss} {1}{2}", DateTime.Now, new string( ' ', indentation ), s )
            );

            _outputStream.Write( b, 0, b.Length );
            _outputStream.Flush();
        }

        public void Dump()
        {
            //    System.Runtime.Serialization. .Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(myPerson.GetType());
            //    MemoryStream ms = new MemoryStream();
            //    serializer.WriteObject( ms, theUser );
            //    string json = Encoding.Default.GetString(ms.ToArray());
        }

        public string _outputFileName;
        public Stream _outputStream;

        public Profiler( string outputFileName, bool ClearLogfileAtStart = false )
        {
            StartTime = DateTime.Now;
            _outputFileName = outputFileName;
            if ( File.Exists( _outputFileName ) && ClearLogfileAtStart )
                File.Delete( _outputFileName );

            _outputStream = new FileStream( _outputFileName, FileMode.Append, FileAccess.Write );
            this.WriteString( null, "Profiler started\r\n", 0 );
        }

        public Profiler( Stream outputStream )
        {
            StartTime = DateTime.Now;
            _outputStream = outputStream;
            this.WriteString( null, "Profiler started\r\n", 0 );
        }

        public static void FlushOutput()
        {
            Instance._outputStream.Flush();
            Instance._outputStream.Close();
            Instance._outputStream = new FileStream( Profiler.Instance._outputFileName, FileMode.Append, FileAccess.Write );
        }

        ~Profiler()
        {
            if ( _outputStream != null )
            {
                EndTime = DateTime.Now;
                var delta = EndTime - StartTime;

                try
                {
                    var outputStream = new FileStream( OutputFileName, FileMode.Append, FileAccess.Write );
                    var s = String.Format( "{0:MM-dd-yyyy H:mm:ss} Profiler ended after {1}:{2},{3}\r\n", DateTime.Now, delta.Minutes, delta.Seconds, delta.Milliseconds );
                    var b = System.Text.Encoding.Unicode.GetBytes( new string( ' ', indentation ) + s  );
                    outputStream.Write( b, 0, b.Length );
                    outputStream.Flush();
                    outputStream.Close();
                    _outputStream.Close();
                }
                catch
                {

                }
            }
        }
    }
#else
    public class Profiler
    {
        public static void Write( string s )
        {
            Console.Write(s);
        }

        public static void Message( string msg )
        {
            msg += "\r\n";
            Profiler.Write( msg );
        }

        public static void Info( string message )
        {
            Profiler.Write( " [INFO] " + message + "\r\n" );
        }

        public static void Debug( string variable, string value )
        {
            Profiler.Write( String.Format( "{0} : {1}\r\n", variable, value ) ); ;
        }
    }
#endif
}
