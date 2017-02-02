using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aedificium
{
    public class DetourUtility
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        readonly List<Detour> detours = new List<Detour>();

        protected void DetourMethod(Type fromType, string fromMethod, Type toType, string toMethod, int args = -1)
        {
            try
            {
                MethodInfo from = GetMethod(fromType, fromMethod, args), to = GetMethod(toType, toMethod);

                if (from == null)
                    Profiler.Trace( "{0} reflection failed: {1}", fromType, fromMethod);
                else if (to == null)
                    Profiler.Trace( "{0} reflection failed: {1}", toType, toMethod);
                else
                    detours.Add(new Detour(from, to));
            }
            catch (Exception e)
            {
                Profiler.Trace( "Reflection failed in {0}", GetType());
                UnityEngine.Debug.LogException(e);
            }
        }

        protected void DetourMethod(Type fromType, string fromMethod, string toMethod, int args = -1)
        {
            DetourMethod(fromType, fromMethod, GetType(), toMethod, args);
        }

        protected void DetourMethod(Type fromType, string fromMethod, int args = -1)
        {
            DetourMethod(fromType, fromMethod, GetType(), fromMethod, args);
        }

        static MethodInfo GetMethod(Type type, string method, int args = -1)
        {
            return args < 0 ? type.GetMethod(method, FLAGS) :
                              type.GetMethods(FLAGS).Single(m => m.Name == method && m.GetParameters().Length == args);
        }

        protected void DetourMethod(Type fromType, string fromMethod, int args, int argIndex, Type argType)
        {
            try
            {
                MethodInfo from = GetMethod(fromType, fromMethod, args, argIndex, argType), to = GetMethod(GetType(), fromMethod);

                if (from == null)
                    Profiler.Trace( "{0} reflection failed: {1}", fromType, fromMethod);
                else if (to == null)
                    Profiler.Trace( "{0} reflection failed: {1}", GetType(), fromMethod);
                else
                    detours.Add(new Detour(from, to));
            }
            catch (Exception e)
            {
                Profiler.Trace( "Reflection failed in {0}", GetType());
                UnityEngine.Debug.LogException(e);
            }
        }

        static MethodInfo GetMethod(Type type, string method, int args, int argIndex, Type argType)
        {
            return type.GetMethods(FLAGS).Single(m => m.Name == method && m.GetParameters().Length == args && m.GetParameters()[argIndex].ParameterType == argType);
        }

        public void Deploy()
        {
            foreach (Detour d in detours)
                d.Deploy();
        }

        public void Revert()
        {
            foreach (Detour d in detours)
                d.Revert();
        }

        public virtual void Dispose()  =>  detours.Clear();
    }

    public class Detour
    {
        readonly MethodInfo from, to;
        bool deployed = false;
        RedirectCallsState state;

        public Detour(MethodInfo from, MethodInfo to)
        {
            this.from = from;
            this.to = to;
        }

        public void Deploy()
        {
            try
            {
                if (!deployed)
                    state = RedirectionHelper.RedirectCalls(from, to);

                deployed = true;
            }
            catch (Exception e)
            {
                Profiler.Trace( "Detour of {0} -> {1} failed", from.Name, to.Name );
                UnityEngine.Debug.LogException(e);
            }
        }

        public void Revert()
        {
            try
            {
                if (deployed)
                    RedirectionHelper.RevertRedirect(from, state);

                deployed = false;
            }
            catch (Exception e)
            {
                Profiler.Error("Revert of " + from.Name + " failed", e);
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
