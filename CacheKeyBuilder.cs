using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication_LearnMiddleware
{
    public sealed class CacheKeyBuilder
    {
        private static readonly string NullString = Guid.NewGuid().ToString();
        private readonly StringBuilder builder = new StringBuilder();

        public CacheKeyBuilder By(object value)
        {
            this.builder.Append('{'); // wrap each value in curly braces
            if (value == null)
            {
                this.builder.Append(NullString);
            }

            DateTime? dateTimeValue;
            IConvertible convertibleValue;
            Type typeValue;
            IEnumerable enumerableValue;
            ICacheKey cacheKeyValue;

            // DateTime is convered by IConvertible, but the default 
            // ToString() implementation doesn't have enough 
            // granularity to distinguish between unequal DateTimes
            if ((dateTimeValue = value as DateTime?).HasValue)
            {
                this.builder.Append(dateTimeValue.Value.Ticks);
            }
            else if ((convertibleValue = value as IConvertible) != null)
            {
                this.builder.Append(
                                convertibleValue.ToString(CultureInfo.InvariantCulture)
                            );
            }
            else if ((typeValue = value as Type) != null)
            {
                this.builder.Append(typeValue.Name);
            }
            else if ((cacheKeyValue = value as ICacheKey) != null)
            {
                cacheKeyValue.BuildCacheKey(this);
            }
            else if ((enumerableValue = value as IEnumerable) != null)
            {
                foreach (object element in enumerableValue)
                {
                    this.By(element);
                }
            }
            else
            {
                throw new ArgumentException(value.GetType() + " cannot be a cache key");
            }

            this.builder.Append('}');
            return this;
        }

        public override string ToString()
        {
            return this.builder.ToString();
        }
    }

    /// <summary>
    /// This interface allows custom types to be cache keys
    /// </summary>
    public interface ICacheKey
    {
        void BuildCacheKey(CacheKeyBuilder builder);
    }
}
