// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// SecurityPermission.cs
// 
// <OWNER>[....]</OWNER>
//

namespace System.Security.Permissions
{
    using System;
    using System.IO;
    using System.Security.Util;
    using System.Text;
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// 为安全权限对象指定访问标志。
    /// </summary>
    [Serializable]
    [Flags]
[System.Runtime.InteropServices.ComVisible(true)]
#if !FEATURE_CAS_POLICY
    // The csharp compiler requires these types to be public, but they are not used elsewhere.
    [Obsolete("SecurityPermissionFlag is no longer accessible to application code.")]
#endif
    public enum SecurityPermissionFlag
    {
        /// <summary>
        /// 无安全性访问。
        /// </summary>
        NoFlags = 0x00,
        /* The following enum value is used in the EE (ASSERT_PERMISSION in security.cpp)
         * Should this value change, make corresponding changes there
         */ 
        /// <summary>
        /// 断言此代码的所有调用方均有该操作所需的权限的能力。
        /// </summary>
        Assertion = 0x01,
        /// <summary>
        /// 能够调用非托管代码。
        /// </summary>
        UnmanagedCode = 0x02,       // Update vm\Security.h if you change this !
        /// <summary>
        /// 能够跳过此程序集内对代码的验证
        /// </summary>
        SkipVerification = 0x04,    // Update vm\Security.h if you change this !
        /// <summary>
        /// 使代码运行的权限。
        /// </summary>
        Execution = 0x08,
        /// <summary>
        /// 能够在线程上使用某些高级操作。
        /// </summary>
        ControlThread = 0x10,
        /// <summary>
        /// 能够提供证据，包括能够更改公共语言运行时所提供的证据。
        /// </summary>
        ControlEvidence = 0x20,
        /// <summary>
        /// 能够查看并修改策略。
        /// </summary>
        ControlPolicy = 0x40,
        /// <summary>
        /// 能够提供序列化服务。 由序列化格式化程序使用。
        /// </summary>
        SerializationFormatter = 0x80,
        /// <summary>
        /// 能够指定域策略。
        /// </summary>
        ControlDomainPolicy = 0x100,
        /// <summary>
        /// 能够操控用户对象。
        /// </summary>
        ControlPrincipal = 0x200,
        /// <summary>
        /// 能够创建和操控 AppDomain。
        /// </summary>
        ControlAppDomain = 0x400,
        /// <summary>
        /// 用于配置远程类型和信道的权限。
        /// </summary>
        RemotingConfiguration = 0x800,
        /// <summary>
        /// 用于将代码插入公共语言运行时结构的权限，如添加 Remoting Context Sink、Envoy Sink 和 Dynamic Sink。
        /// </summary>
        Infrastructure = 0x1000,
        /// <summary>
        /// 在应用程序配置文件中执行显式绑定重定向所需的权限。
        /// </summary>
        BindingRedirects = 0x2000,
        /// <summary>
        /// 权限的无限制状态。
        /// </summary>
        AllFlags = 0x3fff,
    }

    /// <summary>
    /// 描述应用于代码的安全权限集。 此类不能被继承。
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed public class SecurityPermission 
           : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
#pragma warning disable 618
        private SecurityPermissionFlag m_flags;//安全权限对象指定访问标志
#pragma warning restore 618
        
        //
        // 公共构造函数
        //
        
        /// <summary>
        /// 用指定的受限制或无限制的权限初始化 SecurityPermission 类的新实例。
        /// </summary>
        /// <param name="state"></param>
        public SecurityPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                SetUnrestricted( true );
            }
            else if (state == PermissionState.None)
            {
                SetUnrestricted( false );
                Reset();
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));//参数无效许可状态
            }
        }
        
        
        // SecurityPermission
        //
#pragma warning disable 618
        /// <summary>
        /// 使用指定的标志初始设置状态初始化 SecurityPermission 类的新实例。
        /// </summary>
        /// <param name="flag"></param>
        public SecurityPermission(SecurityPermissionFlag flag)
#pragma warning restore 618
        {
            VerifyAccess(flag);
            
            SetUnrestricted(false);
            m_flags = flag;
        }
    
    
        //------------------------------------------------------
        //
        // PRIVATE AND PROTECTED MODIFIERS 
        //
        //------------------------------------------------------
        
        /// <summary>
        /// 设置限制（True:无限制状态，False:不做处理）
        /// </summary>
        /// <param name="unrestricted"></param>
        private void SetUnrestricted(bool unrestricted)
        {
            if (unrestricted)
            {
#pragma warning disable 618
                m_flags = SecurityPermissionFlag.AllFlags;//将状态设置为权限无限制状态
#pragma warning restore 618
            }
        }
        
        /// <summary>
        /// 清除权限
        /// </summary>
        private void Reset()
        {
#pragma warning disable 618
            m_flags = SecurityPermissionFlag.NoFlags;//无安全性访问限制
#pragma warning restore 618
        }
        
        
        /// <summary>
        /// 获取或设置组成 SecurityPermission 权限的所有权限标志。
        /// </summary>
