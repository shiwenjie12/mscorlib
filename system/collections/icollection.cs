// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  ICollection
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Base interface for all collections.
**
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Diagnostics.Contracts;

    // Base interface for all collections, defining enumerators, size, and 
    // synchronization methods.
#if CONTRACTS_FULL
    [ContractClass(typeof(ICollectionContract))]
#endif // CONTRACTS_FULL
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ICollection : IEnumerable
    {
        // Interfaces are not serialable
        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        // 接口不serialable CopyTo集合复制到一个数组,从一个特定的索引数组。
        void CopyTo(Array array, int index);
        
        // 获取在集合中的条目数量
        int Count
        { get; }
        
        
        // SyncRoot will return an Object to use for synchronization 
        // (thread safety).  You can use this object in your code to take a
        // lock on the collection, even if this collection is a wrapper around
        // another collection.  The intent is to tunnel through to a real 
        // implementation of a collection, and use one of the internal objects
        // found in that code.
        // 
        // SyncRoot将返回一个对象用于同步(线程安全)。
        // 您可以在代码中使用这个对象锁定集合,即使这个集合是另一个集合包装。
        // 目的是隧道到集合的真正的实现,并使用一个内部对象代码。
        //
        // In the absense of a static Synchronized method on a collection, 
        // the expected usage for SyncRoot would look like this:
        // 
        // 在静态同步方法也有一个集合,SyncRoot预期的使用情况是这样的:
        // 
        // ICollection col = ...
        // lock (col.SyncRoot) {
        //     // Some operation on the collection, which is now thread safe.
        //     // This may include multiple operations.
        // }
        // 
        // 
        // The system-provided collections have a static method called 
        // Synchronized which will create a thread-safe wrapper around the 
        // collection.  All access to the collection that you want to be 
        // thread-safe should go through that wrapper collection.  However, if
        // you need to do multiple calls on that collection (such as retrieving
        // two items, or checking the count then doing something), you should
        // NOT use our thread-safe wrapper since it only takes a lock for the
        // duration of a single method call.  Instead, use Monitor.Enter/Exit
        // or your language's equivalent to the C# lock keyword as mentioned 
        // above.
        // 
        // 系统提供准备的有一个静态方法叫同步将创建一个线程安全的集合包装。
        //所有你想要访问集合通过包装器收集应该是线程安全的。
        //然而,如果你需要多个调用,集合(如检索两个项目,或检查计数然后做),我们不应该使用线程安全的包装器,因为它只需要一个锁的单个方法调用的持续时间。
        //相反,使用Monitor.Enter /退出或者你的语言的等价于c#锁定关键词如上所述。

        // For collections with no publically available underlying store, the 
        // expected implementation is to simply return the this pointer.  Note 
        // that the this pointer may not be sufficient for collections that 
        // wrap other collections;  those should return the underlying 
        // collection's SyncRoot property.
        // 为集合没有公开可用的底层存储,预期的实现是返回指针。
        // 注意,这个指针并不足以满足包装其他集合的集合;那些应该返回底层SyncRoot房地产集合。
        Object SyncRoot
        { get; }
            
        // Is this collection synchronized (i.e., thread-safe)?  If you want a 
        // thread-safe collection, you can use SyncRoot as an object to 
        // synchronize your collection with.  If you're using one of the 
        // collections in System.Collections, you could call the static 
        // Synchronized method to get a thread-safe wrapper around the 
        // underlying collection.
        // 这是集合(即同步。,线程安全)?如果你想要一个线程安全的集合,您可以使用SyncRoot作为对象同步你的收藏。
        // 如果你使用一个集合的系统。集合,你可以调用静态同步方法得到一个线程安全的包装器底层集合。
        bool IsSynchronized
        { get; }
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(ICollection))]
    internal abstract class ICollectionContract : ICollection
    {
        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count { 
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return default(int);
            }
        }

        Object ICollection.SyncRoot {
            get {
                Contract.Ensures(Contract.Result<Object>() != null);
                return default(Object);
            }
        }

        bool ICollection.IsSynchronized {
            get { return default(bool); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }
    }
#endif // CONTRACTS_FULL
}
