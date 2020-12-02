using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HandlebarsDotNet.Collections;

namespace HandlebarsDotNet.Runtime
{
    /// <summary>
    /// Provides cache for frequently used struct values to avoid unnecessary boxing. 
    /// <para>Overuse may lead to memory leaks! Do not store one-time values here!</para>
    /// <para>Usage example: indexes in iterators</para>
    /// </summary>
    public static class BoxedValues
    {
        private const int BoxedIntegersCount = 20;
        private static readonly object[] BoxedIntegers = new object[BoxedIntegersCount];

        static BoxedValues()
        {
            for (var index = 0; index < BoxedIntegers.Length; index++)
            {
                BoxedIntegers[index] = index;
            }
        }
        
        public static readonly object True = true;
        public static readonly object False = false;
        public static readonly object Zero = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Int(int value)
        {
            if (value >= 0 && value < BoxedIntegersCount)
            {
                return BoxedIntegers[value];
            }
            
            return Value(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Value<T>(T value) where T: struct =>
            BoxedContainer<T>.Boxed.GetOrAdd(value, v => (object) v);

        private static class BoxedContainer<T>
        {
            public static readonly LookupSlim<T, object, IEqualityComparer<T>> Boxed = new LookupSlim<T, object, IEqualityComparer<T>>(EqualityComparer<T>.Default);
        }
    }
}