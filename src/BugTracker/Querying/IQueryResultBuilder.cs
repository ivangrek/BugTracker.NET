/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying
{
    using System;
    using System.Linq.Expressions;

    public interface IQueryResultBuilder<TResult>
        where TResult : class, IResult
    {
        IQueryResultBuilder<TResult> WithValue<TValue>(Expression<Func<TResult, TValue>> propertyExpression,
            TValue value);

        TResult Build();
    }

    public interface IQueryResultBuilder2<TResult>
        where TResult : class
    {
        IQueryResultBuilder2<TResult> WithValue<TValue>(Expression<Func<TResult, TValue>> propertyExpression,
            TValue value);

        TResult Build();
    }
}