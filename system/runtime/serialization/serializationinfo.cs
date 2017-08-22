// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  SerializationInfo
**
**
** Purpose: 存储将对象序列化或反序列化所需的全部数据。此类不能被继承。
**          
**
**
===========================================================*/
namespace System.Runtime.Serialization
{

    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Remoting;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Proxies;
#endif
    using System.Globalization;
    using System.Diagnostics.Contracts;
    using System.Security;
#if FEATURE_CORECLR
    using System.Runtime.CompilerServices;
#endif 

    /// <summary>
    /// 存储将对象序列化或反序列化所需的全部数据。 此类不能被继承。
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SerializationInfo
    {
        /// <summary>
        /// 默认大小
        /// </summary>
        private const int defaultSize = 4;
        /// <summary>
        /// 程序集简称
        /// </summary>
        private const string s_mscorlibAssemblySimpleName = "mscorlib";
        /// <summary>
        /// 程序集全称
        /// </summary>
        private const string s_mscorlibFileName = s_mscorlibAssemblySimpleName + ".dll";
        
        // 即使我们有一个字典,我们仍然保持所有的数组back-compat。
        // 否则我们可能会遇到潜在的破坏行为,例如GetEnumerator()不返回条目在同一个订单,他们补充道。
        internal String[] m_members;
        internal Object[] m_data;
        internal Type[] m_types;
        /// <summary>
        /// 名字键值对
        /// </summary>
        private Dictionary<string, int> m_nameToIndex;
        internal int m_currMember;
        internal IFormatterConverter m_converter;
        private String m_fullTypeName;
        private String m_assemName;
        private Type objectType;
        private bool isFullTypeNameSetExplicit;
        private bool isAssemblyNameSetExplicit;
#if FEATURE_SERIALIZATION
        private bool requireSameTokenInPartialTrust;
#endif

        /// <summary>
        /// 创建 System.Runtime.Serialization.SerializationInfo 类的新实例。
        /// </summary>
        /// <param name="type">要序列化的对象的 System.Type。</param>
        /// <param name="converter">在反序列化过程中使用的 System.Runtime.Serialization.IFormatterConverter。</param>
        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter)
#if FEATURE_SERIALIZATION
            : this(type, converter, false)
        {
        }

        /// <summary>
        /// 初始化 System.Runtime.Serialization.SerializationInfo 类的新实例。
        /// </summary>
        /// <param name="type">要序列化的对象的 System.Type。</param>
        /// <param name="converter">在反序列化过程中使用的 System.Runtime.Serialization.IFormatterConverter。</param>
        /// <param name="requireSameTokenInPartialTrust">指示对象是否需要部分信任的同一标记。</param>
        /// <exception cref="ArgumentNullException">type,converter为空</exception>
        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter, bool requireSameTokenInPartialTrust)
#endif
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }

            Contract.EndContractBlock();

            objectType = type;
            m_fullTypeName = type.FullName;
            m_assemName = type.Module.Assembly.FullName;

            m_members = new String[defaultSize];
            m_data = new Object[defaultSize];
            m_types = new Type[defaultSize];

            m_nameToIndex = new Dictionary<string, int>();

            m_converter = converter;

#if FEATURE_SERIALIZATION
            this.requireSameTokenInPartialTrust = requireSameTokenInPartialTrust;
#endif
        }

        /// <summary>
        /// 获取或设置要序列化的 Type 的全名。
        /// </summary>
        public String FullTypeName
        {
            get
            {
                return m_fullTypeName;
            }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();
           
                m_fullTypeName = value;
                isFullTypeNameSetExplicit = true;
            }
        }

        /// <summary>
        /// 仅在序列化期间获取或设置要序列化的类型的程序集名称。
        /// </summary>
        public String AssemblyName
        {
            get
            {
                return m_assemName;
            }
#if FEATURE_SERIALIZATION
            [SecuritySafeCritical]
#endif
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();
#if FEATURE_SERIALIZATION
                if (this.requireSameTokenInPartialTrust)
                {
                    DemandForUnsafeAssemblyNameAssignments(this.m_assemName, value);
                }
#endif
                m_assemName = value;
                isAssemblyNameSetExplicit = true;
            }
        }

        /// <summary>
        /// 设置要序列化的对象的 Type。
        /// </summary>
        /// <param name="type"></param>
#if FEATURE_SERIALIZATION
        [SecuritySafeCritical]
#endif
        public void SetType(Type type)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();
