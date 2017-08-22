// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** File:    AssemblyHash
**
**
** Purpose: 
**
**
===========================================================*/
namespace System.Configuration.Assemblies {
    using System;

    /// <summary>
    /// 代表程序集清单内容的哈希。
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
    public struct AssemblyHash : ICloneable
    {
        /// <summary>
        /// 程序集加密算法
        /// </summary>
        private AssemblyHashAlgorithm _Algorithm;
        /// <summary>
        /// 
        /// </summary>
        private byte[] _Value;
        
        /// <summary>
        /// 已过时。一个空 AssemblyHash 对象。
        /// </summary>
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static readonly AssemblyHash Empty = new AssemblyHash(AssemblyHashAlgorithm.None, null);
    
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyHash(byte[] value) {
            _Algorithm = AssemblyHashAlgorithm.SHA1;
            _Value = null;
    
            if (value != null) {
                int length = value.Length;
                _Value = new byte[length];
                Array.Copy(value, _Value, length);
            }
        }
    
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyHash(AssemblyHashAlgorithm algorithm, byte[] value) {
            _Algorithm = algorithm;
            _Value = null;
    
            if (value != null) {
                int length = value.Length;
                _Value = new byte[length];
                Array.Copy(value, _Value, length);
            }
        }
    
        // Hash is made up of a byte array and a value from a class of supported 
        // algorithm types.
        /// <summary>
        /// 已过时。获取或设置哈希算法。
        /// </summary>
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyHashAlgorithm Algorithm {
            get { return _Algorithm; }
            set { _Algorithm = value; }
        }

        /// <summary>
        /// 已过时。获取哈希值。
        /// </summary>
        /// <returns></returns>
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public byte[] GetValue() {
            return _Value;
        }

        /// <summary>
        /// 已过时。设置哈希值。
        /// </summary>
        /// <param name="value"></param>
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetValue(byte[] value) {
            _Value = value;
        }
    
        /// <summary>
        /// 已过时。克隆该对象。
        /// </summary>
        /// <returns></returns>
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public Object Clone() {
            return new AssemblyHash(_Algorithm, _Value);
        }
    }

}
