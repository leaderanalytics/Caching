using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LeaderAnalytics.Caching
{
    public class MultiIndexCache<T>
    {
        private object locker = new object();
        private readonly ICache<string> keys;
        private readonly ICache<T> values;
        private readonly string[] indexSignatures;
        private readonly int indexCount;
        private readonly Func<T, string>[] indexes;
        public int ObjectCount { get => values.Count; }
        public int KeyCount { get => keys.Count; }

        public MultiIndexCache(params Expression<Func<T, string>>[] indexes)
        {
            if(indexes == null)
                throw new ArgumentNullException("indexes");

            this.indexes = indexes.Select(x => x.Compile()).ToArray();
            indexSignatures = indexes.Select(x => x.Body.ToString()).ToArray();
            indexCount = indexes.Count();
            keys = new Cache<string>();
            values = new Cache<T>();
        }

        /// <summary>
        /// Find an object in the cache using an expression that matches an index.  This method is slower than Get(int indexID, string lookup).
        /// </summary>
        /// <param name="indexExpr">An expression that matches an index.</param>
        /// <returns></returns>
        public T Get(Expression<Func<T, bool>> indexExpr)
        {
            T result = default(T);
            KeyValuePair<int, string>  query = ParseExpression(indexExpr);

            string link = keys.Get(query.Key.ToString() + query.Value);

            if (link != null)
                result = values.Get(link);
            
            return result;
        }

        /// <summary>
        /// Finds an object in the cache using an index specified by the indexID parameter.  Faster than Get(Expression<Func<T, bool>> indexExpr).
        /// </summary>
        /// <param name="indexID">An integer that matches the ordinal position of one of the indexes passed into the constructor of this class.</param>
        /// <param name="lookup">A value to search for.</param>
        /// <returns></returns>
        public T Get(int indexID, string lookup)
        {
            T result = default(T);

            // Use one of the indexes to get a string which is than used to get an instance of T
            string link = keys.Get(indexID.ToString() + lookup);

            if (link != null)
                result = values.Get(link);

            return result;
        }

        /// <summary>
        /// Adds an object to the cache.  
        /// </summary>
        /// <param name="obj"></param>
        public void Set(T obj)
        {
            lock (locker)
            {
                Remove(obj);
                string link = GuidEncoder.EncodedGuid();
                int counter = 0;
                int startKeyCount = keys.Count;
                int startValueCount = values.Count;

                foreach (Func<T, string> func in indexes)
                    keys.Set(counter++ + func(obj), link);

                int endKeyCount = keys.Count;

                // if all keys are unique we should have added exactly [indexCount] items to the keys cache.
                if (startKeyCount + indexCount != endKeyCount && startKeyCount != endKeyCount)
                    throw new Exception($"Duplicate key error. {indexCount} indexes are defined however only { endKeyCount - startKeyCount} keys were created.");

                values.Set(link, obj);

                int endValueCount = values.Count;

                if (endValueCount != startValueCount + 1)
                    throw new Exception($"Incorrect number of values added. 1 object should be added however {startValueCount - endValueCount} objects were added.");
            }
        }

        /// <summary>
        /// Removes an object and it's keys from the cache.  The object is searched for using the first index.
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(T obj)
        {
            string key = indexes[0](obj);
            Remove(key);
        }

        /// <summary>
        /// Removes an object and it's keys from the cache.  The object is searched for using the first index.
        /// </summary>
        /// <param name="key">A value that can be found using the first index.</param>
        public void Remove(string key)
        {
            lock(locker)
            {
                key = "0" + key;
                string link = keys.Get(key);

                if (link == null)
                    return;

                int startKeyCount = keys.Count;

                T obj = values.Get(link);

                if (obj == null)
                    throw new Exception($"Internal consistency error while removing an object.  A key for an object with id {key} was found in the keys cache but no matching object with key {link} was found in the values cache.");

                for (int i = 0; i < indexCount; i++)
                    keys.Remove(i.ToString() + indexes[i](obj));

                int endKeyCount = keys.Count;
                int startValueCount = values.Count;
                values.Remove(link);
                int endValueCount = values.Count;

                if (endKeyCount + indexCount != startKeyCount)
                    throw new Exception($"Incorrect number of keys removed. {indexCount} indexes are defined however only {startKeyCount - endKeyCount} keys were removed.");

                if(endValueCount != startValueCount -1)
                    throw new Exception($"Incorrect number of values removed. 1 object should be removed however {startValueCount - endValueCount} objects were removed.");
            }
        }

        public void Purge()
        {
            lock (locker)
            {
                keys.Purge();
                values.Purge();
            }
        }

        private KeyValuePair<int, string> ParseExpression(LambdaExpression expr)
        {
            int index = -1;
            string key = null;
            string constant = null;
            BinaryExpression body = (BinaryExpression)expr.Body;
            Expression term = null;

            // try to find the expression that matches one of the indexes
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                    term = body.Left;
                else
                    term = body.Right;

                if (index == -1)
                {
                    string query = (term as Expression).ToString();

                    for (int j = 0; j < indexCount; j++)
                    {
                        if (query == indexSignatures[j]) 
                        {
                            index = j;
                            break;
                        }
                    }

                    if (index > -1)
                        continue;
                }

                // try to find the expression that returns a value to search for
                switch (term.NodeType)
                {
                    case ExpressionType.Constant:
                        constant = ((ConstantExpression)term).Value.ToString();
                        break;
                    case ExpressionType.MemberAccess:
                        try
                        {
                            constant = Expression.Lambda<Func<string>>(term).Compile()();
                        }
                        catch
                        {
                            constant = null;
                        }
                        break;
                    case ExpressionType.Invoke:
                    case ExpressionType.Call:
                        try
                        {
                            constant = Expression.Lambda(term).Compile().DynamicInvoke().ToString();
                        }
                        catch
                        {
                            constant = null;
                        }
                        break;
                    default:
                        key = (term as Expression).ToString();
                        break;
                }

            }

            if (index == -1)
                throw new InvalidOperationException($"An index matching {body.ToString()} was not found.");

            return new KeyValuePair<int, string>(index, constant);
        }
    }

    internal static class GuidEncoder
    {
        // Thanks to Mads
        // https://madskristensen.net/blog/A-shorter-and-URL-friendly-GUID

        public static string EncodedGuid()
        {
            return Encode(Guid.NewGuid());
        }

        public static string Encode(string guidText)
        {
            Guid guid = new Guid(guidText);
            return Encode(guid);
        }

        public static string Encode(Guid guid)
        {
            string enc = Convert.ToBase64String(guid.ToByteArray());
            enc = enc.Replace("/", "_");
            enc = enc.Replace("+", "-");
            return enc.Substring(0, 22);
        }

        public static Guid Decode(string encoded)
        {
            encoded = encoded.Replace("_", "/");
            encoded = encoded.Replace("-", "+");
            byte[] buffer = Convert.FromBase64String(encoded + "==");
            return new Guid(buffer);
        }
    }
}
