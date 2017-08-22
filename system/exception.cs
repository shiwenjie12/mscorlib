// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//
// <OWNER>[....]</OWNER>
//
// ==--==
/*=============================================================================
**
** Class: Exception
**
**
** Purpose: The base class for all exceptional conditions.
**
**
========================================================================== ===*/

namespace System {
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Security;
    using System.IO;
    using System.Text;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// 表示在应用程序执行过程中发生的错误。
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_Exception))]
    [Serializable]
    [ComVisible(true)]
    public class Exception : ISerializable, _Exception//序列化接口，异常接口
    {
        /// <summary>
        /// 初始化相关字段
        /// </summary>
        private void Init()
        {
            _message = null;//获取描述当前异常的消息。
            _stackTrace = null;//得到的字符串表示的直接调用堆栈帧。
            _dynamicMethods = null;//多态方法
            HResult = __HResults.COR_E_EXCEPTION;//获取或设置 HRESULT（一个分配给特定异常的编码数字值）。
            _xcode = _COMPlusExceptionCode;
            _xptrs = (IntPtr) 0;

            // 初始化WatsonBuckets 为空
            _watsonBuckets = null;

            // 初始化沃森桶装IP
            _ipForWatsonBuckets = UIntPtr.Zero;

#if FEATURE_SERIALIZATION
             _safeSerializationManager = new SafeSerializationManager();//安全序列化管理
#endif // FEATURE_SERIALIZATION
        }

        /// <summary>
        /// 构造Exception
        /// </summary>
        public Exception() {
            Init();
        }
    
        /// <summary>
        ///构造Exception
        /// </summary>
        /// <param name="message">当前异常的信息</param>
        public Exception(String message) {
            Init();
            _message = message;
        }
    
        /// <summary>
        /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 Exception 类的新实例。
        /// </summary>
        /// <param name="message">异常信息</param>
        /// <param name="innerException">导致当前异常的 Exception 实例。</param>
        public Exception (String message, Exception innerException) {
            Init();
            _message = message;
            _innerException = innerException;
        }

        /// <summary>
        /// 用序列化数据初始化 Exception 类的新实例。
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">描述给定的序列化流的源和目标，并提供一个由调用方定义的附加上下文。</param>
        /// <exception cref="ArgumentNullException">info 为空</exception>
        /// <exception cref="SerializationException">序列化信息为空</exception>
        [System.Security.SecuritySafeCritical]  // auto-generated
        protected Exception(SerializationInfo info, StreamingContext context) 
        {
            if (info==null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            #region 将序列化信息赋值
            _className = info.GetString("ClassName");
            _message = info.GetString("Message");
            _data = (IDictionary)(info.GetValueNoThrow("Data", typeof(IDictionary)));
            _innerException = (Exception)(info.GetValue("InnerException", typeof(Exception)));
            _helpURL = info.GetString("HelpURL");
            _stackTraceString = info.GetString("StackTraceString");
            _remoteStackTraceString = info.GetString("RemoteStackTraceString");
            _remoteStackIndex = info.GetInt32("RemoteStackIndex");

            _exceptionMethodString = (String)(info.GetValue("ExceptionMethod", typeof(String)));
            HResult = info.GetInt32("HResult");
            _source = info.GetString("Source");

            // 序列化的WatsonBuckets——这尤其支持穿过AD转换异常。
            // 我们使用没有抛出版本因为我们可以反序列化pre-V4异常对象,可能没有这个条目。在这种情况下,我们可以得到null。
            _watsonBuckets = (Object)info.GetValueNoThrow("WatsonBuckets", typeof(byte[])); 
            

#if FEATURE_SERIALIZATION
            _safeSerializationManager = info.GetValueNoThrow("SafeSerializationManager", typeof(SafeSerializationManager)) as SafeSerializationManager;
#endif // FEATURE_SERIALIZATION
            #endregion

            if (_className == null || HResult==0)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
            
            // 在调用CrossAppDomain后，正在构建一个新的异常
            if (context.State == StreamingContextStates.CrossAppDomain)
            {
                // ...this new exception may get thrown.  It is logically a re-throw, but 
                //  physically a brand-new exception.  Since the stack trace is cleared 
                //  on a new exception, the "_remoteStackTraceString" is provided to 
                //  effectively import a stack trace from a "remote" exception.  So,
                //  move the _stackTraceString into the _remoteStackTraceString.  Note
                //  that if there is an existing _remoteStackTraceString, it will be 
                //  preserved at the head of the new string, so everything works as 
                //  expected.
                // Even if this exception is NOT thrown, things will still work as expected
                //  because the StackTrace property returns the concatenation of the
                //  _remoteStackTraceString and the _stackTraceString.
                // …这可能会抛出新的异常。
                // 这在逻辑上是一个抛出收到,但身体上的一个全新的例外。
                // 自清除堆栈跟踪新的异常,提供有效地导入“_remoteStackTraceString”从“远程”异常堆栈跟踪。
                // 因此,移动_stackTraceString _remoteStackTraceString。
                // 注意,如果有现有_remoteStackTraceString,它将被保留下来的新字符串,所以一切按预期工作
                _remoteStackTraceString = _remoteStackTraceString + _stackTraceString;
                _stackTraceString = null;
            }
        }
        
        /// <summary>
        /// 获取描述当前异常的消息
        /// </summary>
        public virtual String Message {
               get {  
                if (_message == null) {//如果异常信息为为空，则返回className
                    if (_className==null) {
                        _className = GetClassName();
                    }
                    return Environment.GetResourceString("Exception_WasThrown", _className);

                } else {
                    return _message;
                }
            }
        }

        /// <summary>
        /// 获取提供有关异常的其他用户定义信息的键/值对集合。
        /// </summary>
        public virtual IDictionary Data { 
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (_data == null)//如果数据键值对为空，在根据条件初始化数据键值对
                    if (IsImmutableAgileException(this))
                        _data = new EmptyReadOnlyDictionaryInternal();
                    else
                        _data = new ListDictionaryInternal();
                
                return _data;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsImmutableAgileException(Exception e);

#if FEATURE_COMINTEROP
        // 异常需要添加到任何数据字典是可序列化的
        // 这个包装器是可序列化的,以满足这一要求,但不序列化对象序列化期间,完全忽略它,因为我们只需要应用程序持有错误的异常实例对象。
        // 一旦例外是序列化的调试器,调试器只需要引用字符串的错误
        /// <summary>
        /// 受限错误对象
        /// </summary>
        [Serializable]
        internal class __RestrictedErrorObject
        {
            //持有错误的对象实例,但不序列化/反序列化
            [NonSerialized]
            private object _realErrorObject;

            internal __RestrictedErrorObject(object errorObject)
            {
                _realErrorObject = errorObject;    
            }

            public object RealErrorObject
            {
               get
               {
                   return _realErrorObject;
               }
            }
        }

        /// <summary>
        /// 为限制错误对象添加异常
        /// </summary>
        /// <param name="restrictedError">限制错误</param>
        /// <param name="restrictedErrorReference">限制错误引用</param>
        /// <param name="restrictedCapabilitySid"></param>
        /// <param name="restrictedErrorObject">限制错误对象</param>
        /// <param name="hasrestrictedLanguageErrorObject">是否有限制语言错误对象</param>
        [FriendAccessAllowed]
        internal void AddExceptionDataForRestrictedErrorInfo(
            string restrictedError, 
            string restrictedErrorReference, 
            string restrictedCapabilitySid,
            object restrictedErrorObject,
            bool hasrestrictedLanguageErrorObject = false)
        {
            IDictionary dict = Data;
            if (dict != null)
            {
                dict.Add("RestrictedDescription", restrictedError);
                dict.Add("RestrictedErrorReference", restrictedErrorReference);
                dict.Add("RestrictedCapabilitySid", restrictedCapabilitySid);

                // Keep the error object alive so that user could retrieve error information
                // using Data["RestrictedErrorReference"]
                dict.Add("__RestrictedErrorObject", (restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject)));
                dict.Add("__HasRestrictedLanguageErrorObject", hasrestrictedLanguageErrorObject);
            }
        }

        /// <summary>
        /// 尝试获取限制语言错误对象
        /// </summary>
        /// <param name="restrictedErrorObject">限制语言错误对象</param>
        /// <returns></returns>
        internal bool TryGetRestrictedLanguageErrorObject(out object restrictedErrorObject)
        {
            restrictedErrorObject = null;
            if (Data != null && Data.Contains("__HasRestrictedLanguageErrorObject"))//判断异常数据信息是否为空，并且判断是否含有__HasRestrictedLanguageErrorObject
            {
                if (Data.Contains("__RestrictedErrorObject"))//如果异常数据信息包含__RestrictedErrorObject，进行赋值
                {
                    __RestrictedErrorObject restrictedObject = Data["__RestrictedErrorObject"] as __RestrictedErrorObject;
                    if (restrictedObject != null)
                        restrictedErrorObject = restrictedObject.RealErrorObject;
                }
                return (bool)Data["__HasRestrictedLanguageErrorObject"];
            }

            return false;
        }
#endif // FEATURE_COMINTEROP

        /// <summary>
        /// 返回Class名称
        /// </summary>
        /// <returns></returns>
        private string GetClassName()
        {
            // Will include namespace but not full instantiation and assembly name.
            if (_className == null)
                _className = GetType().ToString();

            return _className;
        }
    
        // Retrieves the lowest exception (inner most) for the given Exception.
        // This will traverse exceptions using the innerException property.
        // 获取最低的异常（内大多数）给定的异常
        // 这将遍历使用innerException属性异常
        /// <summary>
        /// 当在派生类中重写时，返回 Exception，它是一个或多个并发的异常的根源。
        /// </summary>
        /// <returns>异常链中第一个被引发的异常。 如果当前异常的 InnerException 属性是 null 引用（Visual Basic 中为 Nothing），则此属性返回当前异常。</returns>
        public virtual Exception GetBaseException() 
        {
            Exception inner = InnerException;//内部异常
            Exception back = this;
            
            while (inner != null) {
                back = inner;
                inner = inner.InnerException;
            }
            
            return back;
        }
        
        /// <summary>
        /// 包含在这个异常返回的内部异常
        /// </summary>
        public Exception InnerException {
            get { return _innerException; }
        }


        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static extern private IRuntimeMethodInfo GetMethodFromStackTrace(Object stackTrace);

        /// <summary>
        /// 从堆栈轨迹中获取异常方法
        /// </summary>
        /// <returns>方法或构造函数的信息</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private MethodBase GetExceptionMethodFromStackTrace()
        {
            IRuntimeMethodInfo method = GetMethodFromStackTrace(_stackTrace);

            // Under certain race conditions when exceptions are re-used, this can be null
            if (method == null)
                return null;

            return RuntimeType.GetMethodBase(method);
        }
        
        /// <summary>
        /// 获取引发当前异常的方法。
        /// </summary>
        public MethodBase TargetSite {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return GetTargetSiteInternal();
            }
        }


        // 这个函数是作为私人助手,以避免安全需求
        [System.Security.SecurityCritical]  // auto-generated
        private MethodBase GetTargetSiteInternal() {
            if (_exceptionMethod!=null) {
                return _exceptionMethod;
            }
            if (_stackTrace==null) {
                return null;
            }

            if (_exceptionMethodString!=null) {
                _exceptionMethod = GetExceptionMethodFromString();
            } else {
                _exceptionMethod = GetExceptionMethodFromStackTrace();
            }
            return _exceptionMethod;
        }
    
        /// <summary>
        /// 以字符串形式返回堆栈跟踪。如果没有可用的堆栈跟踪,返回null。
        /// </summary>
        public virtual String StackTrace
        {
#if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical] 
#endif
            get 
            {
                return GetStackTrace(true); //默认情况下试图包含文件和行号信息
            }
        }

        // 如果needFileInfo是正确的，计算并返回堆栈跟踪作为字符串试图获取源文件和行号信息。
        // 注意,这需要FileIOPermission(PathDiscovery),所以在CoreCLR通常会失败。
        // 避免产生需求和SecurityException我们可以明确甚至试图让fileinfo。
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private string GetStackTrace(bool needFileInfo)
        {
            string stackTraceString = _stackTraceString;
            string remoteStackTraceString = _remoteStackTraceString;

#if !FEATURE_CORECLR
            if (!needFileInfo)
            {
                // 从堆栈轨迹或远程堆栈轨迹中，过滤文件名/路径和行号
                // 这是只有当生成堆栈跟踪用于字符串必须PII-free沃森。
                stackTraceString = StripFileInfo(stackTraceString, false);
                remoteStackTraceString = StripFileInfo(remoteStackTraceString, true);
            }
#endif // !FEATURE_CORECLR

            // if no stack trace, try to get one
            if (stackTraceString != null)
            {
                return remoteStackTraceString + stackTraceString;
            }
            if (_stackTrace == null)
            {
                return remoteStackTraceString;
            }

            // Obtain the stack trace string. Note that since Environment.GetStackTrace
            // will add the path to the source file if the PDB is present and a demand
            // for FileIOPermission(PathDiscovery) succeeds, we need to make sure we 
            // don't store the stack trace string in the _stac、kTraceString member variable.
            String tempStackTraceString = Environment.GetStackTrace(this, needFileInfo);
            return remoteStackTraceString + tempStackTraceString;
         }
    
        /// <summary>
        /// 设置错误代码
        /// </summary>
        /// <param name="hr"></param>
        [FriendAccessAllowed]
        internal void SetErrorCode(int hr)
        {
            HResult = hr;
        }
        
        // Sets the help link for this exception.
        // This should be in a URL/URN form, such as:
        // "file:///C:/Applications/Bazzal/help.html#ErrorNum42"
        // Changed to be a read-write String and not return an exception
        /// <summary>
        /// 获取或设置异常帮助连接
        /// </summary>
        public virtual String HelpLink
        {
            get
            {
                return _helpURL;
            }
            set
            {
                _helpURL = value;
            }
        }
        
        /// <summary>
        /// 获取或设置导致错误的应用程序或对象的名称。
        /// </summary>
        public virtual String Source {
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            get { 
                if (_source == null)
                {
                    StackTrace st = new StackTrace(this, true);//获取堆栈轨迹
                    if (st.FrameCount > 0)//如果堆栈跟踪中的帧数大于零
                    {
                        StackFrame sf = st.GetFrame(0);//获取指定的堆栈帧
                        MethodBase method = sf.GetMethod();//获取方法信息

                        Module module = method.Module;//获取该方法模板

                        RuntimeModule rtModule = module as RuntimeModule;//转换为运行时模板

                        if (rtModule == null)//如果为空，则构建运行时模板
                        {
                            System.Reflection.Emit.ModuleBuilder moduleBuilder = module as System.Reflection.Emit.ModuleBuilder;//获取模板Builder
                            if (moduleBuilder != null)
                                rtModule = moduleBuilder.InternalModule;
                            else
                                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeReflectionObject"));
                        }

                        _source = rtModule.GetRuntimeAssembly().GetSimpleName();//获取模板名称，即引发异常源
                    }
                }

                return _source;
            }
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            set { _source = value; }
        }

        /// <summary>
        /// 重载字符串转换
        /// </summary>
        /// <returns></returns>
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] 
#endif
        public override String ToString()
        {
            return ToString(true, true);
        }

        /// <summary>
        /// 重载字符串转换
        /// </summary>
        /// <param name="needFileLineInfo">是否文件行信息</param>
        /// <param name="needMessage">是否需要信息</param>
        /// <returns></returns>
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private String ToString(bool needFileLineInfo, bool needMessage) {
            String message = (needMessage ? Message : null);//设置信息
            String s;//异常字符串

            if (message == null || message.Length <= 0) {
                s = GetClassName();
            }
            else {
                s = GetClassName() + ": " + message;
            }

            if (_innerException!=null) {
                s = s + " ---> " + _innerException.ToString(needFileLineInfo, needMessage) + Environment.NewLine + 
                "   " + Environment.GetResourceString("Exception_EndOfInnerExceptionStack");

            }

            string stackTrace = GetStackTrace(needFileLineInfo);
            if (stackTrace != null)
            {
                s += Environment.NewLine + stackTrace;
            }

            return s;
        }
    
        /// <summary>
        /// 获取异常方法字符串
        /// </summary>
        /// <returns></returns>
        [System.Security.SecurityCritical]  // auto-generated
        private String GetExceptionMethodString() {
            MethodBase methBase = GetTargetSiteInternal();
            if (methBase==null) {
                return null;
            }
            if (methBase is System.Reflection.Emit.DynamicMethod.RTDynamicMethod)
            {
                // DynamicMethods不能被序列化
                return null;
            }

            // 注意换行符分隔符只是一个分隔符,选择,这样它不会(通常)发生在方法名称。
            // 这个字符串的仅用于序列化异常的方法。
            char separator = '\n';
            StringBuilder result = new StringBuilder();
            if (methBase is ConstructorInfo) {//如果函数是构造函数
                RuntimeConstructorInfo rci = (RuntimeConstructorInfo)methBase;//将目标函数转换为运行时构造函数
                Type t = rci.ReflectedType;//获取构造函数的引用格式
                result.Append((int)MemberTypes.Constructor);//添加成员属性为构造
                result.Append(separator);//添加换行符
                result.Append(rci.Name);//添加运行时构造函数名称
                if (t!=null)
                {
                    result.Append(separator);//添加换行号
                    result.Append(t.Assembly.FullName);//添加程序集全称
                    result.Append(separator);//添加换行符
                    result.Append(t.FullName);//添加格式全称
                }
                result.Append(separator);//添加换行符
                result.Append(rci.ToString());//添加运行时信息
            } else {//如果不是构造函数
                Contract.Assert(methBase is MethodInfo, "[Exception.GetExceptionMethodString]methBase is MethodInfo");
                RuntimeMethodInfo rmi = (RuntimeMethodInfo)methBase;//将目标函数转换为运行时方法
                Type t = rmi.DeclaringType;//获取运行时声明函数
                result.Append((int)MemberTypes.Method);//添加成员属性为方法
                result.Append(separator);//添加换行符
                result.Append(rmi.Name);//添加运行时方法名称
                result.Append(separator);//添加换行符
                result.Append(rmi.Module.Assembly.FullName);//添加程序集信息
                result.Append(separator);//添加换行符
                if (t != null)
                {
                    result.Append(t.FullName);//添加方法全称
                    result.Append(separator);//添加换行符
                }
                result.Append(rmi.ToString());//添加运行时方法信息
            }
            
            return result.ToString();
        }

        /// <summary>
        /// 从字符串中获取引发异常的方法
        /// </summary>
        /// <returns>有关函数信息</returns>
        [System.Security.SecurityCritical]  // auto-generated
        private MethodBase GetExceptionMethodFromString() {
            Contract.Assert(_exceptionMethodString != null, "Method string cannot be NULL!");
            String[] args = _exceptionMethodString.Split(new char[]{'\0', '\n'});
            if (args.Length!=5) {
                throw new SerializationException();
            }
            SerializationInfo si = new SerializationInfo(typeof(MemberInfoSerializationHolder), new FormatterConverter());//获取序列化信息
            si.AddValue("MemberType", (int)Int32.Parse(args[0], CultureInfo.InvariantCulture), typeof(Int32));//添加序列化信息
            si.AddValue("Name", args[1], typeof(String));
            si.AddValue("AssemblyName", args[2], typeof(String));
            si.AddValue("ClassName", args[3]);
            si.AddValue("Signature", args[4]);
            MethodBase result;
            StreamingContext sc = new StreamingContext(StreamingContextStates.All);//设置跨域流内容
            try {
                result = (MethodBase)new MemberInfoSerializationHolder(si, sc).GetRealObject(sc);//获取实际对象，方法信息
            } catch (SerializationException) {
                result = null;
            }
            return result;
        }