#if FEATURE_SERIALIZATION
            if (this.requireSameTokenInPartialTrust)
            {
                DemandForUnsafeAssemblyNameAssignments(this.ObjectType.Assembly.FullName, type.Assembly.FullName);
            }
#endif
            if (!Object.ReferenceEquals(objectType, type))
            {
                objectType = type;
                m_fullTypeName = type.FullName;
                m_assemName = type.Module.Assembly.FullName;
                isFullTypeNameSetExplicit = false;
                isAssemblyNameSetExplicit = false;
            }
        }

        /// <summary>
        /// 比较两组数组
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool Compare(byte[] a, byte[] b)
        {
            // if either or both assemblies do not have public key token, we should demand, hence, returning false will force a demand
            if (a == null || b == null || a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i]) return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 为不安全程序集分配需求
        /// </summary>
        /// <param name="originalAssemblyName">原始程序集名称</param>
        /// <param name="newAssemblyName">新程序集名称</param>
        [SecuritySafeCritical]
        internal static void DemandForUnsafeAssemblyNameAssignments(string originalAssemblyName, string newAssemblyName)
        {
            if (!IsAssemblyNameAssignmentSafe(originalAssemblyName, newAssemblyName))
            {
                CodeAccessPermission.Demand(PermissionType.SecuritySerialization);
            }
        }

        /// <summary>
        /// 程序集是否安全分配安全
        /// </summary>
        /// <param name="originalAssemblyName">原始程序集</param>
        /// <param name="newAssemblyName">新程序集</param>
        /// <returns></returns>
        internal static bool IsAssemblyNameAssignmentSafe(string originalAssemblyName, string newAssemblyName)
        {
            if (originalAssemblyName == newAssemblyName)
            {
                return true;
            }

            AssemblyName originalAssembly = new AssemblyName(originalAssemblyName);
            AssemblyName newAssembly = new AssemblyName(newAssemblyName);

            // mscorlib will get loaded by the runtime regardless of its string casing or its public key token,
            // so setting the assembly name to mscorlib must always be protected by a demand
            if (string.Equals(newAssembly.Name, s_mscorlibAssemblySimpleName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(newAssembly.Name, s_mscorlibFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Compare(originalAssembly.GetPublicKeyToken(), newAssembly.GetPublicKeyToken());
        }

        /// <summary>
        /// 获取已添加到 SerializationInfo 存储区中的成员的数目
        /// </summary>
        public int MemberCount
        {
            get
            {
                return m_currMember;
            }
        }

        /// <summary>
        /// 返回要序列化的对象的类型。
        /// </summary>
        public Type ObjectType
        {
            get
            {
                return objectType;
            }
        }

        /// <summary>
        /// 获取完整类型名称是否已经显式设置。
        /// </summary>
        public bool IsFullTypeNameSetExplicit
        {
            get
            {
                return isFullTypeNameSetExplicit;
            }
        }

        /// <summary>
        /// 获取程序集名称是否已经显式设置。
        /// </summary>
        public bool IsAssemblyNameSetExplicit
        {
            get
            {
                return isAssemblyNameSetExplicit;
            }
        }

        /// <summary>
        /// 返回一个 SerializationInfoEnumerator，它用于循环访问 SerializationInfo 存储区中的名称/值对
        /// 备注：在将值写入流前需要枚举通过这些值的格式化程序最常使用此函数。
        /// </summary>
        /// <returns>一个 SerializationInfoEnumerator，用于分析此 SerializationInfo 存储区中的名称/值对。</returns>
        public SerializationInfoEnumerator GetEnumerator()
        {
            return new SerializationInfoEnumerator(m_members, m_data, m_types, m_currMember);
        }

        /// <summary>
        /// 扩张数组
        /// </summary>
        private void ExpandArrays()
        {
            int newSize;
            Contract.Assert(m_members.Length == m_currMember, "[SerializationInfo.ExpandArrays]m_members.Length == m_currMember");

            newSize = (m_currMember * 2);//当前成员*2

            //
            // 在病理情况下,我们可以包装
            // 将新数组大小设置为Int32最大值
            if (newSize < m_currMember)
            {
                if (Int32.MaxValue > m_currMember)
                {
                    newSize = Int32.MaxValue;
                }
            }

            //
            // 分配更多的空间和复制数组
            // 
            String[] newMembers = new String[newSize];
            Object[] newData = new Object[newSize];
            Type[] newTypes = new Type[newSize];

            // 数组内容复制
            Array.Copy(m_members, newMembers, m_currMember);
            Array.Copy(m_data, newData, m_currMember);
            Array.Copy(m_types, newTypes, m_currMember);

            //
            // 分配新的进行回成员var。
            //
            m_members = newMembers;
            m_data = newData;
            m_types = newTypes;
        }

        /// <summary>
        /// 将一个值添加到 SerializationInfo 存储区中，其中 value 与 name 相关联，并序列化为 Typetype。
        /// </summary>
        /// <param name="name">将与值关联的名称，因此它可在以后被反序列化。</param>
        /// <param name="value">将序列化的值。此对象的所有子级将自动被序列化。</param>
        /// <param name="type">要与当前对象相关联的 Type。此参数必须始终是该对象本身的类型或其一个基类的类型。</param>
        /// <exception cref="ArgumentNullException">如果 name 或 type 为 null。</exception>
        /// <exception cref="SerializationException">已有值与 name 相关联。</exception>
        public void AddValue(String name, Object value, Type type)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            AddValueInternal(name, value, type);
        }

        public void AddValue(String name, Object value)
        {
            if (null == value)
            {
                AddValue(name, value, typeof(Object));
            }
            else
            {
                AddValue(name, value, value.GetType());
            }
        }

        public void AddValue(String name, bool value)
        {
            AddValue(name, (Object)value, typeof(bool));
        }

        public void AddValue(String name, char value)
        {
            AddValue(name, (Object)value, typeof(char));
        }


        [CLSCompliant(false)]
        public void AddValue(String name, sbyte value)
        {
            AddValue(name, (Object)value, typeof(sbyte));
        }

        public void AddValue(String name, byte value)
        {
            AddValue(name, (Object)value, typeof(byte));
        }

        public void AddValue(String name, short value)
        {
            AddValue(name, (Object)value, typeof(short));
        }

        [CLSCompliant(false)]
        public void AddValue(String name, ushort value)
        {
            AddValue(name, (Object)value, typeof(ushort));
        }

        public void AddValue(String name, int value)
        {
            AddValue(name, (Object)value, typeof(int));
        }

        [CLSCompliant(false)]
        public void AddValue(String name, uint value)
        {
            AddValue(name, (Object)value, typeof(uint));
        }

        public void AddValue(String name, long value)
        {
            AddValue(name, (Object)value, typeof(long));
        }

        [CLSCompliant(false)]
        public void AddValue(String name, ulong value)
        {
            AddValue(name, (Object)value, typeof(ulong));
        }

        public void AddValue(String name, float value)
        {
            AddValue(name, (Object)value, typeof(float));
        }

        public void AddValue(String name, double value)
        {
            AddValue(name, (Object)value, typeof(double));
        }

        public void AddValue(String name, decimal value)
        {
            AddValue(name, (Object)value, typeof(decimal));
        }

        public void AddValue(String name, DateTime value)
        {
            AddValue(name, (Object)value, typeof(DateTime));
        }

        /// <summary>
        /// 内部添加值方法
        /// </summary>
        /// <param name="name">将与值关联的名称，因此它可在以后被反序列化。</param>
        /// <param name="value">将序列化的值。此对象的所有子级将自动被序列化。</param>
        /// <param name="type">要与当前对象相关联的 Type。此参数必须始终是该对象本身的类型或其一个基类的类型。</param>
        /// <exception cref="SerializationException">已有值与 name 相关联。</exception>
        internal void AddValueInternal(String name, Object value, Type type)
        {
            //判断键值对中是否有相同的那么，如果有则抛出异常
            if (m_nameToIndex.ContainsKey(name))
            {
                BCLDebug.Trace("SER", "[SerializationInfo.AddValue]Tried to add ", name, " twice to the SI.");
                throw new SerializationException(Environment.GetResourceString("Serialization_SameNameTwice"));
            }
            //想键值对中添加name和索引
            m_nameToIndex.Add(name, m_currMember);

            //
            // 如果我们需要扩张数组
            //
            if (m_currMember >= m_members.Length)
            {
                ExpandArrays();
            }

            //
            // 添加数据，然后提前计数器。
            //
            m_members[m_currMember] = name;
            m_data[m_currMember] = value;
            m_types[m_currMember] = type;
            m_currMember++;
        }

        /*=================================UpdateValue==================================
        **Action: Finds the value if it exists in the current data.  If it does, we replace
        **        the values, if not, we append it to the end.  This is useful to the 
        **        ObjectManager when it's performing fixups, but shouldn't be used by 
        **        clients.  Exposing out this functionality would allow children to overwrite
        **        their parent's values.
        **Returns: void
        **Arguments: name  -- the name of the data to be updated.
        **           value -- the new value.
        **           type  -- the type of the data being added.
        **Exceptions: None.  All error checking is done with asserts.
        ==============================================================================*/
        /// <summary>
        /// 更新序列化信息
        /// </summary>
        /// <param name="name">将与值关联的名称，因此它可在以后被反序列化。</param>
        /// <param name="value">将序列化的值。此对象的所有子级将自动被序列化。</param>
        /// <param name="type">要与当前对象相关联的 Type。此参数必须始终是该对象本身的类型或其一个基类的类型。</param>
        /// <exception cref="ArgumentNullException">name为空</exception>
        internal void UpdateValue(String name, Object value, Type type)
        {
            Contract.Assert(null != name, "[SerializationInfo.UpdateValue]name!=null");
            Contract.Assert(null != value, "[SerializationInfo.UpdateValue]value!=null");
            Contract.Assert(null != (object)type, "[SerializationInfo.UpdateValue]type!=null");

            //
            // 如果返回索引为-1，则添加值，否则如果name存在，更新值
            int index = FindElement(name);
            if (index < 0)
            {
                AddValueInternal(name, value, type);
            }
            else
            {
                m_data[index] = value;
                m_types[index] = type;
            }

        }

        /// <summary>
        /// 检索name数组是否存在
        /// </summary>
        /// <param name="name">与之相关的名称</param>
        /// <returns>如果存在则返回索引，否则返回-1</returns>
        private int FindElement(String name)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }
            Contract.EndContractBlock();
            BCLDebug.Trace("SER", "[SerializationInfo.FindElement]Looking for ", name, " CurrMember is: ", m_currMember);
            int index;
            // 返回索引
            if (m_nameToIndex.TryGetValue(name, out index))
            {
                return index;
            }
            return -1;
        }

        /*==================================GetElement==================================
        **Action: Use FindElement to get the location of a particular member and then return
        **        the value of the element at that location.  The type of the member is
        **        returned in the foundType field.
        **Returns: The value of the element at the position associated with name.
        **Arguments: name -- the name of the element to find.
        **           foundType -- the type of the element associated with the given name.
        **Exceptions: None.  FindElement does null checking and throws for elements not 
        **            found.
        ==============================================================================*/
        /// <summary>
        /// 获取name中的值和种类（若name不存在，则抛出异常）
        /// </summary>
        /// <param name="name">将与值关联的名称</param>
        /// <param name="foundType">要与当前对象相关联的 Type。</param>
        /// <returns>将序列化的值。</returns>
        /// <exception cref="SerializationException">name不存在</exception>
        private Object GetElement(String name, out Type foundType)
        {
            // 获取对象的索引
            int index = FindElement(name);
            if (index == -1)
            {
                throw new SerializationException(Environment.GetResourceString("Serialization_NotFound", name));
            }

            Contract.Assert(index < m_data.Length, "[SerializationInfo.GetElement]index<m_data.Length");
            Contract.Assert(index < m_types.Length, "[SerializationInfo.GetElement]index<m_types.Length");

            // 获取与对象有关的格式
            foundType = m_types[index];
            Contract.Assert((object)foundType != null, "[SerializationInfo.GetElement]foundType!=null");
            return m_data[index];//返回与对象有关的值
        }

        /// <summary>
        /// 获取name中的值和种类（若name不存在，则返回null）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="foundType"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.ComVisible(true)]
        private Object GetElementNoThrow(String name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                foundType = null;
                return null;
            }

            Contract.Assert(index < m_data.Length, "[SerializationInfo.GetElement]index<m_data.Length");
            Contract.Assert(index < m_types.Length, "[SerializationInfo.GetElement]index<m_types.Length");

            foundType = m_types[index];
            Contract.Assert((object)foundType != null, "[SerializationInfo.GetElement]foundType!=null");
            return m_data[index];
        }

        //
        // The user should call one of these getters to get the data back in the 
        // form requested.  
        //

        /// <summary>
        /// 从 SerializationInfo 存储区中检索一个值。
        /// </summary>
        /// <param name="name">与要检索的值关联的名称。</param>
        /// <param name="type">要检索的值的 Type。如果存储的值不能转换为该类型，系统将引发 InvalidCastException。</param>
        /// <returns>与 name 关联的指定 Type 的对象。</returns>
        /// <exception cref="ArgumentNullException">name 或 type 为 null。</exception>
        /// <exception cref="InvalidCastException">与 name 关联的值不能转换为 type。</exception>
        /// <exception cref="SerializationException">当前实例中没有找到具有指定名称的元素。</exception>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Object GetValue(String name, Type type)
        {

            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            RuntimeType rt = type as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));

            Type foundType;
            Object value;

            //获得元素内容
            value = GetElement(name, out foundType);
