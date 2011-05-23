using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace ClientLiquesceFTPTray
{
   /* Example usage
      string key = "EmployeeList";
      List<Employee> employees;

      if (!CacheHelper.Get(key, out employees))
      {
          employees = DataAccess.GetEmployeeList();
          CacheHelper.Add(employees, key);
          Message.Text = "Employees not found but retrieved and added to cache for next lookup.";
      }
      else
      {
          Message.Text = "Employees pulled from cache.";
      }

   */
   
   /// <summary>
   /// Stolen from http://johnnycoder.com/blog/2008/12/10/c-cache-helper-class/
   /// Then I made it use generics,
   /// TODO:
   /// Then I made it look closer to a Dictionary class
   /// </summary>
   public class CacheHelper< TValue>: IDictionary<string, TValue>
   {
      private readonly uint expireSeconds;

      public CacheHelper(uint expireSeconds)
      {
         this.expireSeconds = expireSeconds;
      }

      /// <summary>
      /// Insert value into the cache using
      /// appropriate name/value pairs
      /// </summary>
      /// <typeparam name="T">Type of cached item</typeparam>
      /// <param name="o">Item to be cached</param>
      /// <param name="key">Name of item</param>
      public void AddWithTimeout(string key, TValue o)
      {
         HttpContext.Current.Cache.Insert(key,o,null,
             DateTime.Now.AddSeconds(expireSeconds),
             System.Web.Caching.Cache.NoSlidingExpiration);
      }

      /// <summary>
      /// Insert value into the cache using
      /// appropriate name/value pairs
      /// </summary>
      /// <typeparam name="T">Type of cached item</typeparam>
      /// <param name="o">Item to be cached</param>
      /// <param name="key">Name of item</param>
      public void AddNoTimeout(string key, TValue o )
      {
         HttpContext.Current.Cache.Insert(key, o);
      }

      /// <summary>
      /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
      /// </summary>
      /// <returns>
      /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.
      /// </returns>
      /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
      bool IDictionary<string, TValue>.ContainsKey(string key)
      {
         return ContainsKey(key);
      }

      /// <summary>
      /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
      /// </summary>
      /// <param name="key">The object to use as the key of the element to add.</param><param name="value">The object to use as the value of the element to add.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
      public void Add(string key, TValue value)
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
      /// </summary>
      /// <returns>
      /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
      /// </returns>
      /// <param name="key">The key of the element to remove.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
      bool IDictionary<string, TValue>.Remove(string key)
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Gets the value associated with the specified key.
      /// </summary>
      /// <returns>
      /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
      /// </returns>
      /// <param name="key">The key whose value to get.</param><param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
      public bool TryGetValue(string key, out TValue value)
      {
         try
         {
            if (!ContainsKey(key))
            {
               value = default(TValue);
               return false;
            }

            value = (TValue)HttpContext.Current.Cache[key];
         }
         catch
         {
            value = default(TValue);
            return false;
         }

         return true;
      }

      /// <summary>
      /// Remove item from cache
      /// </summary>
      /// <param name="key">Name of cached item</param>
      public TValue Remove(string key)
      {
         return (TValue)HttpContext.Current.Cache.Remove(key);
      }

      /// <summary>
      /// Check for item in cache
      /// </summary>
      /// <param name="key">Name of cached item</param>
      /// <returns></returns>
      public bool ContainsKey(string key)
      {
         return HttpContext.Current.Cache[key] != null;
      }


      public TValue this[string key]
      {
         get
         {
            object index = HttpContext.Current.Cache[key];
            if (index != null)
            {
               return (TValue)index;
            }
            throw new KeyNotFoundException();

            return default(TValue);
         }
         set
         {
            this.AddNoTimeout(key, value);
         }
      }

      /// <summary>
      /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
      /// </summary>
      /// <returns>
      /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
      /// </returns>
      public ICollection<string> Keys
      {
         get { throw new NotImplementedException(); }
      }

      /// <summary>
      /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
      /// </summary>
      /// <returns>
      /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
      /// </returns>
      public ICollection<TValue> Values
      {
         get { throw new NotImplementedException(); }
      }


      #region Implementation of IEnumerable

      /// <summary>
      /// Returns an enumerator that iterates through the collection.
      /// </summary>
      /// <returns>
      /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
      /// </returns>
      /// <filterpriority>1</filterpriority>
      public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Returns an enumerator that iterates through a collection.
      /// </summary>
      /// <returns>
      /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
      /// </returns>
      /// <filterpriority>2</filterpriority>
      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }

      #endregion

      #region Implementation of ICollection<KeyValuePair<string,TValue>>

      /// <summary>
      /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
      /// </summary>
      /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
      public void Add(KeyValuePair<string, TValue> item)
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
      /// </summary>
      /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
      public void Clear()
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
      /// </summary>
      /// <returns>
      /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
      /// </returns>
      /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
      public bool Contains(KeyValuePair<string, TValue> item)
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
      /// </summary>
      /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
      public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
      /// </summary>
      /// <returns>
      /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
      /// </returns>
      /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
      public bool Remove(KeyValuePair<string, TValue> item)
      {
         throw new NotImplementedException();
      }

      /// <summary>
      /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
      /// </summary>
      /// <returns>
      /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
      /// </returns>
      public int Count
      {
         get { throw new NotImplementedException(); }
      }

      /// <summary>
      /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
      /// </summary>
      /// <returns>
      /// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
      /// </returns>
      public bool IsReadOnly
      {
         get { throw new NotImplementedException(); }
      }

      #endregion
   }
}
