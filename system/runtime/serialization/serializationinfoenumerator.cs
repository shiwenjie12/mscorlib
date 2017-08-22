// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: SerializationInfoEnumerator
**
**
** 提供一种对格式化程序友好的机制，用于分析 SerializationInfo 中的数据。此类不能被继承。
**
============================================================*/
namespace System.Runtime.Serialization {
    using System;
    using System.Collections;
    using System.Diagnostics.Contracts;

    //
    // The tuple returned by SerializationInfoEnumerator.Current.
    //
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct SerializationEntry {
        private Type   m_type;
        private Object m_value;
        private String m_name;

        public Object Value {
            get {
                return m_value;
            }
        }

        public String Name {
            get {
                return m_name;
            }
        }

        public Type ObjectType {
            get {
                return m_type;
            }
        }

        internal SerializationEntry(String entryName, Object entryValue, Type entryType) {
            m_value = entryValue;
            m_name = entryName;
            m_type = entryType;
        }
    }

    //
    // 一个简单的值存储在SerializationInfo枚举器。
    // 这并不快照值,它只是使指针成员变量的SerializationInfo创建它。
    //
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SerializationInfoEnumerator : IEnumerator {
        String[] m_members;
        Object[] m_data;
        Type[]   m_types;
        int      m_numItems;
        int      m_currItem;
        bool     m_current;

        /// <summary>
        /// 序列化信息枚举
        /// </summary>
        /// <param name="members">成员</param>
        /// <param name="info">信息</param>
        /// <param name="types">格式</param>
        /// <param name="numItems">项目数</param>
        internal SerializationInfoEnumerator(String[] members, Object[] info, Type[] types, int numItems) {
            Contract.Assert(members!=null, "[SerializationInfoEnumerator.ctor]members!=null");
            Contract.Assert(info!=null, "[SerializationInfoEnumerator.ctor]info!=null");
            Contract.Assert(types!=null, "[SerializationInfoEnumerator.ctor]types!=null");
            Contract.Assert(numItems>=0, "[SerializationInfoEnumerator.ctor]numItems>=0");
            Contract.Assert(members.Length>=numItems, "[SerializationInfoEnumerator.ctor]members.Length>=numItems");
            Contract.Assert(info.Length>=numItems, "[SerializationInfoEnumerator.ctor]info.Length>=numItems");
            Contract.Assert(types.Length>=numItems, "[SerializationInfoEnumerator.ctor]types.Length>=numItems");

            m_members = members;
            m_data = info;
            m_types = types;
            //MoveNext语义更容易如果我们执行[0 . .m_numItems)是有效的条目，在枚举器,因此我们减去1。
            m_numItems = numItems-1;
            m_currItem = -1;
            m_current = false;
        }

        /// <summary>
        /// 将枚举更新到下一项
        /// 实现：IEnumerator.MoveNext()
        /// </summary>
        /// <returns>如果找到新的元素，则为 true；否则为 false。</returns>
        public bool MoveNext() {
            if (m_currItem<m_numItems) {
                m_currItem++;
                m_current = true;
            } else {
                m_current = false;
            }
            return m_current;
        }

        /// <internalonly/>
        Object IEnumerator.Current { //Actually returns a SerializationEntry
            get {
                if (m_current==false) {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
                return (Object)(new SerializationEntry(m_members[m_currItem], m_data[m_currItem], m_types[m_currItem]));
            }
        }


        public SerializationEntry Current { //Actually returns a SerializationEntry
            get {
                if (m_current==false) {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
                return (new SerializationEntry(m_members[m_currItem], m_data[m_currItem], m_types[m_currItem]));
            }
        }

        /// <summary>
        /// 将枚举数重置为第一项。
        /// </summary>
        public void Reset() {
            m_currItem = -1;
            m_current = false;
        }

        /// <summary>
        /// 获取当前所检查的项的名称。
        /// </summary>
        public String Name {
            get {
                if (m_current==false) {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
                return m_members[m_currItem];
            }
        }

        /// <summary>
        /// 获取当前所检查的项的值。
        /// </summary>
        public Object Value {
            get {
                if (m_current==false) {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
                return m_data[m_currItem];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Type ObjectType {
            get {
                if (m_current==false) {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
                return m_types[m_currItem];
            }
        }
    }
}
