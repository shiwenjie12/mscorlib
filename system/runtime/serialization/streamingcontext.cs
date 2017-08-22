// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** ValueType: StreamingContext
**
**
** Purpose: A value type indicating the source or destination of our streaming.
**
**
===========================================================*/
namespace System.Runtime.Serialization {

    using System.Runtime.Remoting;
    using System;
    /// <summary>
    /// 描述给定的序列化流的源和目标，并提供一个由调用方定义的附加上下文。
    /// </summary>
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct StreamingContext {
        /// <summary>
        /// 额外信息
        /// </summary>
        internal Object m_additionalContext;
        /// <summary>
        /// 上下文状态初始化
        /// </summary>
        internal StreamingContextStates m_state;
        /// <summary>
        /// 使用给定的上下文状态初始化 StreamingContext 类的新实例。
        /// </summary>
        /// <param name="state">上下文状态初始化</param>
        public StreamingContext(StreamingContextStates state) 
            : this (state, null) {
        }
        /// <summary>
        /// 使用给定的上下文状态以及一些附加信息来初始化 StreamingContext 类的新实例。
        /// </summary>
        /// <param name="state">上下文状态初始化</param>
        /// <param name="additional">额外信息</param>
        public StreamingContext(StreamingContextStates state, Object additional) {
            m_state = state;
            m_additionalContext = additional;
        }
        /// <summary>
        /// 获取指定为附加上下文一部分的上下文。
        /// </summary>
        public Object Context {
            get { return m_additionalContext; }
        }
        /// <summary>
        /// 确定两个 StreamingContext 实例是否包含相同的值。 （重写 ValueType.Equals(Object)。）
        /// </summary>
        /// <param name="obj">StreamingContext 实例</param>
        /// <returns>是否相同</returns>
        public override bool Equals(Object obj) {
            if (!(obj is StreamingContext)) {//如果不是StreamiingContext,则返回False
                return false;
            }
            if (((StreamingContext)obj).m_additionalContext == m_additionalContext &&//如果上下文和额外信息相同，则返回True
                ((StreamingContext)obj).m_state == m_state) {
                return true;
            } 
            return false;
        }
        /// <summary>
        /// 返回该对象的哈希代码。 （重写 ValueType.GetHashCode()。）
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode() {
            return (int)m_state;
        }
        /// <summary>
        /// 获取传输数据的源或目标。
        /// </summary>
        public StreamingContextStates State {
            get { return m_state; } 
        }
    }
    
    // **********************************************************
    // Keep these in [....] with the version in vm\runtimehandles.h
    // **********************************************************
    /// <summary>
    /// 定义一个标记集，用于在序列化过程中指定流的源或目标上下文。
    /// </summary>
[Serializable]
[Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum StreamingContextStates {
        /// <summary>
        /// 指定源或目标上下文是同一计算机上的另外一个进程。
        /// </summary>
        CrossProcess=0x01,
        /// <summary>
        /// 指定源或目标上下文是另外一台计算机。
        /// </summary>
        CrossMachine=0x02,
        /// <summary>
        /// 指定源或目标上下文是文件。
        /// 用户可以假定文件的持续时间长于创建它们的进程，并且文件以特定方式将对象序列化，此方式不会使反序列化进程要求访问当前进程中的任何数据。
        /// </summary>
        File        =0x04,
        /// <summary>
        /// 指定源或目的上下文是持续的存储区，它可以包括数据库、文件或其他后备存储区。
        /// 用户可以假定持续数据的持续时间长于创建数据的进程，并且持续数据以特定方式将对象序列化，此方式不会使反序列化进程要求访问当前进程中的任何数据。
        /// </summary>
        Persistence =0x08,
        /// <summary>
        /// 指定数据在未知位置的上下文中进行远程处理。 用户无法假定它是否在同一台计算机上。
        /// </summary>
        Remoting    =0x10,
        /// <summary>
        /// 指定序列化上下文未知。
        /// </summary>
        Other       =0x20,
        /// <summary>
        /// 指定对象图形正在进行克隆。
        /// 用户可以假定克隆图形将继续在同一进程中存在，可以安全地访问句柄或其他对非托管资源的引用。
        /// </summary>
        Clone       =0x40,
        /// <summary>
        /// 指定源或目标上下文是另外一个 AppDomain。
        /// </summary>
        CrossAppDomain =0x80,
        /// <summary>
        /// 指定可以向其他任何上下文传输（或从其他任何上下文接收）序列化数据。
        /// </summary>
        All         =0xFF,
    }
}
