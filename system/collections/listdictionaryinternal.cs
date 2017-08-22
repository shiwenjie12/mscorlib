using System.Diagnostics.Contracts;
// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  ListDictionaryInternal
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: List for exceptions.
**
** 
===========================================================*/

namespace System.Collections {
    ///    This is a simple implementation of IDictionary using a singly linked list. This
    ///    will be smaller and faster than a Hashtable if the number of elements is 10 or less.
    ///    This should not be used if performance is important for large numbers of elements.
    ///    这是一个简单的实现使用一个单链表IDictionary。这将是更小,比一个Hashtable如果元素的数量是10或更少。不应使用如果大量元素的性能是很重要的。
    [Serializable]
    internal class ListDictionaryInternal: IDictionary {
        DictionaryNode head;
        int version;
        int count;
        [NonSerialized]
        private Object _syncRoot;

        public ListDictionaryInternal() {
        }

        public Object this[Object key]
        {
            get
            {
                if(key == null)
                {
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                }
                Contract.EndContractBlock();
                DictionaryNode node = head;

                while(node != null)
                {
                    if(node.key.Equals(key))
                    {
                        return node.value;
                    }
                    node = node.next;
                }
                return null;
            }
            set
            {
                if(key == null)
                {
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                }
                Contract.EndContractBlock();

#if FEATURE_SERIALIZATION
                if (!key.GetType().IsSerializable)
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "key");
                if((value !=null)&&(!value.GetType().IsSerializable))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "value");
                }
#endif
                version++;
                DictionaryNode last = null;
                DictionaryNode node;
                for(node = head; node != null; node = node.next)
                {
                    if(node.key.Equals(key))
                    {
                        break;
                    }
                    last = node;
                }

                if(node!= null)
                {
                    node.value = value;
                    return;
                }

                DictionaryNode newNode = new DictionaryNode();
                newNode.key = key;
                newNode.value = value;
                if(last!= null)
                {
                    last.next = newNode;
                }
                else
                {
                    head = newNode;
                }
                count++;
            }
        }

//        public Object this[Object key] {
//            get {
//                if (key == null) {
//                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
//                }
//                Contract.EndContractBlock();
//                DictionaryNode node = head;

//                while (node != null) {
//                    if ( node.key.Equals(key) ) {
//                        return node.value;
//                    }
//                    node = node.next;
//                }
//                return null;
//            }
//            set {
//                if (key == null) {
//                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
//                }
//                Contract.EndContractBlock();

//#if FEATURE_SERIALIZATION
//                if (!key.GetType().IsSerializable)                 
//                    throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "key");                    

//                if( (value != null) && (!value.GetType().IsSerializable ) )
//                    throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "value");                    
//#endif
                
