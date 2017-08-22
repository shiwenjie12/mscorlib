// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
namespace System {
    
    using System;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// 提供将对象的值格式化为字符串表示形式的功能。
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
#if CONTRACTS_FULL
    [ContractClass(typeof(IFormattableContract))]
#endif // CONTRACTS_FULL
    public interface IFormattable
    {
        /// <summary>
        /// 使用指定的格式格式化当前实例的值。
        /// </summary>
        /// <param name="format">要使用的格式。</param>
        /// <param name="formatProvider">要用于设置值格式的提供程序。</param>
        /// <returns>使用指定格式的当前实例的值。</returns>
        [Pure]
        String ToString(String format, IFormatProvider formatProvider);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IFormattable))]
    internal abstract class IFormattableContract : IFormattable
    {
       String IFormattable.ToString(String format, IFormatProvider formatProvider)
       {
           Contract.Ensures(Contract.Result<String>() != null);
 	       throw new NotImplementedException();
       }
    }
#endif // CONTRACTS_FULL
}
