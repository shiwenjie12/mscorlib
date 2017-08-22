// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Partitioner.cs
//
// <OWNER>[....]</OWNER>
//
// Represents a particular way of splitting a collection into multiple partitions.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// 表示将一个数据源拆分成多个分区的特定方式。
    /// </summary>
    /// <typeparam name="TSource">集合中的元素的类型。</typeparam>
    /// <remarks>
    /// <para>
    /// Inheritors of <see cref="Partitioner{TSource}"/> must adhere to the following rules:
    /// <ol>
    /// <li><see cref="GetPartitions"/> should throw a
    /// <see cref="T:System.ArgumentOutOfRangeException"/> if the requested partition count is less than or
    /// equal to zero.</li>
    /// <li><see cref="GetPartitions"/> should always return a number of enumerables equal to the requested
    /// partition count. If the partitioner runs out of data and cannot create as many partitions as 
    /// requested, an empty enumerator should be returned for each of the remaining partitions. If this rule
    /// is not followed, consumers of the implementation may throw a <see
    /// cref="T:System.InvalidOperationException"/>.</li>
    /// <li><see cref="GetPartitions"/> and <see cref="GetDynamicPartitions"/>
    /// should never return null. If null is returned, a consumer of the implementation may throw a
    /// <see cref="T:System.InvalidOperationException"/>.</li>
    /// <li><see cref="GetPartitions"/> and <see cref="GetDynamicPartitions"/> should always return
    /// partitions that can fully and uniquely enumerate the input data source. All of the data and only the
    /// data contained in the input source should be enumerated, with no duplication that was not already in
    /// the input, unless specifically required by the particular partitioner's design. If this is not
    /// followed, the output ordering may be scrambled.</li>
    /// </ol>
    /// </para>
    /// </remarks>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public abstract class Partitioner<TSource>
    {
        /// <summary>
        /// 将基础集合分区成给定数目的分区。
        /// </summary>
        /// <param name="partitionCount">要创建的分区数</param>
        /// <returns>一个包含partitionCount枚举器的列表</returns>
        public abstract IList<IEnumerator<TSource>> GetPartitions(int partitionCount);

        /// <summary>
        /// 获取是否可以动态创建分区
        /// </summary>
        /// <returns>
        /// 如果 System.Collections.Concurrent.Partitioner<TSource> 可以根据分区请求动态创建分区，则为 true；如果
        /// System.Collections.Concurrent.Partitioner<TSource> 只能以静态方式分配分区，则为 false。partitions statically.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If a derived class does not override and implement <see cref="GetDynamicPartitions"/>,
        /// <see cref="SupportsDynamicPartitions"/> should return false. The value of <see
        /// cref="SupportsDynamicPartitions"/> should not vary over the lifetime of this instance.
        /// </para>
        /// </remarks>
        public virtual bool SupportsDynamicPartitions
        {
            get { return false; }
        }

        /// <summary>
        /// 创建一个可将基础集合分区成可变数目的分区的对象。
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned object implements the <see
        /// cref="T:System.Collections.Generic.IEnumerable{TSource}"/> interface. Calling <see
        /// cref="System.Collections.Generic.IEnumerable{TSource}.GetEnumerator">GetEnumerator</see> on the
        /// object creates another partition over the sequence.
        /// </para>
        /// <para>
        /// The <see cref="GetDynamicPartitions"/> method is only supported if the <see
        /// cref="SupportsDynamicPartitions"/>
        /// property returns true.
        /// </para>
        /// </remarks>
        /// <returns>一个可针对 基础数据源创建分区的对象</returns>
        /// <exception cref="NotSupportedException">动态分区不能被支持</exception>
        public virtual IEnumerable<TSource> GetDynamicPartitions()
        {
            throw new NotSupportedException(Environment.GetResourceString("Partitioner_DynamicPartitionsNotSupported"));
        }
    }
}
