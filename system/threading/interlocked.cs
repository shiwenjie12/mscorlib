// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//
// <OWNER>[....]</OWNER>
namespace System.Threading
{
    using System;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Runtime;

    // After much discussion, we decided the Interlocked class doesn't need 
    // any HPA's for synchronization or external threading.  They hurt C#'s 
    // codegen for the yield keyword, and arguably they didn't protect much.  
    // Instead, they penalized people (and compilers) for writing threadsafe 
    // code.
    // 在经过很多讨论之后，我们确定Interlocked类不需要任何HPA为了同步或者额外的线程。
    // 他们会损伤c#代码的迭代keyword，并且他们不应该被如此保护，反而，他们应该处罚那些
    // 写线程安全的代码的人。
    /// <summary>
    /// 为多个线程共享的变量提供原子操作。
    /// </summary>
    public static class Interlocked
    {
        #region Increment （原子操作的形式递增指定变量的值并存储结果。）
        /******************************
         * Increment 增加
         *   Implemented（执行）: int
         *                        long
         *****************************/
        /// <summary>
        /// 以原子操作的形式递增指定变量的值并存储结果。
        /// </summary>
        /// <param name="location">其值要递增的变量。</param>
        /// <returns>递增的值。</returns>
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int Increment(ref int location)
        {
            return Add(ref location, 1);
        }

        /// <summary>
        /// 以原子操作的形式递增指定变量的值并存储结果。
        /// </summary>
        /// <param name="location">其值要递增的变量。</param>
        /// <returns>递增的值。</returns>
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static long Increment(ref long location)
        {
            return Add(ref location, 1);
        } 
        #endregion

        #region Decrement （以原子操作的形式递减指定变量的值并存储结果。）
        /******************************
         * Decrement 减少
         *   Implemented（实现）: int
         *                        long
         *****************************/
        /// <summary>
        /// 以原子操作的形式递减指定变量的值并存储结果。
        /// </summary>
        /// <param name="location">其值要递减的变量。</param>
        /// <returns>递减的值。</returns>
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int Decrement(ref int location)
        {
            return Add(ref location, -1);
        }

        /// <summary>
        /// 以原子操作的形式递减指定变量的值并存储结果。
        /// </summary>
        /// <param name="location">其值要递减的变量。</param>
        /// <returns>递减的值。</returns>
        [ResourceExposure(ResourceScope.None)]
        public static long Decrement(ref long location)
        {
            return Add(ref location, -1);
        } 
        #endregion

        #region Exchange （以原子操作的形式，将值设置为指定的值并返回原始值。）
        /******************************
         * Exchange(交换)
         *   Implemented（实现）: int
         *                        long
         *                        float
         *                        double
         *                        Object
         *                        IntPtr
         *****************************/

        /// <summary>
        /// 以原子操作的形式，将 32 位有符号整数设置为指定的值并返回原始值。
        /// </summary>
        /// <param name="location1">要设置为指定值的变量。</param>
        /// <param name="value">location1 参数被设置为的值。</param>
        /// <returns>location1 的原始值。</returns>
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern int Exchange(ref int location1, int value);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern long Exchange(ref long location1, long value);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern float Exchange(ref float location1, float value);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern double Exchange(ref double location1, double value);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern Object Exchange(ref Object location1, Object value);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern IntPtr Exchange(ref IntPtr location1, IntPtr value);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.InteropServices.ComVisible(false)]
        [System.Security.SecuritySafeCritical]
        public static T Exchange<T>(ref T location1, T value) where T : class
        {
            _Exchange(__makeref(location1), __makeref(value));//__makeref可以从对象自身中提取出TypedReference对象
            //Since value is a local we use trash its data on return
            //  The Exchange replaces the data with new data
            // Exchange 将新数据替代老数据
            //  so after the return "value" contains the original location1
            // 所以之后返回的“value”将包含原始location1
            //See ExchangeGeneric for more details   
            // ExchangeGeneric包含很多细节
            return value;
        }

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        private static extern void _Exchange(TypedReference location1, TypedReference value); 
        #endregion

