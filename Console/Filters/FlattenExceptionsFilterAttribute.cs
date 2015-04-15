// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FlattenExceptionsFilterAttribute.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Console.Filters
{
    using System;
    using System.Web.Http.Filters;

    /// <summary>
    /// The flatten exceptions attribute.
    /// </summary>
    public class FlattenExceptionsFilterAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Flattens exceptions thrown during action invocation.
        /// </summary>
        /// <param name="ctx">
        /// The action execution context.
        /// </param>
        public override void OnActionExecuted(HttpActionExecutedContext ctx)
        {
            var result = ctx.Exception;
            
            var exception = result as AggregateException;
            while (exception != null)
            {
                exception.Flatten();

                if (exception.InnerException == null)
                {
                    break;
                }
                
                result = exception.InnerException;
                exception = result as AggregateException;
            }

            ctx.Exception = result;
        }
    }
}