#if FEATURE_REMOTING
            //
            
            if (RemotingServices.IsTransparentProxy(value))
            {
                RealProxy proxy = RemotingServices.GetRealProxy(value);//将值转换为实际对象
                if (RemotingServices.ProxyCheckCast(proxy, rt))
                    return value;
            }
            else
#endif
                if (Object.ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
                {
                    return value;
                }

            Contract.Assert(m_converter != null, "[SerializationInfo.GetValue]m_converter!=null");

            return m_converter.Convert(value, type);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        // 
        internal Object GetValueNoThrow(String name, Type type)
        {
            Type foundType;
            Object value;

            Contract.Assert((object)type != null, "[SerializationInfo.GetValue]type ==null");
            Contract.Assert(type is RuntimeType, "[SerializationInfo.GetValue]type is not a runtime type");

            value = GetElementNoThrow(name, out foundType);
            if (value == null)
                return null;
#if FEATURE_REMOTING
            if (RemotingServices.IsTransparentProxy(value))
            {
                RealProxy proxy = RemotingServices.GetRealProxy(value);
                if (RemotingServices.ProxyCheckCast(proxy, (RuntimeType)type))
                    return value;
            }
            else
#endif
                if (Object.ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
                {
                    return value;
                }

            Contract.Assert(m_converter != null, "[SerializationInfo.GetValue]m_converter!=null");

            return m_converter.Convert(value, type);
        }

        /// <summary>
        /// 从 SerializationInfo 存储区中检索一个布尔值。
        /// </summary>
        /// <param name="name">与要检索的值关联的名称。</param>
        /// <returns>Type: System.Boolean与 name 关联的布尔值。</returns>
        public bool GetBoolean(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);//获取name的元素

            //
            // 判断对象引用是否相同，如果相同则返回值
            if (Object.ReferenceEquals(foundType, typeof(bool)))
            {
                return (bool)value;
            }
            //使用格式类型转换
            return m_converter.ToBoolean(value);
        }

        public char GetChar(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(char)))
            {
                return (char)value;
            }
            return m_converter.ToChar(value);
        }

        [CLSCompliant(false)]
        public sbyte GetSByte(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(sbyte)))
            {
                return (sbyte)value;
            }
            return m_converter.ToSByte(value);
        }

        public byte GetByte(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(byte)))
            {
                return (byte)value;
            }
            return m_converter.ToByte(value);
        }

        public short GetInt16(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(short)))
            {
                return (short)value;
            }
            return m_converter.ToInt16(value);
        }

        [CLSCompliant(false)]
        public ushort GetUInt16(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(ushort)))
            {
                return (ushort)value;
            }
            return m_converter.ToUInt16(value);
        }

        public int GetInt32(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(int)))
            {
                return (int)value;
            }
            return m_converter.ToInt32(value);
        }

        [CLSCompliant(false)]
        public uint GetUInt32(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(uint)))
            {
                return (uint)value;
            }
            return m_converter.ToUInt32(value);
        }

        public long GetInt64(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(long)))
            {
                return (long)value;
            }
            return m_converter.ToInt64(value);
        }

        [CLSCompliant(false)]
        public ulong GetUInt64(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(ulong)))
            {
                return (ulong)value;
            }
            return m_converter.ToUInt64(value);
        }

        public float GetSingle(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(float)))
            {
                return (float)value;
            }
            return m_converter.ToSingle(value);
        }


        public double GetDouble(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(double)))
            {
                return (double)value;
            }
            return m_converter.ToDouble(value);
        }

        public decimal GetDecimal(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(decimal)))
            {
                return (decimal)value;
            }
            return m_converter.ToDecimal(value);
        }

        public DateTime GetDateTime(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(DateTime)))
            {
                return (DateTime)value;
            }
            return m_converter.ToDateTime(value);
        }

        public String GetString(String name)
        {
            Type foundType;
            Object value;

            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof(String)) || value == null)
            {
                return (String)value;
            }
            return m_converter.ToString(value);
        }

        /// <summary>
        /// 获取
        /// </summary>
        internal string[] MemberNames
        {
            get
            {
                return m_members;
            }

        }

        internal object[] MemberValues
        {
            get
            {
                return m_data;
            }
        }

    }
}