//                version++;
//                DictionaryNode last = null;
//                DictionaryNode node;
//                for (node = head; node != null; node = node.next) {
//                    if( node.key.Equals(key) ) {
//                        break;
//                    } 
//                    last = node;
//                }
//                if (node != null) {
//                    // Found it
//                    node.value = value;
//                    return;
//                }
//                // 没有节点，我们就加入一个新的节点
//                DictionaryNode newNode = new DictionaryNode();
//                newNode.key = key;
//                newNode.value = value;
//                if (last != null) {
//                    last.next = newNode;
//                }
//                else {
//                    head = newNode;
//                }
//                count++;
//            }
//        }

        public int Count {
            get {
                return count;
            }
        }   

        public ICollection Keys {
            get {
                return new NodeKeyValueCollection(this, true);
            }
        }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        public bool IsFixedSize {
            get {
                return false;
            }
        }

        public bool IsSynchronized {
            get {
                return false;
            }
        }

        //public Object SyncRoot {
        //    get {
        //        if( _syncRoot == null) {
        //            System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);    
        //        }
        //        return _syncRoot; 
        //    }
        //}

        public Object SyncRoot
        {
            get
            {
                if(_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        public ICollection Values {
            get {
                return new NodeKeyValueCollection(this, false);
            }
        }

        /// <summary>
        /// 将键值对添加到字典列表中（Object this[Object key] get 相似但是增加了对键和值的验证）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void Add(Object key, Object value) {
            if (key == null) {
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            }
            Contract.EndContractBlock();

#if FEATURE_SERIALIZATION
            if (!key.GetType().IsSerializable)                 
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "key" );                    

            if( (value != null) && (!value.GetType().IsSerializable) )
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "value");                    
#endif
            
            version++;
            DictionaryNode last = null;
            DictionaryNode node;
            for (node = head; node != null; node = node.next) {//如果有相同的键，将会抛出参数异常
                if (node.key.Equals(key)) {
                    throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate__", node.key, key));
                } 
                last = node;
            }
            if (node != null) {
                // Found it
                node.value = value;
                return;
            }
            // 如果未发现，则添加一个新的节点
            DictionaryNode newNode = new DictionaryNode();
            newNode.key = key;
            newNode.value = value;
            if (last != null) {
                last.next = newNode;
            }
            else {
                head = newNode;
            }
            count++;
        }

        /// <summary>
        /// 将字典队列清除
        /// 数量清空
        /// head链式清空
        /// </summary>
        public void Clear() {
            count = 0;
            head = null;
            version++;
        }

        ///// <summary>
        ///// 判断是否包含键
        ///// </summary>
        ///// <param name="key">键</param>
        ///// <returns></returns>
        //public bool Contains(Object key) {
        //    if (key == null) {
        //        throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
        //    }
        //    Contract.EndContractBlock();
        //    for (DictionaryNode node = head; node != null; node = node.next) {
        //        if (node.key.Equals(key)) {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public bool Contains(Object key)
        {
            if(key == null)
            {
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            }
            Contract.EndContractBlock();
            for(DictionaryNode node = head;node!=null;node = node.next)
            {
                if(node.key.Equals(key))
                {
                    return true;
                }
            }
            return false;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="array"></param>
        ///// <param name="index"></param>
        //public void CopyTo(Array array, int index)  {
        //    if (array==null)
        //        throw new ArgumentNullException("array");
                
        //    if (array.Rank != 1)
        //        throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));

        //    if (index < 0)
        //            throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

        //    if ( array.Length - index < this.Count ) 
        //        throw new ArgumentException( Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");
        //    Contract.EndContractBlock();

        //    for (DictionaryNode node = head; node != null; node = node.next) {
        //        array.SetValue(new DictionaryEntry(node.key, node.value), index);
        //        index++;
        //    }
        //}

        public void CopyTo(Array array,int index)
        {
            if(array == null)
            {
                throw new ArgumentException("array");
            }
            if(array.Rank != 1)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            }
            if(index < 0)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if(array.Length - index < this.count)
            {
                throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");
            }

            Contract.EndContractBlock();
            for(DictionaryNode node = head; node != null;node = node.next)
            {
                array.SetValue(new DictionaryEntry(node.key, node.value), index);
                index++;
            }
        }

        //public IDictionaryEnumerator GetEnumerator() {
        //    return new NodeEnumerator(this);
        //}

        public IDictionaryEnumerator GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new NodeEnumerator(this);
        }

        //public void Remove(Object key) {
        //    if (key == null) {
        //        throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
        //    }
        //    Contract.EndContractBlock();
        //    version++;
        //    DictionaryNode last = null;
        //    DictionaryNode node;
        //    for (node = head; node != null; node = node.next) {
        //        if (node.key.Equals(key)) {
        //            break;
        //        } 
        //        last = node;
        //    }
        //    if (node == null) {
        //        return;
        //    }          
        //    if (node == head) {
        //        head = node.next;
        //    } else {
        //        last.next = node.next;
        //    }
        //    count--;
        //}

        public void Remove(Object key)
        {
            if(key == null)
            {
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            }
            Contract.EndContractBlock();
            version++;
            DictionaryNode last = null;
            DictionaryNode node;
            for(node = head; node!= null; node= node.next)
            {
                if(node.key.Equals(key))
                {
                    break;
                }
                last = node;
            }
            if(node == null)
            {
                return;
            }

            if(node == head)
            {
                head = head.next;
            }
            else
            {
                last.next = node.next;
            }

            count--;
        }

        //private class NodeEnumerator : IDictionaryEnumerator {
        //    ListDictionaryInternal list;
        //    DictionaryNode current;
        //    int version;
        //    bool start;


        //    public NodeEnumerator(ListDictionaryInternal list) {
        //        this.list = list;
        //        version = list.version;
        //        start = true;
        //        current = null;
        //    }

        //    public Object Current {
        //        get {
        //            return Entry;
        //        }
        //    }

        //    public DictionaryEntry Entry {
        //        get {
        //            if (current == null) {
        //                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
        //            }
        //            return new DictionaryEntry(current.key, current.value);
        //        }
        //    }

        //    public Object Key {
        //        get {
        //            if (current == null) {
        //                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
        //            }
        //            return current.key;
        //        }
        //    }

        //    public Object Value {
        //        get {
        //            if (current == null) {
        //                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
        //            }
        //            return current.value;
        //        }
        //    }

        //    public bool MoveNext() {
        //        if (version != list.version) {
        //            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
        //        }
        //        if (start) {
        //            current = list.head;
        //            start = false;
        //        }
        //        else {
        //            if( current != null ) {
        //                current = current.next;
        //            }
        //        }
        //        return (current != null);
        //    }

        //    public void Reset() {
        //        if (version != list.version) {
        //            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
        //        }
        //        start = true;
        //        current = null;
        //    }
            
        //}

        private class NodeEnumerator : IDictionaryEnumerator
        {
            ListDictionaryInternal list;
            DictionaryNode current;
            int version;
            bool start;

            public NodeEnumerator(ListDictionaryInternal list)
            {
                this.list = list;
                version = list.version;
                start = true;
                current = null;
            }

            public DictionaryEntry Current
            {
                get
                {
                    return Entry;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    }
                    return new DictionaryEntry(current.key,current.value);
                }
            }

            public Object Key
            {
                get
                {
                    if(current==null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    return current.key;
                }
            }

            public Object value
            {
                get
                {
                    if(current==null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    return current.value;
                }
            }

            public bool MoveNext()
            {
                if(version!=list.version)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                if(start)
                {
                    current = list.head;
                    start = false;
                }
                else
                {
                    if(current!=null)
                    {
                        current = current.next;
                    }
                }
                return (current != null);
            }

            public void Reset()
            {
                if(version!=null)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                }
                start = true;
                current = null;
            }
        }


        //private class NodeKeyValueCollection : ICollection {
        //    ListDictionaryInternal list;
        //    bool isKeys;

        //    public NodeKeyValueCollection(ListDictionaryInternal list, bool isKeys) {
        //        this.list = list;
        //        this.isKeys = isKeys;
        //    }

        //    void ICollection.CopyTo(Array array, int index)  {
        //        if (array==null)
        //            throw new ArgumentNullException("array");
        //        if (array.Rank != 1)
        //            throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
        //        if (index < 0)
        //            throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
        //        Contract.EndContractBlock();
        //        if (array.Length - index < list.Count) 
        //            throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");                
        //        for (DictionaryNode node = list.head; node != null; node = node.next) {
        //            array.SetValue(isKeys ? node.key : node.value, index);
        //            index++;
        //        }
        //    }

        //    int ICollection.Count {
        //        get {
        //            int count = 0;
        //            for (DictionaryNode node = list.head; node != null; node = node.next) {
        //                count++;
        //            }
        //            return count;
        //        }
        //    }   

        //    bool ICollection.IsSynchronized {
        //        get {
        //            return false;
        //        }
        //    }

        //    Object ICollection.SyncRoot {
        //        get {
        //            return list.SyncRoot;
        //        }
        //    }

        //    IEnumerator IEnumerable.GetEnumerator() {
        //        return new NodeKeyValueEnumerator(list, isKeys);
        //    }


        //    private class NodeKeyValueEnumerator: IEnumerator {
        //        ListDictionaryInternal list;
        //        DictionaryNode current;
        //        int version;
        //        bool isKeys;
        //        bool start;

        //        public NodeKeyValueEnumerator(ListDictionaryInternal list, bool isKeys) {
        //            this.list = list;
        //            this.isKeys = isKeys;
        //            this.version = list.version;
        //            this.start = true;
        //            this.current = null;
        //        }

        //        public Object Current {
        //            get {
        //                if (current == null) {
        //                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
        //                }
        //                return isKeys ? current.key : current.value;
        //            }
        //        }

        //        public bool MoveNext() {
        //            if (version != list.version) {
        //                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
        //            }
        //            if (start) {
        //                current = list.head;
        //                start = false;
        //            }
        //            else {
        //                if( current != null) {
        //                    current = current.next;
        //                }
        //            }
        //            return (current != null);
        //        }

        //        public void Reset() {
        //            if (version != list.version) {
        //                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
        //            }
        //            start = true;
        //            current = null;
        //        }
        //    }        
        //}

        private class NodeKeyValueCollection : ICollection
        {
            ListDictionaryInternal list;
            bool isKeys;

            public NodeKeyValueCollection(ListDictionaryInternal list,bool isKeys)
            {
                this.list = list;
                this.isKeys = isKeys;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }
                if(array.Rank!=1)
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                }
                if(index < 0)
                {
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                }
                Contract.EndContractBlock();
                if (array.Length - index < list.count)
                    throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");
                for(DictionaryNode node = list.head;node != null;node = node.next)
                {
                    array.SetValue(isKeys ? node.key : node.value, index);
                }
            }

            int ICollection.Count
            {
                get {
                    int count = 0;
                    for(DictionaryNode node = list.head;node != null; node= node.next)
                    {
                        count++;
                    }
                    return count;
                }
            }

            object ICollection.SyncRoot
            {
                get { return false; }
            }

            bool ICollection.IsSynchronized
            {
                get { return list.IsSynchronized; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return list.GetEnumerator();
            }

            private class NodeKeyValueEnumerator : IEnumerable
            {

                ListDictionaryInternal list;
                DictionaryNode current;
                int version;
                bool isKeys;
                bool start;

                public NodeKeyValueEnumerator(ListDictionaryInternal list,bool isKsys)
                {
                    this.list = list;
                    this.version = list.version;
                    this.isKeys = isKsys;
                    start = true;
                    this.current = null;
                }

                public IEnumerator GetEnumerator()
                {
                    throw new NotImplementedException();
                }

                public Object Current
                {
                    get
                    {
                        if(current!= null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                        }

                        return isKeys ? current.key : current.value; 
                    }
                }

                public bool MoveNext()
                {
                    if(version != list.version)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                    if(start)
                    {
                        current = list.head;
                        start = false;
                    }else
                    {
                        if(current != null)
                        {
                            current = current.next;
                        }
                    }
                    return (current != null);
                }

                public void Reset()
                {
                    if(version != list.version)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                    start = true;
                    current = null;
                }
            }
        }

        /// <summary>
        /// 字典节点
        /// </summary>
        [Serializable]
        private class DictionaryNode {
            public Object key;
            public Object value;
            public DictionaryNode next;
        }
    }
}
