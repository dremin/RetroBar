using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ManagedShell.Common.DesignPatterns
{
    // http://sanity-free.org/132/generic_singleton_pattern_in_csharp.html
    // http://weblogs.asp.net/cumpsd/archive/2008/07/15/6401804.aspx
    // ALT: use the new() constraint
    [DebuggerStepThrough]
    public class SingletonObject<T> : DependencyObject where T : class
    {
        public static T Instance
        {
            get
            {
                return Nested.Instance;
            }
        }

        #region Nested type: Nested
        
        private sealed class Nested
        {
            internal static readonly T Instance;

            static Nested()
            {
                ConstructorInfo ci = typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);

                if (ci == null)
                    throw new InvalidOperationException("class must contain a private constructor");

                Instance = (T)ci.Invoke(null);
            }

            private Nested()
            {
            }
        }

        #endregion
    }
}
