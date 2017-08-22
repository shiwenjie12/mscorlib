// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: OptionallySerializableAttribute
**
**
** Purpose: Various Attributes for Serialization 
**
**
============================================================*/
namespace System.Runtime.Serialization
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;

    /// <summary>
    /// 指定序列化流中可以缺少一个字段，这样 BinaryFormatter 和 SoapFormatter 就不会引发异常。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited=false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OptionalFieldAttribute : Attribute 
    {
        int versionAdded = 1;
        public OptionalFieldAttribute() { }
        
        public int VersionAdded 
        {
            get {
                return this.versionAdded;
            }
            set {
                if (value < 1)
                    throw new ArgumentException(Environment.GetResourceString("Serialization_OptionalFieldVersionValue"));
                Contract.EndContractBlock();
                this.versionAdded = value;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnSerializingAttribute : Attribute 
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnSerializedAttribute : Attribute 
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnDeserializingAttribute : Attribute 
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class OnDeserializedAttribute : Attribute 
    {
    }

}
