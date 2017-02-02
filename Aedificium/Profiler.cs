using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

#if DEBUG
using PostSharp.Aspects;
using Aedificium.Extensions;
#endif

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
        var type = args.Instance?.GetType().ToString();
        var method = args.Method.Name;
        var arglist = String.Join( ", ", args.Method.GetParameters().Select( (n, i) =>
            String.Format("{0} {1} = {2}", n.ParameterType.Name, n.Name, args.Arguments[i] )
        ).ToArray() );
        
        Profiler.Trace( "Executing {0}.{1} ( {2} )", type, method, arglist );
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
#endif

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
                _DebugLevel = Aedificium.Settings.instance.DebugLevel;
            }

            return _DebugLevel;
        }
    }

    public static Profiler Instance;
    public static int indentation = 0;

    static String OutputFileName;
    DateTime StartTime;

    private static void Write( string mode, string message )
    {
        if ( OutputFileName == null )
            OutputFileName = Path.Combine(
                Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
                @"Colossal Order\Cities_Skylines\Aedificium.trace"
            );

        if ( Instance == null )
            Instance = new Profiler( OutputFileName, ClearLogfileAtStart: true );

        Instance.WriteString( mode, message, indentation );
    }

    public static void Message( string msg )
    {
        msg += "\r\n";
        Profiler.Write( msg, null );
    }

    public static T TraceReturn<T>( string format, T value )
    {
        Trace( format, value );
        return value;
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


    public void WriteString( string mode, string message, int indentation )
    {
        if ( DebugLevel != DebugLevels.Trace )
            indentation = 0;

        var space = new string( ' ', indentation );
        var now   = String.Format( "{0:MM-dd-yyyy H:mm:ss}", DateTime.Now );
        var strng = String.IsNullOrEmpty( mode )? $"{now} {space}{message}" : $"{now} {mode} {space}{message}";
        var bytes = System.Text.Encoding.Unicode.GetBytes(strng);

        _outputStream.Write( bytes, 0, bytes.Length );
        _outputStream.Flush();
    }

    public static void Dump( object obj )
    {
        Profiler.Write( "DUMP", obj.Dump() ); 
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
            var delta = DateTime.Now - StartTime;

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

