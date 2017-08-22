// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  IEnumerator
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Base interface for all enumerators.
**
** 
===========================================================*/
namespace System.Collections {    
    using System;
    using System.Runtime.InteropServices;

    // Base interface for all enumerators, providing a simple approach
    // to iterating over a collection.
    [Guid("496B0ABF-CDEE-11d3-88E8-00902754C43A")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IEnumerator
    {
        // 接口不枚举器可序列化的进步枚举的下一个元素,并返回一个布尔值表示一个元素是否可用。
        // 在创建,一个枚举器概念上定位在枚举的第一个元素之前,和第一次调用MoveNext带来第一个元素的枚举。
        /// <summary>
        /// 将枚举数推进到集合的下一个元素。
        /// </summary>
        /// <returns>如果枚举数已成功地推进到下一个元素，则为 true；如果枚举数传递到集合的末尾，则为 false。</returns>
        bool MoveNext();

        // 返回当前元素的枚举。
        // 返回的值是未定义在第一次调用MoveNext调用MoveNext后,返回false。
        // 多个调用GetCurrent没有干预MoveNext调用将返回相同的对象。
        /// <summary>
        /// 获取集合中位于枚举数当前位置的元素。
        /// </summary>
        Object Current {
            get; 
        }

        // 重置计数器枚举的开始,重新开始。
        // 重置的首选行为是返回相同的枚举。
        // 这意味着如果你修改底层集合然后调用重置,IEnumerator将是无效的,就像它是如果你有称为MoveNext或Current。
        /// <summary>
        /// 将枚举数设置为其初始位置，该位置位于集合中第一个元素之前。
        /// </summary>
        void Reset();
    }
}