#pragma warning disable 618
        public SecurityPermissionFlag Flags
#pragma warning restore 618
        {
            set
            {
                VerifyAccess(value);
            
                m_flags = value;
            }
            
            get
            {
                return m_flags;
            }
        }
        
        //
        // CodeAccessPermission methods
        // 
        
       /*
         * IPermission interface implementation
         */
        /// <summary>
        /// 确定当前权限是否为指定权限的子集。 （重写 CodeAccessPermission.IsSubsetOf(IPermission)。）
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)//当值为空时，设置无安全性访问。
            {
                return m_flags == 0;
            }

            SecurityPermission operand = target as SecurityPermission;//将目标值转换为SecurityPermission
            if (operand != null)//如果不为空，则进行判断。否则抛出 Argument_WrongType 异常
            {
                return (((int)this.m_flags) & ~((int)operand.m_flags)) == 0;//按位运算，求出目标权限是否包含，如果包含则返回True,否则返回Flase;
            }
            else
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }

        }
        
        /// <summary>
        /// 创建一个权限，该权限是当前权限与指定权限的并集。 （重写 CodeAccessPermission.Union(IPermission)。）
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        public override IPermission Union(IPermission target) {
            if (target == null) return(this.Copy());
            if (!VerifyType(target)) {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            SecurityPermission sp_target = (SecurityPermission)target;//将目标转化为SecurityPermission格式
            if (sp_target.IsUnrestricted() || IsUnrestricted()) {//如果本对象或者目标对象有一个为无权限限制，则返回为无限制权限的SecurityPermission对象
                return(new SecurityPermission(PermissionState.Unrestricted));
            }
#pragma warning disable 618
            SecurityPermissionFlag flag_union = (SecurityPermissionFlag)(m_flags | sp_target.m_flags);//按位运算，求出本对象与目标对象的交集
#pragma warning restore 618
            return(new SecurityPermission(flag_union));
        }
    
        /// <summary>
        /// 创建并返回一个权限，该权限是当前权限和指定权限的交集。(替代 CodeAccessPermission.Intersect(IPermission)。)
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        public override IPermission Intersect(IPermission target)
        {
            if (target == null)//如果目标对象为空，则返回为空
                return null;
            else if (!VerifyType(target))//如果格式不正确，则抛出Argument_WrongType的参数异常
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }

            SecurityPermission operand = (SecurityPermission)target;//将目标接口转化为SecurityPermission操作对象
#pragma warning disable 618
            SecurityPermissionFlag isectFlags = SecurityPermissionFlag.NoFlags;//声明一个嵌入无安全性访问标志
#pragma warning restore 618
           
            if (operand.IsUnrestricted())//如果操作对象权限是无权限限制等级
            {
                if (this.IsUnrestricted())//如果本对象也为无权限限制等级，则返回一个无权限限制SecurityPermission对象
                    return new SecurityPermission(PermissionState.Unrestricted);
                else
#pragma warning disable 618
                    isectFlags = (SecurityPermissionFlag)this.m_flags;//将本对象标志赋值为嵌入标志
#pragma warning restore 618
            }
            else if (this.IsUnrestricted())//如果本对象是无权限限制等级，则将操作对象标志赋值给嵌入标志
            {
#pragma warning disable 618
                isectFlags = (SecurityPermissionFlag)operand.m_flags;
#pragma warning restore 618
            }
            else
            {
#pragma warning disable 618
                isectFlags = (SecurityPermissionFlag)m_flags & (SecurityPermissionFlag)operand.m_flags;//返回本对象与目标对象的交集
#pragma warning restore 618
            }
            
            if (isectFlags == 0)
                return null;
            else
                return new SecurityPermission(isectFlags);
        }
    
        /// <summary>
        /// 创建并返回当前权限的相同副本。 （重写 CodeAccessPermission.Copy()。）
        /// </summary>
        /// <returns></returns>
        public override IPermission Copy()
        {
            if (IsUnrestricted())
                return new SecurityPermission(PermissionState.Unrestricted);
            else
#pragma warning disable 618
                return new SecurityPermission((SecurityPermissionFlag)m_flags);
#pragma warning restore 618
        }
        
        /// <summary>
        /// 是否是自由的，即无权限限制
        /// </summary>
        /// <returns></returns>
        public bool IsUnrestricted()
        {
#pragma warning disable 618
            return m_flags == SecurityPermissionFlag.AllFlags;//无限制权限为自由
#pragma warning restore 618
        }
        
        /// <summary>
        /// 验证标志
        /// </summary>
        /// <param name="type">安全访问标志</param>
        private
