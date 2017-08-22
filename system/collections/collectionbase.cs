// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
// <OWNER>[....]</OWNER>
// 

namespace System.Collections {
    using System;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// 类的读/写集合中的项目从对象中派生的有用的基类
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class CollectionBase : IList {
        ArrayList list;

        /// <summary>
        /// 使用默认初始容量初始化 CollectionBase 类的新实例。
        /// </summary>
        protected CollectionBase() {
            list = new ArrayList();
        }
        
        /// <summary>
        /// 使用指定的容量初始化 CollectionBase 类的新实例。
        /// </summary>
        /// <param name="capacity"></param>
        protected CollectionBase(int capacity) {
            list = new ArrayList(capacity);
        }

        /// <summary>
        /// 获取一个 ArrayList，它包含 CollectionBase 实例中元素的列表。
        /// </summary>
        protected ArrayList InnerList { 
            get { 
                if (list == null)
                    list = new ArrayList();
                return list;
            }
        }

        /// <summary>
        /// 获取一个 IList，它包含 CollectionBase 实例中元素的列表。
        /// </summary>
        protected IList List {
            get { return (IList)this; }
        }

        /// <summary>
        /// 获取或设置 CollectionBase 可包含的元素数。
        /// </summary>
        [System.Runtime.InteropServices.ComVisible(false)]        
        public int Capacity {
            get {
                return InnerList.Capacity;
            }
            set {
                InnerList.Capacity = value;
            }
        }

        /// <summary>
        /// 获取包含在 CollectionBase 实例中的元素数。不能重写此属性。
        /// </summary>
        public int Count {
            get {
                return list == null ? 0 : list.Count;
            }
        }

        /// <summary>
        /// 从 CollectionBase 实例移除所有对象。不能重写此方法。
        /// </summary>
        public void Clear() {
            OnClear();//虚方法开始清除
            InnerList.Clear();//ArrayList内容清空
            OnClearComplete();//虚方法清除完毕
        }

        /// <summary>
        /// 移除 CollectionBase 实例的指定索引处的元素。此方法不可重写。
        /// </summary>
        /// <param name="index">要移除的元素的从零开始的索引。</param>
        /// <exception cref="ArgumentOutOfRangeException">index不符合范围</exception>
        public void RemoveAt(int index) {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            Object temp = InnerList[index];
            OnValidate(temp);//虚方法
            OnRemove(index, temp);//虚方法
            InnerList.RemoveAt(index);//ArrayList内容移除
            //如果移除失败，则重新插入值，并抛出异常
            try {
                OnRemoveComplete(index, temp);
            }
            catch {
                InnerList.Insert(index, temp);
                throw;
            }

        }

        /// <summary>
        /// 获取是否是只读的
        /// </summary>
        bool IList.IsReadOnly {
            get { return InnerList.IsReadOnly; }
        }

        /// <summary>
        /// 获取是否是固定大小的
        /// </summary>
        bool IList.IsFixedSize {
            get { return InnerList.IsFixedSize; }
        }

        /// <summary>
        /// 获取是否支持线程同步访问
        /// </summary>
        bool ICollection.IsSynchronized {
            get { return InnerList.IsSynchronized; }
        }

        /// <summary>
        /// 获取可用于同步对 ArrayList 的访问的对象。
        /// </summary>
        Object ICollection.SyncRoot {
            get { return InnerList.SyncRoot; }
        }

        /// <summary>
        /// 整个数组列表复制到一个兼容的一维数组,从目标的指定索引数组。
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        void ICollection.CopyTo(Array array, int index) {
            InnerList.CopyTo(array, index);
        }

        /// <summary>
        /// 获取IList索引出对象
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Object IList.this[int index] {
            get { 
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                return InnerList[index]; 
            }
            set { 
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                OnValidate(value);
                Object temp = InnerList[index];
                OnSet(index, temp, value); 
                InnerList[index] = value; 
                try {
                    OnSetComplete(index, temp, value);
                }
                catch {
                    InnerList[index] = temp; 
                    throw;
                }
            }
        }

        #region 显示接口实现
        /// <summary>
        /// 确定 CollectionBase 是否包含特定元素。
        /// </summary>
        /// <param name="value">要在 CollectionBase 中查找的 Object。</param>
        /// <returns>Type: System.Boolean如果 CollectionBase 包含指定的 value，则为 true；否则为 false。</returns>
        bool IList.Contains(Object value)
        {
            return InnerList.Contains(value);
        }

        /// <summary>
        /// 将对象添加到 CollectionBase 的结尾处。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int IList.Add(Object value)
        {
            OnValidate(value);
            OnInsert(InnerList.Count, value);
            int index = InnerList.Add(value);
            try
            {
                OnInsertComplete(index, value);
            }
            catch
            {
                InnerList.RemoveAt(index);
                throw;
            }
            return index;
        }


        /// <summary>
        /// 从 CollectionBase 中移除特定对象的第一个匹配项。
        /// </summary>
        /// <param name="value"></param>
        void IList.Remove(Object value)
        {
            OnValidate(value);
            int index = InnerList.IndexOf(value);
            if (index < 0) throw new ArgumentException(Environment.GetResourceString("Arg_RemoveArgNotFound"));
            OnRemove(index, value);
            InnerList.RemoveAt(index);
            try
            {
                OnRemoveComplete(index, value);
            }
            catch
            {
                InnerList.Insert(index, value);
                throw;
            }
        }

        /// <summary>
        /// 搜索指定的 Object，并返回整个 CollectionBase 中第一个匹配项的从零开始的索引。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int IList.IndexOf(Object value)
        {
            return InnerList.IndexOf(value);
        }

        /// <summary>
        /// 将元素插入 CollectionBase 的指定索引处。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        void IList.Insert(int index, Object value)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            OnValidate(value);
            OnInsert(index, value);
            InnerList.Insert(index, value);
            try
            {
                OnInsertComplete(index, value);
            }
            catch
            {
                InnerList.RemoveAt(index);
                throw;
            }
        }
        #endregion

