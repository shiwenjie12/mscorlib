// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// MemberTypes is an bit mask marking each type of Member that is defined as
// 
// <OWNER>WESU</OWNER>
//    a subclass of MemberInfo.  These are returned by MemberInfo.MemberType and 
//    are useful in switch statements.
//    MemberInfo的一个子类。这些都是由MemberInfo返回。MemberType和
//    switch语句中是有用的。
// <EMAIL>Author: darylo</EMAIL>
// Date: July 99
//
namespace System.Reflection {
    
    using System;
    // This Enum matchs the CorTypeAttr defined in CorHdr.h
    [Serializable]
    [Flags()]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum MemberTypes
    {
        // The following are the known classes which extend MemberInfo
        // 以下是已知的类扩展成员信息
        Constructor     = 0x01,//构造函数
        Event           = 0x02,//事件
        Field           = 0x04,//字段
        Method          = 0x08,//方法
        Property        = 0x10,//属性
        TypeInfo        = 0x20,//格式信息
        Custom          = 0x40,//习俗
        NestedType      = 0x80,//反射
        All             = Constructor | Event | Field | Method | Property | TypeInfo | NestedType,
    }
}
