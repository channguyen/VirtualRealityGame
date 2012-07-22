using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AGXNASK {
    /// <summary>
    /// Priority queue for A* close-set and open-set
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class PriorityQueue<T> where T : IComparable<T> {
        private List<T> mList = new List<T>();

        /// <summary>
        /// Add an item to the front of the queue
        /// </summary>
        /// <param name="item">an item</param>
        public void Add(T item) {
            int n = mList.Count;
            mList.Add(item);
            while (n != 0) {
                int p = n / 2;    // parent
                if (mList[n].CompareTo(mList[p]) >= 0)
                    break;  // item >= parent

                // swap
                T tmp = mList[n];
                mList[n] = mList[p];
                mList[p] = tmp; 
                n = p;  // continue
            }
        }


        /// <summary>
        /// Returns the number of elements in the queue.
        /// </summary>
        public int Count {
            get {
                return mList.Count;
            }
        }

        /// <summary>
        /// Returns true if the queue is empty. Trying to call 
        /// Peek() or Pop() on an empty queue will throw an exception.
        /// Check using Empty first before calling these methods.
        /// </summary>
        public bool Empty {
            get {
                return mList.Count == 0;
            }
        }

        /// <summary>
        /// Allows you to look at the first element waiting 
        /// in the queue, without removing it.This element will be the 
        /// one that will be returned if you subsequently call Pop().
        /// </summary>
        public T Peek() {
            return mList[0];
        }

        /// <summary>
        /// Check if an item is in the queue
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsMember(T item) {
            if (mList.IndexOf(item) >= 0)
                return true;

            return false;
        }

        /// <summary>
        /// Get the internal data
        /// NOTE: can't modify
        /// </summary>
        /// <returns></returns>
        public List<T> GetList() {
            return mList;
        }

        /// <summary>
        /// Removes and returns the first element 
        /// from the queue (least element)
        /// </summary>
        /// <returns>The first element in the queue, in ascending order.</returns>
        public T Pop() {
            // The element to return is of course the first element in the array, 
            // or the root of the tree. However, this will leave a 'hole' there. We
            // fill up this hole with the last element from the array. This will 
            // break the heap property. So we bubble the element downwards by swapping
            // it with it's lower child until it reaches it's correct level. The lower
            // child (one of the original elements with index 1 or 2) will now be at the
            // head of the queue (root of the tree).
            T val = mList[0];
            int nMax = mList.Count - 1;
            mList[0] = mList[nMax];
            mList.RemoveAt(nMax);  // Move the last element to the top

            int p = 0;
            while (true) {
                // c is the child we want to swap with. If there
                // is no child at all, then the heap is balanced
                int c = p * 2; 
                if (c >= nMax)  
                    break;

                // If the second child is smaller than the first, that's the one
                // we want to swap with this parent.
                if (c + 1 < nMax && mList[c + 1].CompareTo(mList[c]) < 0)
                    c++;

                // If the parent is already smaller than this smaller child, then
                // we are done
                if (mList[p].CompareTo(mList[c]) <= 0)
                    break;

                // Otherwise, swap parent and child, and follow down the parent
                T tmp = mList[p];
                mList[p] = mList[c];
                mList[c] = tmp;
                p = c;
            }
            return val;
        }
    }
}
