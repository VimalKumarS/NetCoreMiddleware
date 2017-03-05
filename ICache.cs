using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WebApplication_LearnMiddleware
{
   public interface ICache
    {
        TResult InvokeCached<TResult>(
           Expression<Func<TResult>> expression,
           CachePolicy policy);
    }
}
