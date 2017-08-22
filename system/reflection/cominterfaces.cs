// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>WESU</OWNER>
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.Policy;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// 向非托管代码公开 System.Type 类的公共成员。
    /// </summary>
    [GuidAttribute("BCA8B44D-AAD6-3A86-8AB7-03349F4F2DA2")]
    [CLSCompliant(false)]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeLibImportClassAttribute(typeof(System.Type))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _Type
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        /// <summary>
        /// 检索对象提供的类型信息接口的数量（0 或 1）。
        /// </summary>
        /// <param name="pcTInfo">指向一个位置，该位置接收对象提供的类型信息接口的数量。</param>
        void GetTypeInfoCount(out uint pcTInfo);
        /// <summary>
        /// 检索对象的类型信息，然后可以使用该信息获取接口的类型信息。
        /// </summary>
        /// <param name="iTInfo">要返回的类型信息。</param>
        /// <param name="lcid">类型信息的区域设置标识符。</param>
        /// <param name="ppTInfo">接收一个指针，指向请求的类型信息对象。</param>
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        /// <summary>
        /// 将一组名称映射为对应的一组调度标识符。
        /// </summary>
        /// <param name="riid">保留供将来使用。 必须为 IID_NULL。</param>
        /// <param name="rgszNames">要映射的名称的传入数组。</param>
        /// <param name="cNames">要映射的名称的计数。</param>
        /// <param name="lcid">要在其中解释名称的区域设置上下文。</param>
        /// <param name="rgDispId">调用方分配的数组，用于接收与名称对应的 ID。</param>
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        /// <summary>
        /// 提供对某一对象公开的属性和方法的访问。
        /// </summary>
        /// <param name="dispIdMember">标识成员。</param>
        /// <param name="riid">保留供将来使用。 必须为 IID_NULL。</param>
        /// <param name="lcid">要在其中解释参数的区域设置上下文。</param>
        /// <param name="wFlags">描述调用的上下文的标志。</param>
        /// <param name="pDispParams">指向一个结构的指针，该结构包含一个参数数组、一个命名参数的 DISPID 参数数组和数组中元素数的计数。</param>
        /// <param name="pVarResult">指向要存储结果的位置的指针。</param>
        /// <param name="pExcepInfo">指向一个包含异常信息的结构的指针。</param>
        /// <param name="puArgErr">第一个出错参数的索引。</param>
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        /// <summary>
        /// 为 COM 对象提供对 System.Type.ToString() 方法的版本无关的访问。
        /// </summary>
        /// <returns>表示当前 System.Type 的名称的 System.String。</returns>
        String ToString();
        /// <summary>
        /// 为 COM 对象提供对 System.Type.Equals(System.Object) 方法的版本无关的访问。
        /// </summary>
        /// <param name="other">System.Object，其基础系统类型将与当前 System.Type 的基础系统类型相比较。</param>
        /// <returns>如果 o 的基础系统类型与当前 System.Type 的基础系统类型相同，则为 true；否则为 false。</returns>
        bool Equals(Object other);
        /// <summary>
        /// 为 COM 对象提供对 System.Type.GetHashCode() 方法的版本无关的访问。
        /// </summary>
        /// <returns>包含此实例的哈希代码的 System.Int32。</returns>
        int GetHashCode();
        /// <summary>
        /// 为 COM 对象提供对 System.Type.GetType() 方法的版本无关的访问。
        /// </summary>
        /// <returns>当前 System.Type。</returns>
        Type GetType();
        #endregion

        #region MemberInfo Members
        /// <summary>
        /// 为 COM 对象提供对 System.Type.MemberType 属性的版本无关的访问。
        /// </summary>
        MemberTypes MemberType { get; }
        /// <summary>
        /// 为 COM 对象提供对 System.Reflection.MemberInfo.Name 属性的版本无关的访问。
        /// </summary>
        String Name { get; }
        /// <summary>
        /// 为 COM 对象提供对 System.Type.DeclaringType 属性的版本无关的访问。
        /// 声明格式
        /// </summary>
        Type DeclaringType { get; }
        /// <summary>
        /// 为 COM 对象提供对 System.Type.ReflectedType 属性的版本无关的访问。
        /// 引用格式
        /// </summary>
        Type ReflectedType { get; }
        /// <summary>
        /// 为 COM 对象提供对 System.Reflection.MemberInfo.GetCustomAttributes(System.Type,System.Boolean)方法的版本无关的访问。
        /// </summary>
        /// <param name="attributeType">要搜索的特性类型。 只返回可分配给此类型的属性。</param>
        /// <param name="inherit">指定是否搜索该成员的继承链以查找这些属性。</param>
        /// <returns>应用于此成员的自定义特性的数组；如果未应用任何特性，则为包含零 (0) 个元素的数组。</returns>
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        /// <summary>
        /// 为 COM 对象提供对 System.Reflection.Assembly.GetCustomAttributes(System.Boolean)方法的版本无关的访问。
        /// </summary>
        /// <param name="inherit">指定是否搜索该成员的继承链以查找这些属性。</param>
        /// <returns>应用于此成员的自定义特性的数组；如果未应用任何特性，则为包含零 (0) 个元素的数组。</returns>
        Object[] GetCustomAttributes(bool inherit);
        /// <summary>
        /// 为 COM 对象提供对 System.Reflection.MemberInfo.IsDefined(System.Type,System.Boolean)方法的版本无关的访问。
        /// </summary>
        /// <param name="attributeType">自定义属性应用于的 Type 对象。</param>
        /// <param name="inherit">指定是否搜索该成员的继承链以查找这些属性。</param>
        /// <returns></returns>
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region Type Members
        Guid GUID { get; }           
        Module Module { get; }            
        Assembly Assembly { get; }            
        RuntimeTypeHandle TypeHandle { get; }            
        String FullName { get; }            
        String Namespace { get; }            
        String AssemblyQualifiedName { get; }            
        int GetArrayRank();        
        Type BaseType { get; }
            
        ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);
        Type GetInterface(String name, bool ignoreCase);
        Type[] GetInterfaces();        
        Type[] FindInterfaces(TypeFilter filter,Object filterCriteria);        
        EventInfo GetEvent(String name,BindingFlags bindingAttr);        
        EventInfo[] GetEvents();
        EventInfo[] GetEvents(BindingFlags bindingAttr);
        Type[] GetNestedTypes(BindingFlags bindingAttr);
        Type GetNestedType(String name, BindingFlags bindingAttr);
        MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr);        
        MemberInfo[] GetDefaultMembers();               
        MemberInfo[] FindMembers(MemberTypes memberType,BindingFlags bindingAttr,MemberFilter filter,Object filterCriteria);    
        Type GetElementType();
        bool IsSubclassOf(Type c);        
        bool IsInstanceOfType(Object o);
        bool IsAssignableFrom(Type c);
        InterfaceMapping GetInterfaceMap(Type interfaceType);
        MethodInfo GetMethod(String name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);
        MethodInfo GetMethod(String name, BindingFlags bindingAttr);            
        MethodInfo[] GetMethods(BindingFlags bindingAttr);    
        FieldInfo GetField(String name, BindingFlags bindingAttr);    
        FieldInfo[] GetFields(BindingFlags bindingAttr);        
        PropertyInfo GetProperty(String name, BindingFlags bindingAttr);                
        PropertyInfo GetProperty(String name,BindingFlags bindingAttr,Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);                
        PropertyInfo[] GetProperties(BindingFlags bindingAttr);
        MemberInfo[] GetMember(String name, BindingFlags bindingAttr);
        MemberInfo[] GetMembers(BindingFlags bindingAttr);        
        Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters);
        Type UnderlyingSystemType
        {
            get;
        }       
    
        Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder, Object target, Object[] args, CultureInfo culture);   
        Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder, Object target, Object[] args);   
        ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,  Type[] types, ParameterModifier[] modifiers);   
        ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);            
        ConstructorInfo GetConstructor(Type[] types);
        ConstructorInfo[] GetConstructors();
        ConstructorInfo TypeInitializer
        {
            get;
        }
            
        MethodInfo GetMethod(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);        
        MethodInfo GetMethod(String name, Type[] types, ParameterModifier[] modifiers);
        MethodInfo GetMethod(String name, Type[] types);
        MethodInfo GetMethod(String name);                
        MethodInfo[] GetMethods();
        FieldInfo GetField(String name);   
        FieldInfo[] GetFields();
        Type GetInterface(String name);
        EventInfo GetEvent(String name);           
        PropertyInfo GetProperty(String name, Type returnType, Type[] types,ParameterModifier[] modifiers);                
        PropertyInfo GetProperty(String name, Type returnType, Type[] types);                
        PropertyInfo GetProperty(String name, Type[] types);               
        PropertyInfo GetProperty(String name, Type returnType);
        PropertyInfo GetProperty(String name);            
        PropertyInfo[] GetProperties();
        Type[] GetNestedTypes();
        Type GetNestedType(String name);
        MemberInfo[] GetMember(String name);
        MemberInfo[] GetMembers();
        TypeAttributes Attributes { get; }            
        bool IsNotPublic { get; }            
        bool IsPublic { get; }            
        bool IsNestedPublic { get; }            
        bool IsNestedPrivate { get; }            
        bool IsNestedFamily { get; }            
        bool IsNestedAssembly { get; }            
        bool IsNestedFamANDAssem { get; }            
        bool IsNestedFamORAssem { get; }            
        bool IsAutoLayout { get; }            
        bool IsLayoutSequential { get; }            
        bool IsExplicitLayout { get; }            
        bool IsClass { get; }            
        bool IsInterface { get; }            
        bool IsValueType { get; }            
        bool IsAbstract { get; }            
        bool IsSealed { get; }            
        bool IsEnum { get; }            
        bool IsSpecialName { get; }            
        bool IsImport { get; }            
        bool IsSerializable { get; }            
        bool IsAnsiClass { get; }            
        bool IsUnicodeClass { get; }            
        bool IsAutoClass { get; }            
        bool IsArray { get; }            
        bool IsByRef { get; }            
        bool IsPointer { get; }            
        bool IsPrimitive { get; }            
        bool IsCOMObject { get; }            
        bool HasElementType { get; }            
        bool IsContextful { get; }          
        bool IsMarshalByRef { get; }            
        bool Equals(Type o);
        #endregion
