using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Penguin.Web
{
    public class ConcurrentBagWrapper<T> : IList<T>
    {
        public ConcurrentBag<T> Backing { get; internal set; }
        public int Count => this.Backing.Count;
        public bool IsReadOnly => false;

        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        internal ConcurrentBagWrapper(ConcurrentBag<T> backing)
        {
            this.Backing = backing;
        }

        public int IndexOf(T item)
        {
            return this.Backing.ToList().IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.Backing.Add(item);
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(T item)
        {
            this.Backing.Add(item);
        }

        public void Clear()
        {
            while (this.Backing.Any())
            {
                _ = this.Backing.TryTake(out _);
            }
        }
        public bool Contains(T item)
        {
            return this.Backing.Any(c => c != null && c.Equals(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            List<T> items = this.Backing.ToList();
            bool toReturn = false;
            this.Clear();

            foreach (T i in items)
            {
                if (!(i != null && i.Equals(item)))
                {
                    this.Backing.Add(i);
                }
                else
                {
                    if (i != null)
                    {
                        toReturn = true;
                    }
                }
            }

            return toReturn;
        }
        public IEnumerator<T> GetEnumerator()
        {
            return this.Backing.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Backing.GetEnumerator();
        }
    }
}
