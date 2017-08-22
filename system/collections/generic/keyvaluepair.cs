// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  KeyValuePair
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: Generic key-value pair for dictionary enumerators.
**
** 
===========================================================*/
namespace System.Collections.Generic {
    
    using System;
    using System.Text;

    // A KeyValuePair holds a key and a value from a dictionary.
    // It is used by the IEnumerable<T> implementation for both IDictionary<TKey, TValue>
    // and IReadOnlyDictionary<TKey, TValue>.
    /// <summary>
    /// 定义可设置或检索的键/值对。
    /// </summary>
    /// <typeparam name="TKey">键的类型。</typeparam>
    /// <typeparam name="TValue">值的类型。</typeparam>
    [Serializable]
    public struct KeyValuePair<TKey, TValue> {
        private TKey key;
        private TValue value;

        public KeyValuePair(TKey key, TValue value) {
            this.key = key;
            this.value = value;
        }

        public TKey Key {
            get { return key; }
        }

        public TValue Value {
            get { return value; }
        }

        /// <summary>
        /// 使用键和值的字符串表示形式返回 System.Collections.Generic.KeyValuePair<TKey,TValue> 的字符串表示形式。
        /// </summary>
        /// <returns>System.Collections.Generic.KeyValuePair<TKey,TValue> 的字符串表示形式，它包括键和值的字符串表示形式。</returns>
        public override string ToString() {
            StringBuilder s = StringBuilderCache.Acquire();
            s.Append('[');
            if( Key != null) {
                s.Append(Key.ToString());
            }
            s.Append(", ");
            if( Value != null) {
               s.Append(Value.ToString());
            }
            s.Append(']');
            return StringBuilderCache.GetStringAndRelease(s);
        }
    }
}