        #region CompareExchange（比较两个值是否相等，如果相等，则替换其中一个值。）
        /******************************
         * CompareExchange
         *    Implemented: int
         *                         long
         *                         float
         *                         double
         *                         Object
         *                         IntPtr
         *****************************/

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern int CompareExchange(ref int location1, int value, int comparand);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern long CompareExchange(ref long location1, long value, long comparand);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern float CompareExchange(ref float location1, float value, float comparand);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern double CompareExchange(ref double location1, double value, double comparand);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern Object CompareExchange(ref Object location1, Object value, Object comparand);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern IntPtr CompareExchange(ref IntPtr location1, IntPtr value, IntPtr comparand);

        /*****************************************************************
         * CompareExchange<T>
         * 
         * Notice how CompareExchange<T>() uses the __makeref keyword
         * to create two TypedReferences before calling _CompareExchange().
         * This is horribly slow. Ideally we would like CompareExchange<T>()
         * to simply call CompareExchange(ref Object, Object, Object); 
         * however, this would require casting a "ref T" into a "ref Object", 
         * which is not legal in C#.
         * 
         * Thus we opted to cheat, and hacked to JIT so that when it reads
         * the method body for CompareExchange<T>() it gets back the
         * following IL:
         *
         *     ldarg.0 
         *     ldarg.1
         *     ldarg.2
         *     call System.Threading.Interlocked::CompareExchange(ref Object, Object, Object)
         *     ret
         *
         * See getILIntrinsicImplementationForInterlocked() in VM\JitInterface.cpp
         * for details.
         *****************************************************************/

        /// <summary>
        /// 比较指定的引用类型 T 的两个实例是否相等，如果相等，则替换其中一个。
        /// </summary>
        /// <typeparam name="T">用于 location1、value 和 comparand 的类型。 此类型必须是引用类型。</typeparam>
        /// <param name="location1">其值将与 comparand 进行比较并且可能被替换的目标。 这是一个引用参数（在 C# 中是 ref，在 Visual Basic 中是 ByRef）。</param>
        /// <param name="value">比较结果相等时替换目标值的值。</param>
        /// <param name="comparand">与位于 location1 处的值进行比较的值。</param>
        /// <returns>location1 中的原始值。</returns>
        /// <exception cref="System.NullReferenceException:">location1 的地址为空指针。</exception>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.InteropServices.ComVisible(false)]
        [System.Security.SecuritySafeCritical]
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class
        {
            // _CompareExchange() passes back the value read from location1 via local named 'value'
            _CompareExchange(__makeref(location1), __makeref(value), comparand);
            return value;
        }

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        private static extern void _CompareExchange(TypedReference location1, TypedReference value, Object comparand);

        // BCL-internal overload that returns success via a ref bool param, useful for reliable spin locks.
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        internal static extern int CompareExchange(ref int location1, int value, int comparand, ref bool succeeded); 
        #endregion

        #region Add（对两个值进行求和并用和替换第一个整数，上述操作作为一个原子操作完成。）
        /******************************
         * Add
         *    Implemented: int
         *                         long
         *****************************/

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int ExchangeAdd(ref int location1, int value);

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern long ExchangeAdd(ref long location1, long value);

        /// <summary>
        /// 对两个 32 位整数进行求和并用和替换第一个整数，上述操作作为一个原子操作完成。
        /// </summary>
        /// <param name="location1">一个变量，包含要添加的第一个值。 两个值的和存储在 location1 中。</param>
        /// <param name="value">要添加到整数中的 location1 位置的值。</param>
        /// <returns>存储在 location1 处的新值。</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int Add(ref int location1, int value)
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static long Add(ref long location1, long value)
        {
            return ExchangeAdd(ref location1, value) + value;
        } 
        #endregion
     
        /******************************
         * Read
         *****************************/
        /// <summary>
        /// 返回一个以原子操作形式加载的 64 位值。
        /// </summary>
        /// <param name="location">要加载的 64 位值。</param>
        /// <returns>加载的值。</returns>
        public static long Read(ref long location)
        {
            return Interlocked.CompareExchange(ref location,0,0);
        }

        /// <summary>
        /// 按如下方式同步内存存取：执行当前线程的处理器在对指令重新排序时，不能采用先执行 System.Threading.Interlocked.MemoryBarrier()
        /// 调用之后的内存存取，再执行 System.Threading.Interlocked.MemoryBarrier() 调用之前的内存存取的方式。
        /// </summary>
        public static void MemoryBarrier()
        {
            Thread.MemoryBarrier();
        }
    }
}
