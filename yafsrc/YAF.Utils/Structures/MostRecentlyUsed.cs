/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
* Copyright (C) 2014-2017 Ingo Herbote
 * http://www.yetanotherforum.net/
 * 
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * http://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
namespace YAF.Utils.Structures
{
  #region Using

  using System;
  using System.Collections;
  using System.Collections.Specialized;
  using System.Text;

  using YAF.Types;

  #endregion

  /// <summary>
  /// The most recently used.
  /// </summary>
  [Serializable]
  public class MostRecentlyUsed : DictionaryBase
  {
    #region Constants and Fields

    /// <summary>
    /// The m_link to key.
    /// </summary>
    private readonly HybridDictionary m_linkToKey = new HybridDictionary(); // LinkItem -> key

    /// <summary>
    /// The m_list.
    /// </summary>
    private readonly DoubleLinkedList m_list = new DoubleLinkedList();

    /// <summary>
    /// The m_max.
    /// </summary>
    private uint m_max = 50;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MostRecentlyUsed"/> class. 
    ///   Default constructor for the most recently used items using the default size (50)
    /// </summary>
    public MostRecentlyUsed()
    {
      // TODO: Add constructor logic here
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MostRecentlyUsed"/> class. 
    /// Construct a most recently used items list with the maximum number of items
    ///   allowed in the list.
    /// </summary>
    /// <param name="maxItems">
    /// Maximum number of items allowed
    /// </param>
    public MostRecentlyUsed(uint maxItems)
    {
      this.m_max = maxItems;
    }

    #endregion

    #region Delegates

    /// <summary>
    /// The purged from cache delegate.
    /// </summary>
    /// <param name="key">
    /// The key.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public delegate void PurgedFromCacheDelegate(object key, object value);

    #endregion

    #region Events

    /// <summary>
    ///   Event that is fired when an item falls outside of the cache
    /// </summary>
    public event PurgedFromCacheDelegate OnPurgedFromCache;

    #endregion

    #region Properties

    /// <summary>
    ///   The maximum capacity of the list
    /// </summary>
    public uint Capacity
    {
      get
      {
        return this.m_max;
      }

      set
      {
        this.m_max = value;
      }
    }

    /// <summary>
    /// Gets Keys.
    /// </summary>
    public ICollection Keys
    {
      get
      {
        return this.Dictionary.Keys;
      }
    }

    /// <summary>
    /// Gets Values.
    /// </summary>
    public ICollection Values
    {
      get
      {
        return this.Dictionary.Values;
      }
    }

    #endregion

    #region Indexers

    /// <summary>
    /// The this.
    /// </summary>
    /// <param name="key">
    /// The key.
    /// </param>
    public object this[[NotNull] object key]
    {
      get
      {
        CodeContracts.VerifyNotNull(key, "key");

        var item = (DoubleLinkedList.LinkItem)this.Dictionary[key];

        if (item == null)
        {
          return null;
        }

        this.m_list.MoveToHead(item);

        return item.Item;
      }

      set
      {
        CodeContracts.VerifyNotNull(key, "key");
        CodeContracts.VerifyNotNull(value, "value");

        DoubleLinkedList.LinkItem link = null;

        if (this.Dictionary.Contains(key))
        {
          link = (DoubleLinkedList.LinkItem)this.Dictionary[key];
          link.Item = value;

          this.m_list.MoveToHead(link);

          this.Dictionary[key] = link;

          // Keep a reverse index from the link to the key
          this.m_linkToKey[link] = key;
        }
        else
        {
          this.Add(key, value);
        }
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// The add.
    /// </summary>
    /// <param name="key">
    /// The key.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public void Add([NotNull] object key, [NotNull] object value)
    {
      CodeContracts.VerifyNotNull(key, "key");
      CodeContracts.VerifyNotNull(value, "value");

      DoubleLinkedList.LinkItem link = this.m_list.Prepend(value);

      this.Dictionary.Add(key, link);

      // Keep a reverse index from the link to the key
      this.m_linkToKey[link] = key;
    }

    /// <summary>
    /// The contains.
    /// </summary>
    /// <param name="key">
    /// The key.
    /// </param>
    /// <returns>
    /// The contains.
    /// </returns>
    public bool Contains([NotNull] object key)
    {
      CodeContracts.VerifyNotNull(key, "key");

      bool hasKey = this.Dictionary.Contains(key);

      // Update the reference for this link
      if (hasKey)
      {
        this.m_list.MoveToHead((DoubleLinkedList.LinkItem)this.Dictionary[key]);
      }

      return hasKey;
    }

    /// <summary>
    /// The remove.
    /// </summary>
    /// <param name="key">
    /// The key.
    /// </param>
    public void Remove([NotNull] object key)
    {
      CodeContracts.VerifyNotNull(key, "key");

      var link = (DoubleLinkedList.LinkItem)this.Dictionary[key];

      this.Dictionary.Remove(key);

      if (link != null)
      {
        this.m_list.RemoveLink(link);

        // Keep a reverse index from the link to the key
        this.m_linkToKey.Remove(link);
      }
    }

    /// <summary>
    /// The to string.
    /// </summary>
    /// <returns>
    /// The to string.
    /// </returns>
    public override string ToString()
    {
      var buff = new StringBuilder(Convert.ToInt32(this.m_max));

      buff.Append("[");

      foreach (object item in this.m_list)
      {
        if (buff.Length > 1)
        {
          buff.Append(", ");
        }

        buff.Append(item.ToString());
      }

      buff.Append("]");

      return buff.ToString();
    }

    #endregion

    #region Methods

    /// <summary>
    /// The on insert.
    /// </summary>
    /// <param name="key">
    /// The key.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    protected override void OnInsert([NotNull] object key, [NotNull] object value)
    {
      CodeContracts.VerifyNotNull(key, "key");
      CodeContracts.VerifyNotNull(value, "value");

      if (this.Dictionary.Keys.Count >= this.m_max)
      {
        // Purge an item from the cache
        DoubleLinkedList.LinkItem tail = this.m_list.TailLink;

        if (tail != null)
        {
          object purgeKey = this.m_linkToKey[tail];

          if (purgeKey != null)
          {
            // Fire the event
            if (this.OnPurgedFromCache != null && this.OnPurgedFromCache.GetInvocationList().Length > 0)
            {
              this.OnPurgedFromCache(purgeKey, tail.Item);
            }

            this.Remove(purgeKey);
          }
        }
      }
    }

    #endregion
  }
}