#if FEATURE_SERIALIZATION
        /// <summary>
        /// 序列化对象事件处理器
        /// </summary>
        protected event EventHandler<SafeSerializationEventArgs> SerializeObjectState
        {
            add { _safeSerializationManager.SerializeObjectState += value; }
            remove { _safeSerializationManager.SerializeObjectState -= value; }
        }
#endif // FEATURE_SERIALIZATION

        /// <summary>
        /// 获取对象数据
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">序列化源和目标</param>
        /// <exception cref="ArgumentNullException">info为null</exception>
        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();

            String tempStackTraceString = _stackTraceString; //获取堆栈轨迹字符串      

            if (_stackTrace != null) //如果堆栈轨迹为空，则设置tempStackTraceString、_exceptionMethod
            {
                if (tempStackTraceString==null) 
                {
                    tempStackTraceString = Environment.GetStackTrace(this, true);
                }
                if (_exceptionMethod==null) 
                {
                    _exceptionMethod = GetExceptionMethodFromStackTrace();
                }
            }

            if (_source == null) 
            {
                _source = Source; // 序列化之前正确设置源信息
            }

            #region 设置序列化信息
            info.AddValue("ClassName", GetClassName(), typeof(String));
            info.AddValue("Message", _message, typeof(String));
            info.AddValue("Data", _data, typeof(IDictionary));
            info.AddValue("InnerException", _innerException, typeof(Exception));
            info.AddValue("HelpURL", _helpURL, typeof(String));
            info.AddValue("StackTraceString", tempStackTraceString, typeof(String));
            info.AddValue("RemoteStackTraceString", _remoteStackTraceString, typeof(String));
            info.AddValue("RemoteStackIndex", _remoteStackIndex, typeof(Int32));
            info.AddValue("ExceptionMethod", GetExceptionMethodString(), typeof(String));
            info.AddValue("HResult", HResult);
            info.AddValue("Source", _source, typeof(String));

            // Serialize the Watson bucket details as well
            info.AddValue("WatsonBuckets", _watsonBuckets, typeof(byte[])); 
            #endregion

