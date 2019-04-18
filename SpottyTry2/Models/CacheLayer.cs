using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;


namespace SpottyTry2.Models
{
    public class CacheLayer
    {
        static readonly ObjectCache Cache = MemoryCache.Default;
        /// <summary>
        /// Retrieve Cached Item
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Name of cached item</param>
        /// <returns>cached item as type</returns>
        public static T Get<T>(string key) where T : class
        {
            try
            {
                return (T)Cache[key];
            }
            catch
            {

                return null;
            }
        }

        public static void Add(object objectToCache, string key)
        {
            Cache.Add(key, objectToCache, DateTime.Now.AddDays(1));
        }
    }
}