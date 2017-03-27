using System;
using System.Reflection;

namespace QuickCompiler
{
    public class ObjectWrapper
    {
        private readonly Assembly _assembly;
        private readonly string _typeName;
        private readonly Type _type;
        private readonly object _object;

        public ObjectWrapper(Assembly assembly, string typeName)
        {
            _assembly = assembly;
            _typeName = typeName;
            _type = assembly.GetType(typeName);
            _object = Activator.CreateInstance(_type);
        }

        public void Action(string methodName)
        {
            var method = _type.GetMethod(methodName);
            var action = (Action)Delegate.CreateDelegate(typeof(Action), _object, method);
            action();
        }

        public void Action<T1>(string methodName, T1 t1)
        {
            var method = _type.GetMethod(methodName);
            var action = (Action<T1>)Delegate.CreateDelegate(typeof(Action<T1>), _object, method);
            action(t1);
        }

        public TResult Func<TResult>(string methodName)
        {
            var method = _type.GetMethod(methodName);
            var func = (Func<TResult>)Delegate.CreateDelegate(typeof(Func<TResult>), _object, method);
            var result = func();
            return result;
        }

        public TResult Func<T1, TResult>(string methodName, T1 t1)
        {
            var method = _type.GetMethod(methodName);
            var func = (Func<T1, TResult>)Delegate.CreateDelegate(typeof(Func<T1, TResult>), _object, method);
            var result = func(t1);
            return result;
        }
    }
}