#pragma warning disable 618
        void VerifyAccess(SecurityPermissionFlag type)
#pragma warning restore 618
        {
#pragma warning disable 618
            if ((type & ~SecurityPermissionFlag.AllFlags) != 0)//如果权限不在其中，这报出错误
#pragma warning restore 618
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)type));
            Contract.EndContractBlock();
        }

#if FEATURE_CAS_POLICY
        //------------------------------------------------------
        //
        // 公共编码方法
        //
        //------------------------------------------------------
        
        private const String _strHeaderAssertion  = "Assertion";//声明
        private const String _strHeaderUnmanagedCode = "UnmanagedCode";//为托管代码
        private const String _strHeaderExecution = "Execution";//执行
        private const String _strHeaderSkipVerification = "SkipVerification";//跳过验证
        private const String _strHeaderControlThread = "ControlThread";//控制线程
        private const String _strHeaderControlEvidence = "ControlEvidence";//控制证据
        private const String _strHeaderControlPolicy = "ControlPolicy";//控制政策
        private const String _strHeaderSerializationFormatter = "SerializationFormatter";//序列化格式
        private const String _strHeaderControlDomainPolicy = "ControlDomainPolicy";//控制域验证
        private const String _strHeaderControlPrincipal = "ControlPrincipal";//控制委托
        private const String _strHeaderControlAppDomain = "ControlAppDomain";//控制应用域
    
        /// <summary>
        /// 创建权限及其当前状态的 XML 编码。(替代 CodeAccessPermission.ToXml()。)
        /// </summary>
        /// <returns></returns>
        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.SecurityPermission" );
            if (!IsUnrestricted())
            {
                esd.AddAttribute( "Flags", XMLUtil.BitFieldEnumToString( typeof( SecurityPermissionFlag ), m_flags ) );
            }
            else
            {
                esd.AddAttribute( "Unrestricted", "true" );
            }
            return esd;
        }
    
        public override void FromXml(SecurityElement esd)
        {
            CodeAccessPermission.ValidateElement( esd, this );
            if (XMLUtil.IsUnrestricted( esd ))
            {
                m_flags = SecurityPermissionFlag.AllFlags;
                return;
            }
           
            Reset () ;
            SetUnrestricted (false) ;
    
            String flags = esd.Attribute( "Flags" );
    
            if (flags != null)
                m_flags = (SecurityPermissionFlag)Enum.Parse( typeof( SecurityPermissionFlag ), flags );
        }
#endif // FEATURE_CAS_POLICY

        //
        // Object Overrides
        //
        
    #if ZERO   // Do not remove this code, usefull for debugging
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SecurityPermission(");
            if (IsUnrestricted())
            {
                sb.Append("Unrestricted");
            }
            else
            {
                if (GetFlag(SecurityPermissionFlag.Assertion))
                    sb.Append("Assertion; ");
                if (GetFlag(SecurityPermissionFlag.UnmanagedCode))
                    sb.Append("UnmangedCode; ");
                if (GetFlag(SecurityPermissionFlag.SkipVerification))
                    sb.Append("SkipVerification; ");
                if (GetFlag(SecurityPermissionFlag.Execution))
                    sb.Append("Execution; ");
                if (GetFlag(SecurityPermissionFlag.ControlThread))
                    sb.Append("ControlThread; ");
                if (GetFlag(SecurityPermissionFlag.ControlEvidence))
                    sb.Append("ControlEvidence; ");
                if (GetFlag(SecurityPermissionFlag.ControlPolicy))
                    sb.Append("ControlPolicy; ");
                if (GetFlag(SecurityPermissionFlag.SerializationFormatter))
                    sb.Append("SerializationFormatter; ");
                if (GetFlag(SecurityPermissionFlag.ControlDomainPolicy))
                    sb.Append("ControlDomainPolicy; ");
                if (GetFlag(SecurityPermissionFlag.ControlPrincipal))
                    sb.Append("ControlPrincipal; ");
            }
            
            sb.Append(")");
            return sb.ToString();
        }
    #endif

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return SecurityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.SecurityPermissionIndex;
        }
    }
}
