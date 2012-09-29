/* Yet Another Forum.NET
 * Copyright (C) 2006-2012 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */
namespace YAF.Types.Interfaces.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    ///     The DbFunctionSession interface.
    /// </summary>
    public interface IDbFunctionSession : IDbFunction, IDisposable
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the current Db Transaction
        /// </summary>
        IDbTransaction DbTransaction { get; }

        /// <summary>
        /// Gets the get reader.
        /// </summary>
        dynamic GetReader { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get typed.
        /// </summary>
        /// <param name="getFunction">
        /// The get function. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// The <see cref="IList"/> . 
        /// </returns>
        IList<T> GetTyped<T>(Func<dynamic, object> getFunction)
            where T : new();

        #endregion
    }
}