        /// <summary>
        /// 返回循环访问 CollectionBase 实例的枚举器。
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return InnerList.GetEnumerator();
        } 


        /// <summary>
        /// 当在 CollectionBase 实例中设置值之前执行其他自定义进程。
        /// </summary>
        /// <param name="index">从零开始的索引，可在该位置找到 oldValue。</param>
        /// <param name="oldValue">要用 newValue 替换的值。</param>
        /// <param name="newValue">index 处的元素的新值。</param>
        protected virtual void OnSet(int index, Object oldValue, Object newValue) { 
        }

        /// <summary>
        /// 在向 CollectionBase 实例中插入新元素之前执行其他自定义进程。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        protected virtual void OnInsert(int index, Object value) { 
        }

        /// <summary>
        /// 当清除 CollectionBase 实例的内容时执行其他自定义进程
        /// </summary>
        protected virtual void OnClear() { 
        }

        /// <summary>
        /// 当从 CollectionBase 实例移除元素时执行其他自定义进程。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        protected virtual void OnRemove(int index, Object value) { 
        }

        /// <summary>
        /// 当验证值时执行其他自定义进程。
        /// </summary>
        /// <param name="value"></param>
        protected virtual void OnValidate(Object value) { 
            if (value == null) throw new ArgumentNullException("value");
            Contract.EndContractBlock();
        }

        /// <summary>
        /// 当在 CollectionBase 实例中设置值后执行其他自定义进程。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected virtual void OnSetComplete(int index, Object oldValue, Object newValue) { 
        }

        /// <summary>
        /// 在向 CollectionBase 实例中插入新元素之后执行其他自定义进程。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        protected virtual void OnInsertComplete(int index, Object value) { 
        }

        /// <summary>
        /// 在清除 CollectionBase 实例的内容之后执行其他自定义进程。
        /// </summary>
        protected virtual void OnClearComplete() { 
        }

        /// <summary>
        /// 在从 CollectionBase 实例中移除元素之后执行其他自定义进程。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        protected virtual void OnRemoveComplete(int index, Object value) { 
        }
    
    }

}
