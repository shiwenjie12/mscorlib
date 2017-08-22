// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  IEnumerable
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Interface for classes providing IEnumerators
**
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;

    /// <summary>
    /// 实现这个接口,如果你需要支持VB的foreach语义。同时,COM类支持一个枚举器也将实现该接口。
    /// </summary>
#if CONTRACTS_FULL
    [ContractClass(typeof(IEnumerableContract))]
#endif // CONTRACTS_FULL
    [Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IEnumerable
    {
        // Interfaces are not serializable
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
        [Pure]
        [DispId(-4)]
        IEnumerator GetEnumerator();
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IEnumerable))]
    internal abstract class IEnumerableContract : IEnumerable
    {
        [Pure]
        IEnumerator IEnumerable.GetEnumerator()
        {
            Contract.Ensures(Contract.Result<IEnumerator>() != null);
            return default(IEnumerator);
        }
    }
#endif // CONTRACTS_FULL
}
