using System;
using System.Collections.Generic;
using System.Linq;

namespace live_trivia.Utilities;

public class CollectionProjector<TSource>
    where TSource : BaseEntity
{
    public IReadOnlyList<TResult> Project<TResult, TKey>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> orderBy,
        Func<TSource, TResult> selector)
        where TResult : class, new()
        where TKey : notnull, IComparable<TKey>
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        return source
            .OrderByDescending(orderBy)
            .Select(item => selector(item) ?? new TResult())
            .ToList();
    }
}

