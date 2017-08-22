// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  ICloneable
**
** This interface is implemented by classes that support cloning.
**
===========================================================*/
namespace System {
    
    using System;
    // Defines an interface indicating that an object may be cloned.  Only objects 
    // that implement ICloneable may be cloned. The interface defines a single 
    // method which is called to create a clone of the object.   Object defines a method
    // MemberwiseClone to support default clone operations.
    // 定义一个接口说明一个对象可能是克隆。只有实现ICloneable可能克隆的对象。
    // 接口定义了一个方法,就是创建一个克隆的对象。对象定义了一个方法MemberwiseClone支持缺省克隆操作。
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ICloneable
    {
        // Interface does not need to be marked with the serializable attribute
        // Make a new object which is a copy of the object instanced.  This object may be either
        // deep copy or a shallow copy depending on the implementation of clone.  The default
        // Object support for clone does a shallow copy.
        // 接口不需要标明可序列化的属性
        // 新建一个对象实例化对象副本。这个对象可以是深复制或浅拷贝根据克隆的实现。默认对象支持克隆一个浅拷贝。
        Object Clone();
    }
}
