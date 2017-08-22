using System;

namespace System.Collections {

    /// <summary>
    /// 结构体比较器接口
    /// </summary>
    public interface IStructuralComparable {
        Int32 CompareTo(Object other, IComparer comparer);
    }
}