#endif
    }

    [GuidAttribute("17156360-2f1a-384a-bc52-fde93c215c5b")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Assembly))]
    [CLSCompliant(false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Assembly
    {
#if !FEATURE_CORECLR
        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region Assembly Members
        String CodeBase { 
#if FEATURE_CORECLR
[System.Security.SecurityCritical] // auto-generated
#endif
get; }
        String EscapedCodeBase { get; }
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        AssemblyName GetName();
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        AssemblyName GetName(bool copiedName);
        String FullName { get; }
        MethodInfo EntryPoint { get; }
        Type GetType(String name);
        Type GetType(String name, bool throwOnError);
        Type[] GetExportedTypes();
        Type[] GetTypes();
        Stream GetManifestResourceStream(Type type, String name);
        Stream GetManifestResourceStream(String name);
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        FileStream GetFile(String name);
        FileStream[] GetFiles();
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        FileStream[] GetFiles(bool getResourceModules);
        String[] GetManifestResourceNames();
        ManifestResourceInfo GetManifestResourceInfo(String resourceName);
        String Location { 
#if FEATURE_CORECLR
[System.Security.SecurityCritical] // auto-generated
#endif
get; }
#if FEATURE_CAS_POLICY
        Evidence Evidence { get; }
#endif // FEATURE_CAS_POLICY
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
#if FEATURE_SERIALIZATION
        [System.Security.SecurityCritical]  // auto-generated_required
        void GetObjectData(SerializationInfo info, StreamingContext context);
#endif
        [method: System.Security.SecurityCritical]
        event ModuleResolveEventHandler ModuleResolve;
        Type GetType(String name, bool throwOnError, bool ignoreCase);     
        Assembly GetSatelliteAssembly(CultureInfo culture);
        Assembly GetSatelliteAssembly(CultureInfo culture, Version version);
#if FEATURE_MULTIMODULE_ASSEMBLIES        
        Module LoadModule(String moduleName, byte[] rawModule);
        Module LoadModule(String moduleName, byte[] rawModule, byte[] rawSymbolStore);
#endif        
        Object CreateInstance(String typeName);
        Object CreateInstance(String typeName, bool ignoreCase);
        Object CreateInstance(String typeName, bool ignoreCase, BindingFlags bindingAttr,  Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes);
        Module[] GetLoadedModules();
        Module[] GetLoadedModules(bool getResourceModules);
        Module[] GetModules();
        Module[] GetModules(bool getResourceModules);
        Module GetModule(String name);
        AssemblyName[] GetReferencedAssemblies();
        bool GlobalAssemblyCache { get; }
        #endregion
#endif
    }


    [GuidAttribute("f7102fa9-cabb-3a74-a6da-b4567ef1b079")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.MemberInfo))]
    [CLSCompliant(false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _MemberInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion
#endif
    }


    [GuidAttribute("6240837A-707F-3181-8E98-A36AE086766B")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.MethodBase))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _MethodBase
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region MethodBase Members
        ParameterInfo[] GetParameters();
        MethodImplAttributes GetMethodImplementationFlags();
        RuntimeMethodHandle MethodHandle { get; }
        MethodAttributes Attributes { get; }
        CallingConventions CallingConvention { get; }
        Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsFinal { get; }
        bool IsVirtual { get; }
        bool IsHideBySig { get; }
        bool IsAbstract { get; }
        bool IsSpecialName { get; }      
        bool IsConstructor { get; }      
        Object Invoke(Object obj, Object[] parameters);        
        #endregion
