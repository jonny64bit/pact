﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pact.Kendo
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Kendo data source support for soft delete, text filtering and sorting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="kendoDataRequest"></param>
        /// <returns></returns>
        public static IQueryable<T> Kendo<T>(this IQueryable<T> source, KendoDataRequest kendoDataRequest) where T : class
        {
            //Remove soft delete
            source = SoftDelete(source);

            //Kendo text filter
            source = TextFilter(source, kendoDataRequest.TextFilter);

            //Sort
            if (kendoDataRequest.Sort != null)
            {
                if (kendoDataRequest.Sort.Count > 0)
                    source = source.OrderBy(kendoDataRequest.Sort.First().ToString());
                if (kendoDataRequest.Sort.Count > 1)
                    source = source.ThenBy(kendoDataRequest.Sort[0].ToString());
            }

            return source;
        }

        /// <summary>
        /// Kendo data source support for soft delete, text filtering and sorting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="kendoDataRequest"></param>
        /// <returns></returns>
        public static async Task<JsonResult> KendoResultAsync<T>(this IQueryable<T> source, KendoDataRequest kendoDataRequest) where T : class
        {
            //Execute
            var items = await source.ToListAsync();

            //Get the count for kendo ui grids
            var count = items.Count;

            //Skip/Take
            items = items.Skip(kendoDataRequest.Skip).Take(kendoDataRequest.Take).ToList();

            return new JsonResult(new KendoResult<T> { Result = "OK", Records = items, Count = count });
        }

        /// <summary>
        /// Removes soft delete items if the "SoftDelete" property is present and true
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IQueryable<T> SoftDelete<T>(this IQueryable<T> source) where T : class
        {
            var softDeleteProp = typeof(T).GetProperty("SoftDelete", typeof(bool));
            if (softDeleteProp != null)
            {
                var item = Expression.Parameter(typeof(T), "x");
                var prop = Expression.Property(item, "SoftDelete");
                var falseConstant = Expression.Constant(false);
                var equal = Expression.Equal(prop, falseConstant);
                var lambda = Expression.Lambda<Func<T, bool>>(equal, item);
                source = source.Where(lambda);
            }

            return source;
        }

        /// <summary>
        /// Filters enumerable using the search term.
        /// If <see cref="FilterAttribute"/> & <see cref="IgnoreFilterAttribute"/> are present on the class these are used to determine what properties to filter on.
        /// If no filter arbitrates are present it checks all string fields.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public static IQueryable<T> TextFilter<T>(this IQueryable<T> source, string search) where T : class
        {
            if (string.IsNullOrWhiteSpace(search))
                return source;

            search = search.Trim().ToLower();

            //Look for specifically set properties to filter on
            var propertyInfos = typeof(T).GetProperties().Where(w => w.PropertyType == typeof(string) && w.CustomAttributes.Count(c=>c.AttributeType == typeof(FilterAttribute)) > 0 && w.CustomAttributes.Count(c => c.AttributeType == typeof(NotMappedAttribute)) < 1).ToList();

            //If none found use all strings which are not filtered out.
            if(propertyInfos == null || propertyInfos.Count < 1)
                propertyInfos = typeof(T).GetProperties().Where(w => w.PropertyType == typeof(string) && w.CustomAttributes.Count(c=>c.AttributeType == typeof(NotMappedAttribute) || c.AttributeType == typeof(IgnoreFilterAttribute)) < 1).ToList();

            var item = Expression.Parameter(typeof(T), "x");
            var searchConstant = Expression.Constant("%" + search + "%");
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });

            if (likeMethod == null)
                throw new Exception("Contains method not found on string in the .net framework");

            var methodCallExpressions = new List<MethodCallExpression>();
            foreach (var propertyInfo in propertyInfos)
            {
                var property = Expression.Property(item, propertyInfo);
                var searchExpression = Expression.Call(null, likeMethod, Expression.Constant(EF.Functions), property, searchConstant);
                methodCallExpressions.Add(searchExpression);
            }

            if (methodCallExpressions.Count < 0)
                return source;
            if (methodCallExpressions.Count == 1)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(methodCallExpressions.First(), item);
                return source.Where(lambda);
            }

            Expression expressionOr = null;
            foreach (var methodCallExpression in methodCallExpressions)
            {
                if (methodCallExpressions.First() == methodCallExpression)
                    continue;
                if (methodCallExpressions.IndexOf(methodCallExpression) == 1)
                    expressionOr = Expression.OrElse(methodCallExpressions.First(), methodCallExpression);
                else if (expressionOr != null)
                    expressionOr = Expression.OrElse(expressionOr, methodCallExpression);
            }

            if (expressionOr != null)
            {
                var lambdaOr = Expression.Lambda<Func<T, bool>>(expressionOr, item);
                return source.Where(lambdaOr);
            }

            return source;
        }
    }
}
