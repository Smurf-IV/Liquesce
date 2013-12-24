#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="CacheHelper.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Simon Coghlan (Aka Smurf-IV)
// 
//  This program is free software: you can redistribute it and/or modify.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// 
//  </copyright>
//  <summary>
//  Url: http://liquesce.wordpress.com/2011/06/07/c-dictionary-cache-that-has-a-timeout-on-its-values/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiquesceSvc
{
   /// <summary>
   /// stolen from the discussions in http://blogs.infosupport.com/blogs/frankb/archive/2009/03/15/CacheDictionary-for-.Net-3.5_2C00_-using-ReaderWriterLockSlim-_3F00_.aspx
   /// And then made it more useable for the cache timeout implementation.
   /// I did play with the ConcurrentDictonary, but this made the simplicity of using a Mutex and a normal dictionary very difficult to read.
   /// </summary>
   /// <example>
   /// Use it like a dictionary and then add the functions required
   /// </example>
   /// <remarks>
   /// Does not implement all the interfaces of IDictionary.
   /// All Thread access locking is performed with this object, so no need for access locking by the caller.
   /// </remarks>
   public class CacheHelper<TKey, TValue>
   {
      #region private fields
      private readonly bool useAPICallToRelease;
      private readonly object cacheLock = new object();
      private class ValueObject<TValueObj>
      {
         private DateTimeOffset Created;
         public readonly TValueObj CacheValue;

         public ValueObject(uint expireSeconds, TValueObj value)
         {
            Created = new DateTimeOffset(DateTime.Now).AddSeconds(expireSeconds);
            CacheValue = value;
         }

         public bool IsValid
         {
            get
            {
               return (Lock
                  || (Created > DateTime.Now)
                  );
            }
         }

         public void Touch(uint expireSeconds)
         {
            Created = new DateTimeOffset(DateTime.Now).AddSeconds(expireSeconds);
         }

         public bool Lock { private get; set; }

      }

      private readonly uint expireSeconds;
      private readonly Dictionary<TKey, ValueObject<TValue>> Cache;

      #endregion

      /// <summary>
      /// Constructor with the timout value
      /// </summary>
      /// <param name="expireSeconds">timeout cannot be -ve</param>
      /// <param name="useApiCallToRelease">When an function call is made then it will go check the staleness of the cache</param>
      /// <param name="comparer">The System.Collections.Generic.IEqualityComparer/T/ implementation to use when comparing keys, or null to use the default System.Collections.Generic.EqualityComparer/T/ for the type of the key.</param>
      /// <remarks>
      /// expiresecounds must be less than 14 hours otherwise the DateTimeOffset for each object will throw an exception
      /// </remarks>
      public CacheHelper(uint expireSeconds, bool useApiCallToRelease = true, IEqualityComparer<TKey> comparer = (IEqualityComparer<TKey>) null)
      {
         Cache = new Dictionary<TKey, ValueObject<TValue>>(comparer);
         this.expireSeconds = expireSeconds;
         useAPICallToRelease = useApiCallToRelease;
      }

      /// <summary>
      /// Value replacement and retrieval
      /// Will not throw an exception if the object is NOT found
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>
      public TValue this[TKey key]
      {
         get
         {
            lock (cacheLock)
            {
               ValueObject<TValue> value;
               if ( Cache.TryGetValue(key, out value) )
               {
                  if (value.IsValid)
                     return value.CacheValue;
                  // else
                  {
                     Cache.Remove(key);
                     if (useAPICallToRelease)
                        ThreadPool.QueueUserWorkItem(CheckStaleness);
                  }
               }
            }
            throw new KeyNotFoundException();

            // return default(TValue);
         }
         set
         {
            lock (cacheLock)
            {
               Cache[key] = new ValueObject<TValue>(expireSeconds, value);
            }
         }
      }

      /// <summary>
      /// Go through the cache and remove the stale items
      /// </summary>
      /// <remarks>
      /// This can be called from a thread, and is used when the useAPICallToRelease is true in the constructor
      /// </remarks>
      /// <param name="state">set to null</param>
      public void CheckStaleness(object state)
      {
         lock (cacheLock)
         {
            try
            {
               foreach (KeyValuePair<TKey, ValueObject<TValue>> i in Cache.Where(kvp => ((kvp.Value == null) || !kvp.Value.IsValid)).ToList())
               {
                  Cache.Remove(i.Key);
               }
            }
            catch { }
         }
      }

      /// <summary>
      /// Does the value exist at this key that has not timed out ?
      /// Will not throw an exception if the object is NOT found
      /// </summary>
      /// <param name="key"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      public bool TryGetValue(TKey key, out TValue value)
      {
         lock (cacheLock)
         {
            ValueObject<TValue> valueobj;
            if (Cache.TryGetValue(key, out valueobj))
            {
               if (valueobj.IsValid)
               {
                  value = valueobj.CacheValue;
                  return true;
               }
               // else
               {
                  Cache.Remove(key);
                  if (useAPICallToRelease)
                     ThreadPool.QueueUserWorkItem(CheckStaleness);
               }
            }
         }

         value = default(TValue);
         return false;
      }

      /// <summary>
      /// Remove the value
      /// </summary>
      /// <param name="key"></param>
      public void Remove(TKey key)
      {
         lock (cacheLock)
         {
            Cache.Remove(key);
         }
      }

      /// <summary>
      /// Used to prevent an object from being removed from the cache;
      /// e.g. when a file is open
      /// Will not throw an exception if the object is NOT found
      /// </summary>
      /// <param name="key"></param>
      /// <param name="state">true to lock</param>
      public void Lock(TKey key, bool state)
      {
         lock (cacheLock)
         {
            ValueObject<TValue> valueobj;
            if (Cache.TryGetValue(key, out valueobj))
            {
               valueobj.Lock = state;
               // If this is unlocking then assume that the target object will "be allowed" to be around for a while
               if (!state)
                  valueobj.Touch(expireSeconds);
            }
         }
      }

      /// <summary>
      /// Used to prevent an object from being removed from the cache;
      /// e.g. when a file is open
      /// Will not throw an exception if the object is NOT found
      /// </summary>
      /// <param name="key"></param>
      /// <param name="state">true to lock</param>
      public void Touch(TKey key)
      {
         lock (cacheLock)
         {
            ValueObject<TValue> valueobj;
            if (Cache.TryGetValue(key, out valueobj))
            {
               valueobj.Touch(expireSeconds);
            }
         }
      }
   }
}