#if FEATURE_SERIALIZATION
            if (_safeSerializationManager != null && _safeSerializationManager.IsActive)//判断安全序列化存在情况及状态
            {
                info.AddValue("SafeSerializationManager", _safeSerializationManager, typeof(SafeSerializationManager));

                // User classes derived from Exception must have a valid _safeSerializationManager.
                // Exceptions defined in mscorlib don't use this field might not have it initalized (since they are 
                // often created in the VM with AllocateObject instead if the managed construtor)
                // If you are adding code to use a SafeSerializationManager from an mscorlib exception, update
                // this assert to ensure that it fails when that exception's _safeSerializationManager is NULL 
                Contract.Assert(((_safeSerializationManager != null) || (this.GetType().Assembly == typeof(object).Assembly)), 
                                "User defined exceptions must have a valid _safeSerializationManager");
            
                // Handle serializing any transparent or partial trust subclass data
                _safeSerializationManager.CompleteSerialization(this, info, context);
            }
#endif // FEATURE_SERIALIZATION
        }

        // 这是远程维护服务器端所使用的堆栈跟踪通过附加到消息……除了rethrown之前在客户端调用站点。
        /// <summary>
        /// 为远程准备的异常信息
        /// </summary>
        /// <returns>异常信息</returns>
        internal Exception PrepForRemoting()
        {
            String tmp = null;

            if (_remoteStackIndex == 0)//如果远程堆索引为0，则为服务器堆信息
            {
                tmp = Environment.NewLine+ "Server stack trace: " + Environment.NewLine
                    + StackTrace 
                    + Environment.NewLine + Environment.NewLine 
                    + "Exception rethrown at ["+_remoteStackIndex+"]: " + Environment.NewLine;
            }
            else
            {
                tmp = StackTrace 
                    + Environment.NewLine + Environment.NewLine 
                    + "Exception rethrown at ["+_remoteStackIndex+"]: " + Environment.NewLine;
            }

            _remoteStackTraceString = tmp;
            _remoteStackIndex++;

            return this;
        }

        // This method will clear the _stackTrace of the exception object upon deserialization
        // to ensure that references from another AD/Process dont get accidently used.
        // 这个方法将明确的异常对象在反序列化的_stackTrace确保引用来自另一个转换/过程不让不小心使用。
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="context">序列化流</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _stackTrace = null;

            // 我们不会序列化或反序列化的IP沃森用桶装,因为我们不知道反序列化对象将使用的地方。
            // 使用跨进程或一个应用程序域中可能无效,导致AV在运行时。
            // 因此,我们把它反序列化发生时为零。
            _ipForWatsonBuckets = UIntPtr.Zero;

