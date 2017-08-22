// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// IPermission.cs
// 
// <OWNER>ShawnFa</OWNER>
//
// Defines the interface that all Permission objects must support.
// 

namespace System.Security
{
    /// <summary>
    /// 定义了接口,必须支持的所有许可对象。
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IPermission : ISecurityEncodable
    {
        // 注意:用于定义的常量是搬到PermissionsEnum.cs由于CLS限制。

        // 安全系统的完整性取决于一种手段复制对象以便对敏感对象的引用不暴露在运行时。因此,必须实现所有权限复制。
        // 使一个准确的许可。
        /// <summary>
        /// 创建并返回当前权限的相同副本。
        /// </summary>
        /// <returns></returns>
        IPermission Copy();

        /*
         * Methods to support the Installation, Registration, others... PolicyEngine
         */

        // 政策和运行机制(例如,否认)需要检索两个权限之间共享状态的一种手段。如果没有两个实例之间共享状态,那么方法应该返回null。
        //
        // 像GetCommonState能想到的方法,但是离开相交,以避免不必要的名称更改。
        // 
        // 返回一个新的许可permission-defined交集的两个权限。十字路口通常定义为特权参数包括“这”和“目标”。如果“目标”是null或错误的类型,返回null。
        // 
        /// <summary>
        /// 创建并返回一个权限，该权限是当前权限和指定权限的交集。
        /// </summary>
        /// <param name="target">目标权限</param>
        /// <returns></returns>
        IPermission Intersect(IPermission target);

        // The runtime policy manager also requires a means of combining the
        // state contained within two permissions of the same type in a logical OR
        // construct.  (The Union of two permission of different type is not defined, 
        // except when one of the two is a CompoundPermission of internal type equal
        // to the type of the other permission.)
        // 运行时策略管理员也需要一种手段相结合的权限中包含两个相同类型的权限逻辑或构造。
        // (联盟的两个不同类型没有定义许可,除非其中一个内部类型的CompoundPermission等于其他许可的类型)。
        //
        //
        /// <summary>
        /// 创建一个权限，该权限是当前权限与指定权限的并集。
        /// </summary>
        /// <param name="target">目标权限</param>
        /// <returns></returns>
        IPermission Union(IPermission target);

        // IsSubsetOf defines a standard mechanism for determining
        // relative safety between two permission demands of the same type.
        // If one demand x demands no more than some other demand y, then
        // x.IsSubsetOf(y) should return true. In this case, if the
        // demand for y is satisfied, then it is possible to assume that
        // the demand for x would also be satisfied under the same
        // circumstances. On the other hand, if x demands something that y
        // does not, then x.IsSubsetOf(y) should return false; the fact
        // that x is satisfied by the current security context does not
        // also imply that the demand for y will also be satisfied.
        // IsSubsetOf之间定义了一个标准的机制来确定相对安全两个相同类型的权限要求。
        //
        // 
        // Returns true if 'this' Permission allows no more access than the
        // argument.
        /// <summary>
        /// 确定当前权限是否为指定权限的子集。
        /// </summary>
        /// <param name="target">目标权限</param>
        /// <returns></returns>
 
        bool IsSubsetOf(IPermission target);

        // The Demand method is the fundamental part of the IPermission
        // interface from a component developer's perspective. The
        // permission represents the demand that the developer wants
        // satisfied, and Demand is the means to invoke the demand.
        // For each type of permission, the mechanism to verify the
        // demand will be different. However, to the developer, all
        // permissions invoke that mechanism through the Demand interface.
        // Mark this method as requiring a security object on the caller's frame
        // so the caller won't be inlined (which would mess up stack crawling).
        /// <summary>
        /// 如果不满足安全要求，则会在运行时引发 SecurityException。
        /// </summary>
        [DynamicSecurityMethodAttribute()]
        void Demand();

    }
}
