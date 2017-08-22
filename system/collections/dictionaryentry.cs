// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  DictionaryEntry
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: Return Value for IDictionaryEnumerator::GetEntry
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    // A DictionaryEntry holds a key and a value from a dictionary.
    // It is returned by IDictionaryEnumerator::GetEntry().
    // DictionaryEntry持有一个键和一个值从一个字典。这是返回IDictionaryEnumerator:GetEntry()。
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public struct DictionaryEntry
    {
        private Object _key;
        private Object _value;
    
        /// <summary>
        /// 构造一个新的DictionaryEnumerator通过设置适当的键和值的字段。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public DictionaryEntry(Object key, Object value) {
            _key = key;
            _value = value;
        }

        public Object Key {
            get {
                return _key;
            }
            
            set {
                _key = value;
            }
        }

        public Object Value {
            get {
                return _value;
            }

            set {
                _value = value;
            }
        }
    }
}
