using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenNos.Core
{
    public class ThreadSafeGenericLockedList<T>
    {
        #region Members

        private List<T> _internalList;

        #endregion

        #region Instantiation

        public ThreadSafeGenericLockedList()
        {
            _internalList = new List<T>();
        }

        public ThreadSafeGenericLockedList(List<T> other)
        {
            _internalList = other.ToList();
        }

        #endregion

        #region Interface Implementation

        public T this[int index]
        {
            get
            {
                lock (_internalList)
                {
                    return _internalList[index];
                }
            }

            set
            {
                lock (_internalList)
                {
                    _internalList[index] = value;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_internalList)
                {
                    return _internalList.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            lock (_internalList)
            {
                _internalList.Add(item);
            }
        }

        public void Clear()
        {
            lock (_internalList)
            {
                _internalList.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_internalList)
            {
                return _internalList.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_internalList)
            {
                _internalList.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator() => ToList().GetEnumerator();

        public int IndexOf(T item)
        {
            lock (_internalList)
            {
                return _internalList.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_internalList)
            {
                _internalList.Insert(index, item);
            }
        }

        public bool Remove(T item)
        {
            lock (_internalList)
            {
                return _internalList.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_internalList)
            {
                _internalList.RemoveAt(index);
            }
        }

        #endregion

        #region Methods

        public void AddRange(IEnumerable<T> collection)
        {
            lock (_internalList)
            {
                _internalList.AddRange(collection);
            }
        }

        public bool Any(Func<T, bool> predicate)
        {
            lock (_internalList)
            {
                return _internalList.Any(predicate);
            }
        }

        public bool Any()
        {
            lock (_internalList)
            {
                return _internalList.Any();
            }
        }

        public ThreadSafeGenericLockedList<T> Clone() => new ThreadSafeGenericLockedList<T>(_internalList);

        public void Lock(Action action)
        {
            lock (_internalList)
            {
                action();
            }
        }

        public void ForEach(Action<T> action)
        {
            lock (_internalList)
            {
                _internalList.ForEach(action);
            }
        }

        public int RemoveAll(Predicate<T> match)
        {
            lock (_internalList)
            {
                return _internalList.RemoveAll(match);
            }
        }

        public List<T> ToList()
        {
            lock (_internalList)
            {
                return _internalList.ToList();
            }
        }

        public List<T> Where(Func<T, bool> predicate)
        {
            lock (_internalList)
            {
                return _internalList.Where(predicate).ToList();
            }
        }

        #endregion
    }
}