#if FEATURE_SERIALIZATION
            if (_safeSerializationManager == null)//
            {
                _safeSerializationManager = new SafeSerializationManager();
            }
            else
            {
                _safeSerializationManager.CompleteDeserialization(this);
            }
#endif // FEATURE_SERIALIZATION
        }

        // 这是使用的运行时抛出收到管理例外。它将复制_remoteStackTraceString的堆栈跟踪。
        /// <summary>
        /// 内部保护堆栈轨迹
        /// </summary>
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] 
#endif
        internal void InternalPreserveStackTrace()
        {
            string tmpStackTraceString;

#if FEATURE_APPX
            if (AppDomain.IsAppXModel())
            {
                // Call our internal GetStackTrace in AppX so we can parse the result should
                // we need to strip file/line info from it to make it PII-free. Calling the
                // public and overridable StackTrace getter here was probably not intended.
                //调用内部GetStackTrace大概我们可以解析结果应该我们需要带文件/行信息PII-free。异常堆栈调用公共和重写的getter这里可能是无意。
                tmpStackTraceString = GetStackTrace(true);

                // Make sure that the _source field is initialized if Source is not overriden.
                // We want it to contain the original faulting point.
                // 如果来源不是重载，确保_source字段初始化。我们希望它包含原始的断裂点。
                string source = Source;
            }
            else
#else // FEATURE_APPX
#if FEATURE_CORESYSTEM
            // Preinitialize _source on CoreSystem as well. The legacy behavior is not ideal and
            // we keep it for back compat but we can afford to make the change on the Phone.
            string source = Source;
#endif // FEATURE_CORESYSTEM
#endif // FEATURE_APPX
            {
                // Call the StackTrace getter in classic for compat.
                tmpStackTraceString = StackTrace;
            }

            if (tmpStackTraceString != null && tmpStackTraceString.Length > 0)
            {
                _remoteStackTraceString = tmpStackTraceString + Environment.NewLine;
            }
            
            _stackTrace = null;
            _stackTraceString = null;
        }
        
