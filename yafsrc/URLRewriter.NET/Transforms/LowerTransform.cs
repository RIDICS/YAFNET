// UrlRewriter - A .NET URL Rewriter module
// Version 2.0
//
// Copyright 2007 Intelligencia
// Copyright 2007 Seth Yates
// 

namespace Intelligencia.UrlRewriter.Transforms
{
    using System;
    using System.Threading;

    using Intelligencia.UrlRewriter.Utilities;

    /// <summary>
    /// Transforms the input to lower case.
    /// </summary>
    public sealed class LowerTransform : IRewriteTransform
    {
        /// <summary>
        /// Applies a transformation to the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The transformed string.</returns>
        public string ApplyTransform(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            return input.ToLower(Thread.CurrentThread.CurrentCulture);
        }

        /// <summary>
        /// The name of the action.
        /// </summary>
        public string Name => Constants.TransformLower;
    }
}