namespace System.Collections {

    /// <summary>
    /// 定义方法以支持对象的结构相等性比较。
    /// </summary>
    public interface IStructuralEquatable {
        /// <summary>
        /// 确定某个对象与当前实例在结构上是否相等。
        /// </summary>
        /// <param name="other">要与当前实例进行比较的对象。</param>
        /// <param name="comparer">一个可确定当前实例与 other 是否相等的对象。</param>
        /// <returns></returns>
        Boolean Equals(Object other, IEqualityComparer comparer);
        
        /// <summary>
        /// 返回当前实例的哈希代码。
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        int GetHashCode(IEqualityComparer comparer);
    }
}