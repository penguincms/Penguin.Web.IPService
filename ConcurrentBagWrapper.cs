using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace Penguin.Web
{
    public class ConcurrentBagWrapper<T> : IList<T>
    {
        public ConcurrentBag<T> Backing { get; internal set; }
        public int Count => Backing.Count;
        public bool IsReadOnly => false;

        public T this[int index] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        internal ConcurrentBagWrapper(ConcurrentBag<T> backing)
        {
            Backing = backing;
        }

        public int IndexOf(T item) =>  Backing.ToList().IndexOf(item);

        public void Insert(int index, T item)
        {
            Backing.Add(item);
        }

        public void RemoveAt(int index) => throw new NotImplementedException();

        public void Add(T item) => Backing.Add(item);
        public void Clear()
        {
            while(Backing.Any())
            {
                Backing.TryTake(out T _);
            }
        }
        public bool Contains(T item) => Backing.Any(c => c != null && c.Equals(item));
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            List<T> items = Backing.ToList();
            bool toReturn = false;
            Clear();

            foreach(T i in items)
            {
                if(!(i != null && i.Equals(item)))
                {
                    Backing.Add(i);
                } else
                {
                    if (i != null)
                    {
                        toReturn = true;
                    }
                }
            }
            return toReturn;
        }
        public IEnumerator<T> GetEnumerator() => Backing.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Backing.GetEnumerator();
    }
}