#if FEATURE_EXCEPTIONDISPATCHINFO

        // This is the object against which a lock will be taken
        // when attempt to restore the EDI. Since its static, its possible
        // that unrelated exception object restorations could get blocked
        // for a small duration but that sounds reasonable considering
        // such scenarios are going to be extremely rare, where timing
        // matches precisely.
        // 这是一个锁的对象将试图恢复EDI。
        // 静态以来,它可能不相关的异常对象修复能阻止小时间但听起来合理考虑这样的场景会极为罕见,在计时精确匹配。
        [OptionalField]
        private static object s_EDILock = new object();

        internal UIntPtr IPForWatsonBuckets
        {
            get {
                return _ipForWatsonBuckets;
            }        
        }
    
        internal object WatsonBuckets
        {
            get 
            {
                return _watsonBuckets;
            }
        }

        internal string RemoteStackTrace
        {
            get
            {
                return _remoteStackTraceString;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void PrepareForForeignExceptionRaise();

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetStackTracesDeepCopy(Exception exception, out object currentStackTrace, out object dynamicMethodArray);

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SaveStackTracesFromDeepCopy(Exception exception, object currentStackTrace, object dynamicMethodArray);

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object CopyStackTrace(object currentStackTrace);

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object CopyDynamicMethods(object currentDynamicMethods);

#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern string StripFileInfo(string stackTrace, bool isRemoteStackTrace);
#endif // !FEATURE_CORECLR

        [SecuritySafeCritical]
        internal object DeepCopyStackTrace(object currentStackTrace)
        {
            if (currentStackTrace != null)
            {
                return CopyStackTrace(currentStackTrace);
            }
            else
            {
                return null;
            }
        }

        [SecuritySafeCritical]
        internal object DeepCopyDynamicMethods(object currentDynamicMethods)
        {
            if (currentDynamicMethods != null)
            {
                return CopyDynamicMethods(currentDynamicMethods);
            }
            else
            {
                return null;
            }
        }
        
        [SecuritySafeCritical]
        internal void GetStackTracesDeepCopy(out object currentStackTrace, out object dynamicMethodArray)
        {
            GetStackTracesDeepCopy(this, out currentStackTrace, out dynamicMethodArray);
        }

        // This is invoked by ExceptionDispatchInfo.Throw to restore the exception stack trace, corresponding to the original throw of the
        // exception, just before the exception is "rethrown".
        [SecuritySafeCritical]
        internal void RestoreExceptionDispatchInfo(System.Runtime.ExceptionServices.ExceptionDispatchInfo exceptionDispatchInfo)
        {
            bool fCanProcessException = !(IsImmutableAgileException(this));
            // Restore only for non-preallocated exceptions
            if (fCanProcessException)
            {
                // Take a lock to ensure only one thread can restore the details
                // at a time against this exception object that could have
                // multiple ExceptionDispatchInfo instances associated with it.
                //
                // We do this inside a finally clause to ensure ThreadAbort cannot
                // be injected while we have taken the lock. This is to prevent
                // unrelated exception restorations from getting blocked due to TAE.
                try{}
                finally
                {
                    // When restoring back the fields, we again create a copy and set reference to them
                    // in the exception object. This will ensure that when this exception is thrown and these
                    // fields are modified, then EDI's references remain intact.
                    //
                    // Since deep copying can throw on OOM, try to get the copies
                    // outside the lock.
                    object _stackTraceCopy = (exceptionDispatchInfo.BinaryStackTraceArray == null)?null:DeepCopyStackTrace(exceptionDispatchInfo.BinaryStackTraceArray);
                    object _dynamicMethodsCopy = (exceptionDispatchInfo.DynamicMethodArray == null)?null:DeepCopyDynamicMethods(exceptionDispatchInfo.DynamicMethodArray);
                    
                    // Finally, restore the information. 
                    //
                    // Since EDI can be created at various points during exception dispatch (e.g. at various frames on the stack) for the same exception instance,
                    // they can have different data to be restored. Thus, to ensure atomicity of restoration from each EDI, perform the restore under a lock.
                    lock(Exception.s_EDILock)
                    {
                        _watsonBuckets = exceptionDispatchInfo.WatsonBuckets;
                        _ipForWatsonBuckets = exceptionDispatchInfo.IPForWatsonBuckets;
                        _remoteStackTraceString = exceptionDispatchInfo.RemoteStackTrace;
                        SaveStackTracesFromDeepCopy(this, _stackTraceCopy, _dynamicMethodsCopy);
                    }
                    _stackTraceString = null;

                    // Marks the TES state to indicate we have restored foreign exception
                    // dispatch information.
                    Exception.PrepareForForeignExceptionRaise();
                }
            }
        }
#endif // FEATURE_EXCEPTIONDISPATCHINFO

        /// <summary>
        /// 调用对象名称
        /// </summary>
        private String _className;  //Needed for serialization.  
        /// <summary>
        /// 调用方法名称
        /// </summary>
        private MethodBase _exceptionMethod;  //Needed for serialization. 
        /// <summary>
        /// 异常方法字符串
        /// </summary>
        private String _exceptionMethodString; //Needed for serialization. 
        /// <summary>
        /// 获取描述当前异常的消息。
        /// </summary>
        internal String _message;
        /// <summary>
        /// 获取提供有关异常的其他用户定义信息的键/值对集合。
        /// </summary>
        private IDictionary _data;
        /// <summary>
        /// 获取导致当前异常的 Exception 实例。
        /// </summary>
        private Exception _innerException;
        /// <summary>
        /// 获取或设置指向与此异常关联的帮助文件链接。
        /// </summary>
        private String _helpURL;
        /// <summary>
        /// 获取调用堆栈上的即时框架字符串表示形式。
        /// </summary>
        private Object _stackTrace;
        [OptionalField] // This isnt present in pre-V4 exception objects that would be serialized.
        private Object _watsonBuckets;
        /// <summary>
        /// 获取调用堆栈上的即时框架字符串表示形式。
        /// </summary>
        private String _stackTraceString; //Needed for serialization. 
        /// <summary>
        /// 获取调用堆栈上的远程即时框架字符串表示形式。
        /// </summary>
        private String _remoteStackTraceString;
        /// <summary>
        /// 远程堆索引
        /// </summary>
        private int _remoteStackIndex;
#pragma warning disable 414  // Field is not used from managed.        
        // _dynamicMethods is an array of System.Resolver objects, used to keep
        // DynamicMethodDescs alive for the lifetime of the exception. We do this because
        // the _stackTrace field holds MethodDescs, and a DynamicMethodDesc can be destroyed
        // unless a System.Resolver object roots it.
        private Object _dynamicMethods; 
#pragma warning restore 414

        // @MANAGED: HResult is used from within the EE!  Rename with care - check VM directory
        internal int _HResult;     // HResult

        /// <summary>
        /// 获取或设置 HRESULT（一个分配给特定异常的编码数字值）。
        /// </summary>
        public int HResult
        {
            get
            {
                return _HResult;
            }
            protected set
            {
                _HResult = value;
            }
        }
        
        private String _source;         // 主要被用于VB
        // WARNING: Don't delete/rename _xptrs and _xcode - used by functions
        // on Marshal class.  Native functions are in COMUtilNative.cpp & AppDomain
        // 警告:不要删除或重命名_xptrs和_xcode——元帅类使用的功能。本机函数COMUtilNative。cpp & AppDomain
        private IntPtr _xptrs;             // 内部EE材料
#pragma warning disable 414  // 字段不在托管中使用
        private int _xcode;             // 内部EE材料
#pragma warning restore 414
        [OptionalField]
        private UIntPtr _ipForWatsonBuckets; // 用于为沃森桶保存IP

#if FEATURE_SERIALIZATION
        /// <summary>
        /// 安全线程管理
        /// </summary>
        [OptionalField(VersionAdded = 4)]
        private SafeSerializationManager _safeSerializationManager;
#endif // FEATURE_SERIALIZATION

    // See clr\src\vm\excep.h's EXCEPTION_COMPLUS definition:
        /// <summary>
        /// Win32位COM+异常
        /// </summary>
        private const int _COMPlusExceptionCode = unchecked((int)0xe0434352);   

        // InternalToString称为运行时的异常的文本和创建一个相应的CrossAppDomainMarshaledException(跨域 封送异常).
        /// <summary>
        /// 运行时的异常的文本
        /// </summary>
        /// <returns></returns>
        [System.Security.SecurityCritical]  // auto-generated
        internal virtual String InternalToString()
        {
            try 
            {
#pragma warning disable 618
                SecurityPermission sp= new SecurityPermission(SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy);
#pragma warning restore 618
                sp.Assert();
            }
            catch  
            {
                //under normal conditions there should be no exceptions
                //however if something wrong happens we still can call the usual ToString
            }

            // Get the current stack trace string.  On CoreCLR we don't bother
            // to try and include file/line-number information because all AppDomains
            // are sandboxed, and so this won't succeed in most (or all) cases.  Therefore the
            // Demand and exception overhead is a waste.
            // We currently have some bugs in watson bucket generation where the SecurityException
            // here causes us to lose saved bucket parameters.  By not even doing the demand
            // we avoid those problems (although there are deep underlying problems that need to
            // be fixed there - relying on this to avoid problems is incomplete and brittle).
            bool fGetFileLineInfo = true;
#if FEATURE_CORECLR
            fGetFileLineInfo = false;
#endif
            return ToString(fGetFileLineInfo, true);
        }

#if !FEATURE_CORECLR
        // 该方法所需的对象。方法不是由编译器虚拟_Exception.GetType()
        public new Type GetType()
        {
            return base.GetType();
        }
#endif

        internal bool IsTransient
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return nIsTransient(_HResult);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static bool nIsTransient(int hr);


        // This piece of infrastructure exists to help avoid deadlocks 
        // between parts of mscorlib that might throw an exception while 
        // holding a lock that are also used by mscorlib's ResourceManager
        // instance.  As a special case of code that may throw while holding
        // a lock, we also need to fix our asynchronous exceptions to use
        // Win32 resources as well (assuming we ever call a managed 
        // constructor on instances of them).  We should grow this set of
        // exception messages as we discover problems, then move the resources
        // involved to native code.
        internal enum ExceptionMessageKind
        {
            ThreadAbort = 1,
            ThreadInterrupted = 2,
            OutOfMemory = 3
        }

        // See comment on ExceptionMessageKind
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static String GetMessageFromNativeResources(ExceptionMessageKind kind)
        {
            string retMesg = null;
            GetMessageFromNativeResources(kind, JitHelpers.GetStringHandleOnStack(ref retMesg));
            return retMesg;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.None)]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetMessageFromNativeResources(ExceptionMessageKind kind, StringHandleOnStack retMesg);
    }



#if FEATURE_CORECLR

    //--------------------------------------------------------------------------
    // Telesto: Telesto doesn't support appdomain marshaling of objects so
    // managed exceptions that leak across appdomain boundaries are flatted to
    // its ToString() output and rethrown as an CrossAppDomainMarshaledException.
    // The Message field is set to the ToString() output of the original exception.
    //--------------------------------------------------------------------------

    [Serializable]
    internal sealed class CrossAppDomainMarshaledException : SystemException 
    {
        public CrossAppDomainMarshaledException(String message, int errorCode) 
            : base(message) 
        {
            SetErrorCode(errorCode);
        }

        // Normally, only Telesto's UEF will see these exceptions.
        // This override prints out the original Exception's ToString()
        // output and hides the fact that it is wrapped inside another excepton.
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal override String InternalToString()
        {
            return Message;
        }
    
    }
#endif


}

