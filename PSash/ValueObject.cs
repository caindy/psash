using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSash
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    [Serializable]
    public abstract class ValueObject<T, TValue, TValues> : IEquatable<T>, IComparable, IComparable<T>
        where T : ValueObject<T, TValue, TValues>
        where TValue : IEquatable<TValue>, IComparable<TValue>
        where TValues : ValueObject<T, TValue, TValues>.Values<TValues>
    {
        /// <summary>
        /// This is the encapsulated value.
        /// </summary>
        public readonly TValue Value;
        protected ValueObject(TValue value)
        {
            Value = value;
        }

        #region equality
        public override bool Equals(object other)
        {
            return other != null && other is T && Equals(other as T);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public bool Equals(T other)
        {
            return other != null && Value.Equals(other.Value);
        }

        public static bool operator ==(ValueObject<T, TValue, TValues> x, ValueObject<T, TValue, TValues> y)
        {
            // pointing to same heap location
            if (ReferenceEquals(x, y)) return true;

            // both references are null
            if (null == (object)(x ?? y)) return true;

            // auto-boxed LHS is not null
            if ((object)x != null)
                return x.Equals(y);

            return false;
        }

        public static bool operator !=(ValueObject<T, TValue, TValues> x, ValueObject<T, TValue, TValues> y)
        {
            return !(x == y);
        }
        #endregion

        public static implicit operator TValue(ValueObject<T, TValue, TValues> obj)
        {
            return obj.Value;
        }

        public static implicit operator ValueObject<T, TValue, TValues>(TValue val)
        {
            T valueObject;
            if (Values<TValues>.Instance.TryGetValue(val, out valueObject))
                return valueObject;

            throw new InvalidCastException(String.Format("{0} cannot be converted", val));
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        #region comparison

        public int CompareTo(T other)
        {
            return Value.CompareTo(other);
        }

        public int CompareTo(object obj)
        {
            if (null == obj)
                throw new ArgumentNullException();

            if (obj is T)
                return Value.CompareTo((obj as T).Value);

            throw new ArgumentException(String.Format("Must be of type {0}", typeof(T)));
        }

        #endregion

        [Serializable]
        public abstract class Values<TDomain> : IEnumerable<T> where TDomain : Values<TDomain>
        {
            [NonSerialized]
            protected readonly Dictionary<TValue, T> _values = new Dictionary<TValue, T>();

            private void Add(T valueObject)
            {
                _values.Add(valueObject.Value, valueObject);
            }

            protected T Add(TValue val)
            {
                var valueObject = typeof(T).InvokeMember(typeof(T).Name,
                               BindingFlags.CreateInstance | BindingFlags.Instance |
                               BindingFlags.NonPublic, null, null, new object[] { val }) as T;
                Add(valueObject);
                return valueObject;
            }

            public bool TryGetValue(TValue value, out T valueObject)
            {
                return _values.TryGetValue(value, out valueObject);
            }

            public bool Contains(T valueObject)
            {
                return _values.ContainsValue(valueObject);
            }

            static volatile TDomain _instance;
            static readonly object Lock = new object();
            public static TDomain Instance
            {
                get
                {
                    if (_instance == null)
                        lock (Lock)
                        {
                            if (_instance == null)
                                _instance = typeof(TDomain)
                                    .InvokeMember(typeof(TDomain).Name,
                                                      BindingFlags.CreateInstance |
                                                      BindingFlags.Instance |
                                                      BindingFlags.NonPublic, null, null, null) as TDomain;
                        }
                    return _instance;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _values.Values.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