#endif
    }


    [GuidAttribute("FFCC1B5D-ECB8-38DD-9B01-3DC8ABC2AA5F")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.MethodInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _MethodInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region MethodBase Members
        ParameterInfo[] GetParameters();
        MethodImplAttributes GetMethodImplementationFlags();
        RuntimeMethodHandle MethodHandle { get; }
        MethodAttributes Attributes { get; }
        CallingConventions CallingConvention { get; }
        Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsFinal { get; }
        bool IsVirtual { get; }
        bool IsHideBySig { get; }
        bool IsAbstract { get; }
        bool IsSpecialName { get; }      
        bool IsConstructor { get; }      
        Object Invoke(Object obj, Object[] parameters);        
        #endregion

        #region MethodInfo Members
        Type ReturnType { get; }
        ICustomAttributeProvider ReturnTypeCustomAttributes { get; }
        MethodInfo GetBaseDefinition();
        #endregion
#endif
    }
        

    [GuidAttribute("E9A19478-9646-3679-9B10-8411AE1FD57D")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.ConstructorInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _ConstructorInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region MethodBase Members
        ParameterInfo[] GetParameters();
        MethodImplAttributes GetMethodImplementationFlags();
        RuntimeMethodHandle MethodHandle { get; }
        MethodAttributes Attributes { get; }
        CallingConventions CallingConvention { get; }
        Object Invoke_2(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsFinal { get; }
        bool IsVirtual { get; }
        bool IsHideBySig { get; }
        bool IsAbstract { get; }
        bool IsSpecialName { get; }      
        bool IsConstructor { get; }      
        Object Invoke_3(Object obj, Object[] parameters);        
        #endregion

        #region ConstructorInfo
        Object Invoke_4(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        Object Invoke_5(Object[] parameters);
        #endregion
#endif
    }


    [GuidAttribute("8A7C1442-A9FB-366B-80D8-4939FFA6DBE0")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.FieldInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _FieldInfo
    {        
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members 成员信息
        /// <summary>
        /// 成员格式
        /// </summary>
        MemberTypes MemberType { get; }
        String Name { get; }
        /// <summary>
        /// 获取说明格式
        /// </summary>
        Type DeclaringType { get; }
        /// <summary>
        /// 获取反射格式
        /// </summary>
        Type ReflectedType { get; }
        /// <summary>
        /// 获取自定义属性数组
        /// </summary>
        /// <param name="attributeType">自定义属性格式</param>
        /// <param name="inherit">是否继承</param>
        /// <returns>返回自定义属性数组</returns>
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        /// <summary>
        /// 获取自定义属性数组
        /// </summary>
        /// <param name="inherit">是否继承</param>
        /// <returns>返回自定义属性数组</returns>
        Object[] GetCustomAttributes(bool inherit);
        /// <summary>
        /// 是否为定义类型
        /// </summary>
        /// <param name="attributeType">引用类型</param>
        /// <param name="inherit"> 是否继承</param>
        /// <returns></returns>
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region FieldInfo Members 字段信息
        /// <summary>
        /// 获取字段格式
        /// </summary>
        Type FieldType { get; }
        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        Object GetValue(Object obj); 
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        Object GetValueDirect(TypedReference obj);                
        void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);        
        void SetValueDirect(TypedReference obj,Object value);        
        RuntimeFieldHandle FieldHandle { get; }        
        FieldAttributes Attributes { get; }           
        void SetValue(Object obj, Object value);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsInitOnly { get; }
        bool IsLiteral { get; }
        bool IsNotSerialized { get; }
        bool IsSpecialName { get; }
        bool IsPinvokeImpl { get; }
        #endregion
#endif
    }

    
    [GuidAttribute("F59ED4E4-E68F-3218-BD77-061AA82824BF")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.PropertyInfo))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _PropertyInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region Property Members
        Type PropertyType { get; }
        Object GetValue(Object obj,Object[] index);
        Object GetValue(Object obj,BindingFlags invokeAttr,Binder binder, Object[] index, CultureInfo culture);
        void SetValue(Object obj, Object value, Object[] index);
        void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture);
        MethodInfo[] GetAccessors(bool nonPublic);
        MethodInfo GetGetMethod(bool nonPublic);
        MethodInfo GetSetMethod(bool nonPublic);
        ParameterInfo[] GetIndexParameters();
        PropertyAttributes Attributes { get; }
        bool CanRead { get; }
        bool CanWrite { get; }
        MethodInfo[] GetAccessors();
        MethodInfo GetGetMethod();
        MethodInfo GetSetMethod();
        bool IsSpecialName { get; }
        #endregion
#endif
    }


    [GuidAttribute("9DE59C64-D889-35A1-B897-587D74469E5B")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.EventInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _EventInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region EventInfo Members
        MethodInfo GetAddMethod(bool nonPublic);
        MethodInfo GetRemoveMethod(bool nonPublic);
        MethodInfo GetRaiseMethod(bool nonPublic);
        EventAttributes Attributes { get; }        
        MethodInfo GetAddMethod();
        MethodInfo GetRemoveMethod();
        MethodInfo GetRaiseMethod();
        void AddEventHandler(Object target, Delegate handler);        
        void RemoveEventHandler(Object target, Delegate handler);        
        Type EventHandlerType { get; }        
        bool IsSpecialName { get; }        
        bool IsMulticast { get; }
        #endregion
#endif
    }

    [GuidAttribute("993634C4-E47A-32CC-BE08-85F567DC27D6")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.ParameterInfo))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _ParameterInfo
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("D002E9BA-D9E3-3749-B1D3-D565A08B13E7")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Module))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Module
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("B42B6AAC-317E-34D5-9FA9-093BB4160C50")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.AssemblyName))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _AssemblyName
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }
}

