// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  Stream
** 
** <OWNER>gpaperin</OWNER>
**
**
** Purpose: Abstract base class for all Streams.  Provides
** default implementations of asynchronous reads & writes, in
** terms of the synchronous reads & writes (and vice versa).
**
**
===========================================================*/
using System;
using System.Threading;
#if FEATURE_ASYNC_IO
using System.Threading.Tasks;
#endif

using System.Runtime;
using System.Runtime.InteropServices;
#if NEW_EXPERIMENTAL_ASYNC_IO
using System.Runtime.CompilerServices;
#endif
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace System.IO {
    [Serializable]
    [ComVisible(true)]
#if CONTRACTS_FULL
    [ContractClass(typeof(StreamContract))]
#endif
#if FEATURE_REMOTING
    public abstract class Stream : MarshalByRefObject, IDisposable {
#else // FEATURE_REMOTING
    public abstract class Stream : IDisposable {
#endif // FEATURE_REMOTING

        /// <summary>
        /// 无后备存储区的 Stream。
        /// </summary>
        public static readonly Stream Null = new NullStream();

        // We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant(有效地)
        // improvement in Copy performance(性能).
        /// <summary>
        /// 默认的复制缓存大小
        /// </summary>
        private const int _DefaultCopyBufferSize = 81920;

#if NEW_EXPERIMENTAL_ASYNC_IO
        // To implement Async IO operations on streams that don't support async IO
        // 为了实现异步IO操作 streams不支持异步io

        /// <summary>
        /// 读写任务
        /// </summary>
        [NonSerialized]
        private ReadWriteTask _activeReadWriteTask;
        /// <summary>
        /// 微小的信号
        /// </summary>
        [NonSerialized]
        private SemaphoreSlim _asyncActiveSemaphore;

        /// <summary>
        /// 保证异步行为信号初始化
        /// </summary>
        /// <returns></returns>
        internal SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
        {
            // Lazily-initialize _asyncActiveSemaphore.  As we're never accessing the SemaphoreSlim's
            // WaitHandle, we don't need to worry about Disposing it.
            // Lazily-initialize _asyncActiveSemaphore.我们从来没有访问SemaphoreSlim WaitHandle,我们不需要担心处理它。
            return LazyInitializer.EnsureInitialized(ref _asyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
        }
#endif
        /// <summary>
        /// 当在派生类中重写时，获取指示当前流是否支持读取的值。
        /// </summary>
        public abstract bool CanRead {
            [Pure]
            get;
        }

        // If CanSeek is false, Position, Seek, Length, and SetLength should throw.
        /// <summary>
        /// 当在派生类中重写时，获取指示当前流是否支持查找功能的值。
        /// </summary>
        public abstract bool CanSeek {
            [Pure]
            get;
        }

        /// <summary>
        /// 获取一个值，该值确定当前流是否可以超时。
        /// </summary>
        [ComVisible(false)]
        public virtual bool CanTimeout {
            [Pure]
            get {
                return false;
            }
        }

        /// <summary>
        /// 当在派生类中重写时，获取指示当前流是否支持写入功能的值。
        /// </summary>
        public abstract bool CanWrite {
            [Pure]
            get;
        }

        /// <summary>
        /// 当在派生类中重写时，获取流长度（以字节为单位）。
        /// </summary>
        public abstract long Length {
            get;
        }

        /// <summary>
        /// 当在派生类中重写时，获取或设置当前流中的位置。
        /// </summary>
        public abstract long Position {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置一个值（以毫秒为单位），该值确定流在超时前尝试读取多长时间。
        /// </summary>
        [ComVisible(false)]
        public virtual int ReadTimeout {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }
            set {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }
        }

        /// <summary>
        /// 获取或设置一个值（以毫秒为单位），该值确定流在超时前尝试写入多长时间。
        /// </summary>
        [ComVisible(false)]
        public virtual int WriteTimeout {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }
            set {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }
        }

#if FEATURE_ASYNC_IO
        /// <summary>
        /// 从当前流中异步读取字节并将其写入到另一个流中。
        /// </summary>
        /// <param name="destination">当前流的内容将复制到的流。</param>
        /// <returns>表示异步复制操作的任务。</returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public Task CopyToAsync(Stream destination)
        {
            return CopyToAsync(destination, _DefaultCopyBufferSize);
        }

        /// <summary>
        /// 使用指定的缓冲区大小，从当前流中异步读取字节并将其写入到另一流中。
        /// </summary>
        /// <param name="destination">当前流的内容将复制到的流。</param>
        /// <param name="bufferSize">缓冲区的大小（以字节为单位）。  此值必须大于零。  默认大小为 81920。  </param>
        /// <returns>表示异步复制操作的任务。</returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public Task CopyToAsync(Stream destination, Int32 bufferSize)
        {
            return CopyToAsync(destination, bufferSize, CancellationToken.None);
        }

        /// <summary>
        /// 使用指定的缓冲区大小和取消令牌，从当前流中异步读取字节并将其写入到另一个流中。
        /// </summary>
        /// <param name="destination">当前流的内容将复制到的流。</param>
        /// <param name="bufferSize">缓冲区的大小（以字节为单位）。  此值必须大于零。  默认大小为 81920。  </param>
        /// <param name="cancellationToken">要监视取消请求的标记。  默认值为 None。  </param>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public virtual Task CopyToAsync(Stream destination, Int32 bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            if (!CanRead && !CanWrite)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("destination", Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (!destination.CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            Contract.EndContractBlock();

            return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
        }

        /// <summary>
        /// 内部实现异步复制
        /// </summary>
        /// <param name="destination">当前流的内容将复制到的流。</param>
        /// <param name="bufferSize">缓冲区的大小（以字节为单位）。  此值必须大于零。  默认大小为 81920。  </param>
        /// <param name="cancellationToken">要监视取消请求的标记。  默认值为 None。  </param>
        /// <returns></returns>
        private async Task CopyToAsyncInternal(Stream destination, Int32 bufferSize, CancellationToken cancellationToken)
        {
            Contract.Requires(destination != null);
            Contract.Requires(bufferSize > 0);
            Contract.Requires(CanRead);
            Contract.Requires(destination.CanWrite);

            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            // 使用两个异步任务，一个是异步读取任务，另一个是异步写入任务，循环读出和写入
            while ((bytesRead = await ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }
#endif // FEATURE_ASYNC_IO

        // Reads the bytes from the current stream and writes the bytes to
        // the destination stream until all bytes are read, starting at
        // the current position.
        /// <summary>
        /// 从当前流中读取字节并将其写入到另一流中。
        /// </summary>
        /// <param name="destination">当前流的内容将复制到的流。</param>
        public void CopyTo(Stream destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (!CanRead && !CanWrite)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("destination", Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (!destination.CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            Contract.EndContractBlock();

            InternalCopyTo(destination, _DefaultCopyBufferSize);
        }

        /// <summary>
        /// 使用指定的缓冲区大小，从当前流中读取字节并将其写入到另一流中。
        /// </summary>
        /// <param name="destination">当前流的内容将复制到的流。</param>
        /// <param name="bufferSize">缓冲区的大小。此值必须大于零。默认大小为 81920。</param>
        public void CopyTo(Stream destination, int bufferSize)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize",
                        Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            if (!CanRead && !CanWrite)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("destination", Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (!destination.CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            Contract.EndContractBlock();

            InternalCopyTo(destination, bufferSize);
        }

        /// <summary>
        /// 内部CopyTo实现
        /// </summary>
        /// <param name="destination">当前流的内容将复制到的流。</param>
        /// <param name="bufferSize">缓冲区的大小。此值必须大于零。默认大小为 81920。</param>
        private void InternalCopyTo(Stream destination, int bufferSize)
        {
            Contract.Requires(destination != null);
            Contract.Requires(CanRead);
            Contract.Requires(destination.CanWrite);
            Contract.Requires(bufferSize > 0);
            
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = Read(buffer, 0, buffer.Length)) != 0)
                destination.Write(buffer, 0, read);
        }


        // Stream used to require that all cleanup logic went into Close(),
        // which was thought up before we invented IDisposable.  However, we
        // need to follow the IDisposable pattern so that users can write 
        // sensible subclasses without needing to inspect all their base 
        // classes, and without worrying about version brittleness, from a
        // base class switching to the Dispose pattern.  We're moving
        // Stream to the Dispose(bool) pattern - that's where all subclasses 
        // should put their cleanup starting in V2.
        //流用于要求所有清理逻辑进入关闭(),它被认为在我们IDisposable发明的。
        //然而,我们需要遵循IDisposable模式,以便用户可以编写合理的子类,
        //而不需要检查所有的基类,而不用担心版本脆性,从一个基类切换到处理模式。
        //我们正在流处理(bool)模式——这就是所有子类都应该把他们清理从V2。
        /// <summary>
        /// 关闭当前流并释放与之关联的所有资源（如套接字和文件句柄）。不直接调用此方法，而应确保流得以正确释放。
        /// </summary>
        public virtual void Close()
        {
            /* These are correct, but we'd have to fix PipeStream & NetworkStream very carefully.
            这些都是正确的,但我们必须解决PipeStream & NetworkStream很小心。
            Contract.Ensures(CanRead == false);
            Contract.Ensures(CanWrite == false);
            Contract.Ensures(CanSeek == false);
            */

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放由 Stream 使用的所有资源。
        /// </summary>
        public void Dispose()
        {
            /* These are correct, but we'd have to fix PipeStream & NetworkStream very carefully.
            Contract.Ensures(CanRead == false);
            Contract.Ensures(CanWrite == false);
            Contract.Ensures(CanSeek == false);
            */

            Close();
        }

        /// <summary>
        /// 释放由 Stream 占用的非托管资源，还可以释放托管资源。
        /// </summary>
        /// <param name="disposing">若要释放托管资源和非托管资源，则为 true；若仅释放非托管资源，则为 false。</param>
        protected virtual void Dispose(bool disposing)
        {
            // Note: Never change this to call other virtual methods on Stream
            // like Write, since the state on subclasses has already been 
            // torn down.  This is the last code to run on cleanup for a stream.
            //应通过指定释放所有资源 true 为 disposing。当 disposing 是 true,流还可以确保数据刷新到基础的缓冲区，并访问其他可终结的对象。这不可能从一个终结器，由于缺乏终结器之间顺序进行调用时。
            //如果您的流使用操作系统句柄来与其源进行通信，请考虑使用的子类 SafeHandle 为此目的。
            //调用此方法由公共 Dispose 方法和 Finalize 方法。 Dispose 调用受保护 Dispose 方法替换 disposing 参数设置为 true。 Finalize 调用 Dispose 与 disposing 设置为 false。

        }

        /// <summary>
        /// 当在派生类中重写时，将清除该流的所有缓冲区，并使得所有缓冲数据被写入到基础设备。
        /// </summary>
        public abstract void Flush();

#if FEATURE_ASYNC_IO
        /// <summary>
        /// 异步清除此流的所有缓冲区并导致所有缓冲数据都写入基础设备中。
        /// </summary>
        /// <returns>表示异步刷新操作的任务。</returns>
        [HostProtection(ExternalThreading=true)]
        [ComVisible(false)]
        public Task FlushAsync()
        {
            return FlushAsync(CancellationToken.None);
        }

        /// <summary>
        /// 异步清理这个流的所有缓冲区，并使所有缓冲数据写入基础设备，并且监控取消请求。
        /// </summary>
        /// <param name="cancellationToken">要监视取消请求的标记。默认值为 None。</param>
        /// <returns>表示异步刷新操作的任务。</returns>
        [HostProtection(ExternalThreading=true)]
        [ComVisible(false)]
        public virtual Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(state => ((Stream)state).Flush(), this,
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
#endif // FEATURE_ASYNC_IO

        [Obsolete("CreateWaitHandle will be removed eventually.  Please use \"new ManualResetEvent(false)\" instead.")]
        protected virtual WaitHandle CreateWaitHandle()
        {
            Contract.Ensures(Contract.Result<WaitHandle>() != null);
            return new ManualResetEvent(false);
        }

        /// <summary>
        /// 开始异步读操作。（考虑使用 ReadAsync 进行替换；请参见“备注”部分。）
        /// </summary>
        /// <param name="buffer">数据读入的缓冲区。</param>
        /// <param name="offset">buffer 中的字节偏移量，从该偏移量开始写入从流中读取的数据。</param>
        /// <param name="count">最多读取的字节数。</param>
        /// <param name="callback">可选的异步回调，在完成读取时调用。</param>
        /// <param name="state">一个用户提供的对象，它将该特定的异步读取请求与其他请求区别开来。</param>
        /// <returns></returns>
        [HostProtection(ExternalThreading=true)]
        public virtual IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
            Contract.Ensures(Contract.Result<IAsyncResult>() != null);
            return BeginReadInternal(buffer, offset, count, callback, state, serializeAsynchronously: false);
            //bool serializeAsynchronously = false;
            //return BeginReadInternal(buffer, offset, count, callback, state, serializeAsynchronously);
        }

        /// <summary>
        /// 内部实现BeginRead
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="serializeAsynchronously">true为异步，false为同步</param>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        internal IAsyncResult BeginReadInternal(byte[] buffer, int offset, int count, AsyncCallback callback, Object state, bool serializeAsynchronously)
        {
            Contract.Ensures(Contract.Result<IAsyncResult>() != null);
            if (!CanRead) __Error.ReadNotSupported();// 如果不能读的话，则提示不支持读

#if !NEW_EXPERIMENTAL_ASYNC_IO
            return BlockingBeginRead(buffer, offset, count, callback, state);
#else

            // Mango did not do Async IO.
            // 进行对WP8的兼容性判断
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                return BlockingBeginRead(buffer, offset, count, callback, state);
            }

            // To avoid a race with a stream's position pointer & generating ---- 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will block the application's main
            // thread if it does a second IO request until the first one completes.
            // 设置信号量，避免线程竞争产生的错误
            var semaphore = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreTask = null;//信号进程
            // 是否异步序列化
            if (serializeAsynchronously)
            {
                semaphoreTask = semaphore.WaitAsync();//输入 SemaphoreSlim 的异步等待。
            }
            else
            {
                semaphore.Wait();// 阻止当前线程，直至它可进入 SemaphoreSlim 为止。
            }

            // Create the task to asynchronously do a Read.  This task serves both
            // as the asynchronous work item and as the IAsyncResult returned to the user.
            // 创建异步读取的任务。这个任务服务于异步工作项和将IAsyncResult返回给用户
            var asyncResult = new ReadWriteTask(true /*isRead*/, delegate
            {
                // The ReadWriteTask stores all of the parameters to pass to Read.
                // As we're currently inside of it, we can get the current task
                // and grab the parameters from it.
                // ReadWriteTask存储传到Read的所有参数
                // 当我们正在处理它时，我们将获取当前任务和从他中获取参数
                // 获取当前执行Task实例
                var thisTask = Task.InternalCurrent as ReadWriteTask;
                Contract.Assert(thisTask != null, "Inside ReadWriteTask, InternalCurrent should be the ReadWriteTask");

                // Do the Read and return the number of bytes read
                // 读取并返回读取bytes数目
                var bytesRead = thisTask._stream.Read(thisTask._buffer, thisTask._offset, thisTask._count);
                thisTask.ClearBeginState(); // 仅仅是帮助减轻一些内存压力
                return bytesRead;
            }, state, this, buffer, offset, count, callback);

            // 安排任务
            if (semaphoreTask != null)
                RunReadWriteTaskWhenReady(semaphoreTask, asyncResult);
            else
                RunReadWriteTask(asyncResult);

            
            return asyncResult; // return it
#endif
        }

        /// <summary>
        /// 等待挂起的异步读取完成。（考虑使用 ReadAsync 进行替换；请参见“备注”部分。）
        /// </summary>
        /// <param name="asyncResult">对要完成的挂起异步请求的引用。</param>
        /// <returns>从流中读取的字节数，介于零 (0) 和所请求的字节数之间。流仅在流结尾返回零 (0)，否则在至少有 1 个字节可用之前应一直进行阻止。</returns>
        public virtual int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)// 判断IAsyncResult是否为空
                throw new ArgumentNullException("asyncResult");
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.EndContractBlock();

#if !NEW_EXPERIMENTAL_ASYNC_IO
            return BlockingEndRead(asyncResult);
#else
            // Mango did not do async IO.
            // 针对于WP8的块级EndRead
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                return BlockingEndRead(asyncResult);
            }
            // 获取当前的读取或写入任务（readTask）
            var readTask = _activeReadWriteTask;

            if (readTask == null)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
            }
            else if (readTask != asyncResult)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
            }
            else if (!readTask._isRead)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
            }
            
            try 
            {
                return readTask.GetAwaiter().GetResult(); // block until completion, then get result / propagate any exception 直到完成，返回结果 / 传播任何异常
            }
            finally
            {
                _activeReadWriteTask = null;// 将当前的读取或写入任务清空
                Contract.Assert(_asyncActiveSemaphore != null, "Must have been initialized in order to get here.");
                _asyncActiveSemaphore.Release();// 将同步锁释放
            }
#endif
        }


#if FEATURE_ASYNC_IO
        /// <summary>
        /// 从当前流异步读取字节序列，并将流中的位置提升读取的字节数。
        /// </summary>
        /// <param name="buffer">数据写入的缓冲区。</param>
        /// <param name="offset">buffer 中的字节偏移量，从该偏移量开始写入从流中读取的数据。</param>
        /// <param name="count">最多读取的字节数。</param>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public Task<int> ReadAsync(Byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None);
        }

        /// <summary>
        /// 从当前流异步读取字节的序列，将流中的位置提升读取的字节数，并监视取消请求。
        /// </summary>
        /// <param name="buffer">数据写入的缓冲区。</param>
        /// <param name="offset">buffer 中的字节偏移量，从该偏移量开始写入从流中读取的数据。</param>
        /// <param name="count">最多读取的字节数。</param>
        /// <param name="cancellationToken">要监视取消请求的标记。  默认值为 None。</param>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public virtual Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If cancellation was requested, bail early with an already completed task.
            // Otherwise, return a task that represents the Begin/End methods.
            // 如果cancellation请求，保释早期已经完成的任务
            // 否则，返回一个代表Begin/End方法的任务
            return cancellationToken.IsCancellationRequested
                        ? Task.FromCancellation<int>(cancellationToken)
                        : BeginEndReadAsync(buffer, offset, count);
        }

        /// <summary>
        /// 开始和结束异步读取
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private Task<Int32> BeginEndReadAsync(Byte[] buffer, Int32 offset, Int32 count)
        {
            return TaskFactory<Int32>.FromAsyncTrim(this,
                        new ReadWriteParameters { Buffer = buffer, Offset = offset, Count = count },
                        (stream, args, callback, state) => stream.BeginRead(args.Buffer, args.Offset, args.Count, callback, state),
                        (stream, asyncResult) => stream.EndRead(asyncResult)
                );// 缓存编译器
        }

        private struct ReadWriteParameters // struct for arguments to Read and Write calls
        {
            internal byte[] Buffer;
            internal int Offset;
            internal int Count;
        }
#endif //FEATURE_ASYNC_IO


        /// <summary>
        /// 开始异步写操作。（考虑使用 WriteAsync 进行替换；请参见“备注”部分。）
        /// </summary>
        /// <param name="buffer">从中写入数据的缓冲区。</param>
        /// <param name="offset">buffer 中的字节偏移量，从此处开始写入。 </param>
        /// <param name="count">最多写入的字节数。</param>
        /// <param name="callback">可选的异步回调，在完成写入时调用。</param>
        /// <param name="state">一个用户提供的对象，它将该特定的异步写入请求与其他请求区别开来。</param>
        /// <returns>表示异步写入的 IAsyncResult（可能仍处于挂起状态）。</returns>
        [HostProtection(ExternalThreading=true)]
        public virtual IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
            Contract.Ensures(Contract.Result<IAsyncResult>() != null);
            return BeginWriteInternal(buffer, offset, count, callback, state, serializeAsynchronously: false);
        }

        /// <summary>
        /// 内部实现BeginWrite
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="serializeAsynchronously"></param>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        internal IAsyncResult BeginWriteInternal(byte[] buffer, int offset, int count, AsyncCallback callback, Object state, bool serializeAsynchronously)
        {
            Contract.Ensures(Contract.Result<IAsyncResult>() != null);
            if (!CanWrite) __Error.WriteNotSupported();
#if !NEW_EXPERIMENTAL_ASYNC_IO
            return BlockingBeginWrite(buffer, offset, count, callback, state);
#else

            // Mango did not do Async IO.
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                return BlockingBeginWrite(buffer, offset, count, callback, state);
            }

            // To avoid a race with a stream's position pointer & generating ---- 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will block the application's main
            // thread if it does a second IO request until the first one completes.
            var semaphore = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreTask = null;
            if (serializeAsynchronously)
            {
                semaphoreTask = semaphore.WaitAsync(); // kick off the asynchronous wait, but don't block
            }
            else
            {
                semaphore.Wait(); // synchronously wait here
            }

            // Create the task to asynchronously do a Write.  This task serves both
            // as the asynchronous work item and as the IAsyncResult returned to the user.
            var asyncResult = new ReadWriteTask(false /*isRead*/, delegate
            {
                // The ReadWriteTask stores all of the parameters to pass to Write.
                // As we're currently inside of it, we can get the current task
                // and grab the parameters from it.
                var thisTask = Task.InternalCurrent as ReadWriteTask;
                Contract.Assert(thisTask != null, "Inside ReadWriteTask, InternalCurrent should be the ReadWriteTask");

                // Do the Write
                thisTask._stream.Write(thisTask._buffer, thisTask._offset, thisTask._count);  
                thisTask.ClearBeginState(); // just to help alleviate some memory pressure
                return 0; // not used, but signature requires a value be returned
            }, state, this, buffer, offset, count, callback);

            // Schedule it
            if (semaphoreTask != null)
                RunReadWriteTaskWhenReady(semaphoreTask, asyncResult);
            else
                RunReadWriteTask(asyncResult);

            return asyncResult; // return it
#endif
        }

#if NEW_EXPERIMENTAL_ASYNC_IO
        /// <summary>
        /// 运行读写任务
        /// </summary>
        /// <param name="asyncWaiter">异步等待任务</param>
        /// <param name="readWriteTask">读写任务</param>
        private void RunReadWriteTaskWhenReady(Task asyncWaiter, ReadWriteTask readWriteTask)
        {
            Contract.Assert(readWriteTask != null);  // Should be Contract.Requires, but CCRewrite is doing a poor job with
                                                     // preconditions（先决条件） in async methods that await.  Mike & Manuel are aware(意识到). (10/6/2011, bug 290222)
            Contract.Assert(asyncWaiter != null);    // Ditto

            // If the wait has already complete, run the task.
            // 如果等待早已完成，将运行任务
            if (asyncWaiter.IsCompleted)
            {
                Contract.Assert(asyncWaiter.IsRanToCompletion, "The semaphore wait should always complete successfully.");
                RunReadWriteTask(readWriteTask);
            }
            else  // 否则,等待轮到我们,然后运行这个任务。
            {
                asyncWaiter.ContinueWith((t, state) =>
                    {
                        Contract.Assert(t.IsRanToCompletion, "The semaphore wait should always complete successfully.");
                        var tuple = (Tuple<Stream,ReadWriteTask>)state;
                        tuple.Item1.RunReadWriteTask(tuple.Item2); // RunReadWriteTask(readWriteTask);
                    }, Tuple.Create<Stream,ReadWriteTask>(this, readWriteTask),
                default(CancellationToken),
                TaskContinuationOptions.ExecuteSynchronously, 
                TaskScheduler.Default);
            }
        }

        /// <summary>
        /// 运行读写任务
        /// </summary>
        /// <param name="readWriteTask"></param>
        private void RunReadWriteTask(ReadWriteTask readWriteTask)
        {
            Contract.Requires(readWriteTask != null);
            Contract.Assert(_activeReadWriteTask == null, "Expected no other readers or writers");

            // Schedule the task.  ScheduleAndStart must happen after the write to _activeReadWriteTask to avoid a race.
            // Internally, we're able to directly call ScheduleAndStart rather than Start, avoiding
            // two interlocked operations.  However, if ReadWriteTask is ever changed to use
            // a cancellation token, this should be changed to use Start.
            // 安排的任务。ScheduleAndStart必须发生写_activeReadWriteTask后,避免一场竞争。
            // 在内部,我们能够直接调用ScheduleAndStart而不是开始,避免两个联锁操作。
            // 然而,如果ReadWriteTask改为使用取消令牌,这应该改为使用开始。
            _activeReadWriteTask = readWriteTask; // store the task so that EndXx can validate it's given the right one 
            readWriteTask.m_taskScheduler = TaskScheduler.Default;
            readWriteTask.ScheduleAndStart(needsProtection: false);
        }
#endif
        /// <summary>
        /// 结束异步写操作。（考虑使用 WriteAsync 进行替换；请参见“备注”部分。）
        /// </summary>
        /// <param name="asyncResult">对未完成的异步 I/O 请求的引用。</param>
        public virtual void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult==null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();

#if !NEW_EXPERIMENTAL_ASYNC_IO
            BlockingEndWrite(asyncResult);
#else            

            // Mango did not do Async IO.
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                BlockingEndWrite(asyncResult);
                return;
            }            

            var writeTask = _activeReadWriteTask;
            if (writeTask == null)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
            }
            else if (writeTask != asyncResult)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
            }
            else if (writeTask._isRead)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
            }

            try 
            {
                writeTask.GetAwaiter().GetResult(); // block until completion, then propagate any exceptions
                Contract.Assert(writeTask.Status == TaskStatus.RanToCompletion);
            }
            finally
            {
                _activeReadWriteTask = null;
                Contract.Assert(_asyncActiveSemaphore != null, "Must have been initialized in order to get here.");
                _asyncActiveSemaphore.Release();
            }
#endif
        }

#if NEW_EXPERIMENTAL_ASYNC_IO
        // Task used by BeginRead / BeginWrite to do Read / Write asynchronously.
        // A single instance of this task serves(服务) four purposes(目的):
        // 1. The work item scheduled to run the Read / Write operation
        // 2. The state holding the arguments to be passed to Read / Write
        // 3. The IAsyncResult returned from BeginRead / BeginWrite
        // 4. The completion action that runs to invoke the user-provided callback.
        // This last item is a bit tricky.  Before the AsyncCallback is invoked, the
        // IAsyncResult must have completed, so we can't just invoke the handler
        // from within the task, since it is the IAsyncResult, and thus it's not
        // yet completed.  Instead, we use AddCompletionAction to install this
        // task as its own completion handler.  That saves the need to allocate
        // a separate(分离) completion handler, it guarantees(保证) that the task will
        // have completed by the time the handler is invoked, and it allows
        // the handler to be invoked synchronously upon the completion of the
        // task.  This all enables BeginRead / BeginWrite to be implemented
        // with a single allocation.
        private sealed class ReadWriteTask : Task<int>, ITaskCompletionAction
        {
            internal readonly bool _isRead;
            internal Stream _stream;
            internal byte [] _buffer;
            internal int _offset;
            internal int _count;
            /// <summary>
            /// 异步回调委托
            /// </summary>
            private AsyncCallback _callback;
            /// <summary>
            /// 当前线程的执行上下文
            /// </summary>
            private ExecutionContext _context;

            /// <summary>
            /// Used to allow the args to Read/Write to be made available(有效地) for GC
            /// 用于允许参数读写为GC可用
            /// </summary>
            internal void ClearBeginState()
            {
                _stream = null;
                _buffer = null;
            }

            /// <summary>
            /// 读或写任务的构造函数
            /// </summary>
            /// <param name="isRead">是否是读任务</param>
            /// <param name="function">读写任务函数</param>
            /// <param name="state"></param>
            /// <param name="stream">数据流</param>
            /// <param name="buffer">缓冲</param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            /// <param name="callback">异步回调</param>
            [SecuritySafeCritical] // necessary for EC.Capture(计算机俘获)
            [MethodImpl(MethodImplOptions.NoInlining)]
            public ReadWriteTask(
                bool isRead,
                Func<object,int> function, object state,
                Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback) :
                base(function, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach)
            {
                Contract.Requires(function != null);
                Contract.Requires(stream != null);
                Contract.Requires(buffer != null);
                Contract.EndContractBlock();

                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

                // 存储参数
                _isRead = isRead;
                _stream = stream;
                _buffer = buffer;
                _offset = offset;
                _count = count;

                // If a callback was provided, we need to:
                // 如果一个回调被提供，我们需要：
                // - Store the user-provided handler
                // - 存储用户提供的handler
                // - Capture an ExecutionContext under which to invoke the handler
                // -捕获的ExecutionContext在调用处理程序
                // - Add this task as its own completion handler so that the Invoke method
                //   will run the callback when this task completes.
                // - 添加这个任务作为他自己完毕handler因此调用方法将运行回调，当这个任务完成
                if (callback != null)
                {
                    _callback = callback;
                    //从当前线程捕获执行上下文，并赋值
                    _context = ExecutionContext.Capture(ref stackMark, 
                        ExecutionContext.CaptureOptions.OptimizeDefaultCase | ExecutionContext.CaptureOptions.IgnoreSyncCtx);
                    base.AddCompletionAction(this);//添加完成行为
                }
            }

            /// <summary>
            /// 调用异步回调
            /// </summary>
            /// <param name="completedTask"></param>
            [SecurityCritical] // necessary for CoreCLR
            private static void InvokeAsyncCallback(object completedTask)
            {
                // 将object转换为ReadWriteTask，并获取他的回调函数调用
                var rwc = (ReadWriteTask)completedTask;
                var callback = rwc._callback;
                rwc._callback = null;
                callback(rwc);
            }

            /// <summary>
            /// 新上下文回调的方法
            /// </summary>
            [SecurityCritical] // necessary for CoreCLR
            private static ContextCallback s_invokeAsyncCallback;
            
            /// <summary>
            /// ITaskCompletionAction.Invoke显式接口实现
            /// </summary>
            /// <param name="completingTask">完成任务</param>
            [SecuritySafeCritical] // necessary for ExecutionContext.Run
            void ITaskCompletionAction.Invoke(Task completingTask)
            {
                // Get the ExecutionContext.  If there is none, just run the callback
                // directly, passing in the completed task as the IAsyncResult.
                // If there is one, process it with ExecutionContext.Run.
                // 获取当前线程的上下文，如果为空，仅直接运行回调，传入IAsyncResult完成任务。
                // 如果有的话,与ExecutionContext.Run处理它。
                var context = _context;
                if (context == null) 
                {
                    var callback = _callback;
                    _callback = null;
                    callback(completingTask);
                }
                else 
                {
                    _context = null;
        
                    var invokeAsyncCallback = s_invokeAsyncCallback;
                    if (invokeAsyncCallback == null) s_invokeAsyncCallback = invokeAsyncCallback = InvokeAsyncCallback; // benign ----

                    using (context) ExecutionContext.Run(context, invokeAsyncCallback, this, true);// 使用ExecutionContext.Run调用函数
                }
            }
        }
#endif

#if !FEATURE_PAL && FEATURE_ASYNC_IO
        /// <summary>
        /// 将字节序列异步写入当前流，并将流的当前位置提升写入的字节数。
        /// </summary>
        /// <param name="buffer">从中写入数据的缓冲区。</param>
        /// <param name="offset">buffer 中的从零开始的字节偏移量，从此处开始将字节复制到该流。</param>
        /// <param name="count">最多写入的字节数。</param>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public Task WriteAsync(Byte[] buffer, int offset, int count)
        {
            return WriteAsync(buffer, offset, count, CancellationToken.None);
        }

        /// <summary>
        /// 将字节的序列异步写入当前流，将该流中的当前位置向前移动写入的字节数，并监视取消请求。
        /// </summary>
        /// <param name="buffer">从中写入数据的缓冲区。</param>
        /// <param name="offset">buffer 中的从零开始的字节偏移量，从此处开始将字节复制到该流。</param>
        /// <param name="count">最多写入的字节数。</param>
        /// <param name="cancellationToken">要监视取消请求的标记。  默认值为 None。  </param>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public virtual Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If cancellation was requested, bail early with an already completed task.
            // Otherwise, return a task that represents the Begin/End methods.
            return cancellationToken.IsCancellationRequested
                        ? Task.FromCancellation(cancellationToken)
                        : BeginEndWriteAsync(buffer, offset, count);
        }


        private Task BeginEndWriteAsync(Byte[] buffer, Int32 offset, Int32 count)
        {            
            return TaskFactory<VoidTaskResult>.FromAsyncTrim(
                        this, new ReadWriteParameters { Buffer=buffer, Offset=offset, Count=count },
                        (stream, args, callback, state) => stream.BeginWrite(args.Buffer, args.Offset, args.Count, callback, state), // cached by compiler
                        (stream, asyncResult) => // cached by compiler
                        {
                            stream.EndWrite(asyncResult);
                            return default(VoidTaskResult);
                        });
        }
#endif // !FEATURE_PAL && FEATURE_ASYNC_IO

        /// <summary>
        /// 当在派生类中重写时，设置当前流中的位置。
        /// </summary>
        /// <param name="offset">相对于 origin 参数的字节偏移量。</param>
        /// <param name="origin">SeekOrigin 类型的值，指示用于获取新位置的参考点。</param>
        /// <returns>当前流中的新位置。</returns>
        public abstract long Seek(long offset, SeekOrigin origin);

        /// <summary>
        /// 当在派生类中重写时，设置当前流的长度。
        /// </summary>
        /// <param name="value">所需的当前流的长度（以字节表示）。</param>
        public abstract void SetLength(long value);

        /// <summary>
        /// 当在派生类中重写时，从当前流读取字节序列，并将此流中的位置提升读取的字节数。
        /// </summary>
        /// <param name="buffer">字节数组。此方法返回时，该缓冲区包含指定的字符数组，该数组的 offset 和 (offset + count -1) 之间的值由从当前源中读取的字节替换。</param>
        /// <param name="offset">buffer 中的从零开始的字节偏移量，从此处开始存储从当前流中读取的数据。</param>
        /// <param name="count">要从当前流中最多读取的字节数。</param>
        /// <returns></returns>
        public abstract int Read([In, Out] byte[] buffer, int offset, int count);

        // Reads one byte from the stream by calling Read(byte[], int, int). 
        // Will return an unsigned byte cast to an int or -1 on end of stream.
        // This implementation does not perform well because it allocates a new
        // byte[] each time you call it, and should be overridden by any 
        // subclass that maintains an internal buffer.  Then, it can help perf
        // significantly for people who are reading one byte at a time.
        /// <summary>
        /// 从流中读取一个字节，并将流内的位置向前提升一个字节，或者如果已到达流结尾，则返回 -1。
        /// </summary>
        /// <returns>强制转换为 Int32 的无符号字节，如果到达流的末尾，则为 -1。</returns>
        public virtual int ReadByte()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < 256);

            byte[] oneByteArray = new byte[1];
            int r = Read(oneByteArray, 0, 1);
            if (r==0)
                return -1;
            return oneByteArray[0];
        }

        /// <summary>
        /// 当在派生类中重写时，向当前流中写入字节序列，并将此流中的当前位置提升写入的字节数。
        /// </summary>
        /// <param name="buffer">字节数组。此方法将 count 个字节从 buffer 复制到当前流。</param>
        /// <param name="offset">buffer 中的从零开始的字节偏移量，从此处开始将字节复制到当前流。</param>
        /// <param name="count">要写入当前流的字节数。</param>
        public abstract void Write(byte[] buffer, int offset, int count);

        // Writes one byte from the stream by calling Write(byte[], int, int).
        // This implementation does not perform well because it allocates a new
        // byte[] each time you call it, and should be overridden by any 
        // subclass that maintains an internal buffer.  Then, it can help perf
        // significantly for people who are writing one byte at a time.
        /// <summary>
        /// 将一个字节写入流内的当前位置，并将流内的位置向前提升一个字节。
        /// </summary>
        /// <param name="value">要写入流中的字节。</param>
        public virtual void WriteByte(byte value)
        {
            byte[] oneByteArray = new byte[1];
            oneByteArray[0] = value;
            Write(oneByteArray, 0, 1);
        }
        /// <summary>
        /// 在指定的 Stream 对象周围创建线程安全（同步）包装。
        /// </summary>
        /// <param name="stream">要同步的 Stream 对象。</param>
        /// <returns>一个线程安全的 Stream 对象。</returns>
        [HostProtection(Synchronization=true)]
        public static Stream Synchronized(Stream stream) 
        {
            if (stream==null)
                throw new ArgumentNullException("stream");
            Contract.Ensures(Contract.Result<Stream>() != null);
            Contract.EndContractBlock();
            if (stream is SyncStream)
                return stream;
            
            return new SyncStream(stream);
        }

#if !FEATURE_PAL  // This method shouldn't have been exposed in Dev10 (we revised object invariants after locking down).
        [Obsolete("Do not call or override this method.")]
        protected virtual void ObjectInvariant() 
        {
        }
#endif
        /// <summary>
        /// 块级BeginRead(针对于WP8的异步处理)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        internal IAsyncResult BlockingBeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
            Contract.Ensures(Contract.Result<IAsyncResult>() != null);

            // To avoid a race with a stream's position pointer & generating ---- 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will block the application's main
            // thread and do the IO synchronously.  
            // This can't perform well - use a different approach.
            SynchronousAsyncResult asyncResult; 
            try {
                int numRead = Read(buffer, offset, count);
                asyncResult = new SynchronousAsyncResult(numRead, state);
            }
            catch (IOException ex) {
                asyncResult = new SynchronousAsyncResult(ex, state, isWrite: false);
            }
            
            if (callback != null) {
                callback(asyncResult);
            }

            return asyncResult;
        }

        internal static int BlockingEndRead(IAsyncResult asyncResult)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);

            return SynchronousAsyncResult.EndRead(asyncResult);
        }

        internal IAsyncResult BlockingBeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
            Contract.Ensures(Contract.Result<IAsyncResult>() != null);

            // To avoid a race with a stream's position pointer & generating ---- 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will block the application's main
            // thread and do the IO synchronously.  
            // This can't perform well - use a different approach.
            SynchronousAsyncResult asyncResult;
            try {
                Write(buffer, offset, count);
                asyncResult = new SynchronousAsyncResult(state);
            }
            catch (IOException ex) {
                asyncResult = new SynchronousAsyncResult(ex, state, isWrite: true);
            }

            if (callback != null) {
                callback(asyncResult);
            }

            return asyncResult;
        }

        internal static void BlockingEndWrite(IAsyncResult asyncResult)
        {
            SynchronousAsyncResult.EndWrite(asyncResult);
        }

        /// <summary>
        /// 空流
        /// </summary>
        [Serializable]
        private sealed class NullStream : Stream
        {
            internal NullStream() {}

            public override bool CanRead {
                [Pure]
                get { return true; }
            }

            public override bool CanWrite {
                [Pure]
                get { return true; }
            }

            public override bool CanSeek {
                [Pure]
                get { return true; }
            }

            public override long Length {
                get { return 0; }
            }

            public override long Position {
                get { return 0; }
                set {}
            }

            protected override void Dispose(bool disposing)
            {
                // Do nothing - we don't want NullStream singleton (static) to be closable
                // 不在任何事 - 我们不想单例NullStream关闭
            }

            public override void Flush()
            {
            }

#if FEATURE_ASYNC_IO
            [ComVisible(false)]
            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return cancellationToken.IsCancellationRequested ?
                    Task.FromCancellation(cancellationToken) :
                    Task.CompletedTask;
            }
#endif // FEATURE_ASYNC_IO

            [HostProtection(ExternalThreading = true)]
            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                if (!CanRead) __Error.ReadNotSupported();

                return BlockingBeginRead(buffer, offset, count, callback, state);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                Contract.EndContractBlock();

                return BlockingEndRead(asyncResult);
            }

            [HostProtection(ExternalThreading = true)]
            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                if (!CanWrite) __Error.WriteNotSupported();

                return BlockingBeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                Contract.EndContractBlock();

                BlockingEndWrite(asyncResult);
            }

            public override int Read([In, Out] byte[] buffer, int offset, int count)
            {
                return 0;
            }

#if FEATURE_ASYNC_IO
            [ComVisible(false)]
            public override Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var nullReadTask = s_nullReadTask;
                if (nullReadTask == null) 
                    s_nullReadTask = nullReadTask = new Task<int>(false, 0, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, CancellationToken.None); // benign ----
                return nullReadTask;
            }
            private static Task<int> s_nullReadTask;
#endif //FEATURE_ASYNC_IO

            public override int ReadByte()
            {
                return -1;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }

#if FEATURE_ASYNC_IO
            [ComVisible(false)]
            public override Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return cancellationToken.IsCancellationRequested ?
                    Task.FromCancellation(cancellationToken) :
                    Task.CompletedTask;
            }
#endif // FEATURE_ASYNC_IO

            public override void WriteByte(byte value)
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return 0;
            }

            public override void SetLength(long length)
            {
            }
        }


        /// <summary>用作IAsyncResult对象当使用异步IO流基类上的方法。</summary>
        internal sealed class SynchronousAsyncResult : IAsyncResult {
            
            private readonly Object _stateObject;// 状态对象        
            private readonly bool _isWrite;// 是否是写
            private ManualResetEvent _waitHandle;// 等待句柄
            private ExceptionDispatchInfo _exceptionInfo; // 异常信息

            private bool _endXxxCalled;// 结束调用
            private Int32 _bytesRead;// 读取字节

            internal SynchronousAsyncResult(Int32 bytesRead, Object asyncStateObject) {
                _bytesRead = bytesRead;
                _stateObject = asyncStateObject;
                //_isWrite = false;
            }

            internal SynchronousAsyncResult(Object asyncStateObject) {
                _stateObject = asyncStateObject;
                _isWrite = true;
            }

            internal SynchronousAsyncResult(Exception ex, Object asyncStateObject, bool isWrite) {
                _exceptionInfo = ExceptionDispatchInfo.Capture(ex);
                _stateObject = asyncStateObject;
                _isWrite = isWrite;                
            }

            public bool IsCompleted {
                // We never hand out objects of this type to the user before the synchronous IO completed:
                get { return true; }
            }

            public WaitHandle AsyncWaitHandle {
                get {
                    return LazyInitializer.EnsureInitialized(ref _waitHandle, () => new ManualResetEvent(true));                    
                }
            }

            public Object AsyncState {
                get { return _stateObject; }
            }

            public bool CompletedSynchronously {
                get { return true; }
            }

            /// <summary>
            /// 如果错误则抛出
            /// </summary>
            internal void ThrowIfError() {
                if (_exceptionInfo != null)
                    _exceptionInfo.Throw();
            }                        

            /// <summary>
            /// 结束读取
            /// </summary>
            /// <param name="asyncResult"></param>
            /// <returns></returns>
            internal static Int32 EndRead(IAsyncResult asyncResult) {

                SynchronousAsyncResult ar = asyncResult as SynchronousAsyncResult;
                if (ar == null || ar._isWrite)
                    __Error.WrongAsyncResult();

                if (ar._endXxxCalled)
                    __Error.EndReadCalledTwice();

                ar._endXxxCalled = true;

                ar.ThrowIfError();
                return ar._bytesRead;
            }

            /// <summary>
            /// 结束写入
            /// </summary>
            /// <param name="asyncResult"></param>
            internal static void EndWrite(IAsyncResult asyncResult) {

                SynchronousAsyncResult ar = asyncResult as SynchronousAsyncResult;
                if (ar == null || !ar._isWrite)
                    __Error.WrongAsyncResult();

                if (ar._endXxxCalled)
                    __Error.EndWriteCalledTwice();

                ar._endXxxCalled = true;

                ar.ThrowIfError();
            }
        }   // class SynchronousAsyncResult


        // SyncStream is a wrapper around a stream that takes 
        // a lock for every operation making it thread safe.
        /// <summary>
        /// SyncStream包装流,为每个操作需要一个锁是线程安全的。
        /// </summary>
        [Serializable]
        internal sealed class SyncStream : Stream, IDisposable
        {
            private Stream _stream;
            [NonSerialized]
            private bool? _overridesBeginRead;
            [NonSerialized]
            private bool? _overridesBeginWrite;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="stream">要被封装stream</param>
            internal SyncStream(Stream stream)
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");
                Contract.EndContractBlock();
                _stream = stream;
            }
            
            public override bool CanRead {
                [Pure]
                get { return _stream.CanRead; }
            }
        
            public override bool CanWrite {
                [Pure]
                get { return _stream.CanWrite; }
            }
        
            public override bool CanSeek {
                [Pure]
                get { return _stream.CanSeek; }
            }
        
            [ComVisible(false)]
            public override bool CanTimeout {
                [Pure]
                get {
                    return _stream.CanTimeout;
                }
            }

            public override long Length {
                get {
                    lock(_stream) {
                        return _stream.Length;
                    }
                }
            }
        
            public override long Position {
                get {
                    lock(_stream) {
                        return _stream.Position;
                    }
                }
                set {
                    lock(_stream) {
                        _stream.Position = value;
                    }
                }
            }

            [ComVisible(false)]
            public override int ReadTimeout {
                get {
                    return _stream.ReadTimeout;
                }
                set {
                    _stream.ReadTimeout = value;
                }
            }

            [ComVisible(false)]
            public override int WriteTimeout {
                get {
                    return _stream.WriteTimeout;
                }
                set {
                    _stream.WriteTimeout = value;
                }
            }

            // In the off chance that some wrapped stream has different 
            // semantics for Close vs. Dispose, let's preserve that.
            /// <summary>
            /// 关闭当前流并释放与之关联的所有资源（如套接字和文件句柄）。不直接调用此方法，而应确保流得以正确释放。
            /// </summary>
            public override void Close()
            {
                lock(_stream) {
                    try {
                        _stream.Close();
                    }
                    finally {
                        base.Dispose(true);
                    }
                }
            }
            
            protected override void Dispose(bool disposing)
            {
                lock(_stream) {
                    try {
                        // Explicitly pick up a potentially methodimpl'ed Dispose
                        if (disposing)
                            ((IDisposable)_stream).Dispose();
                    }
                    finally {
                        base.Dispose(disposing);
                    }
                }
            }
        
            public override void Flush()
            {
                lock(_stream)
                    _stream.Flush();
            }
        
            public override int Read([In, Out]byte[] bytes, int offset, int count)
            {
                lock(_stream)
                    return _stream.Read(bytes, offset, count);
            }
        
            public override int ReadByte()
            {
                lock(_stream)
                    return _stream.ReadByte();
            }

            /// <summary>
            /// 重载BeginMethod方法的内部实现
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            private static bool OverridesBeginMethod(Stream stream, string methodName)
            {
                Contract.Requires(stream != null, "Expected a non-null stream.");
                Contract.Requires(methodName == "BeginRead" || methodName == "BeginWrite",
                    "Expected BeginRead or BeginWrite as the method name to check.");

                // Get all of the methods on the underlying stream
                // 获取底层流的所有方法
                var methods = stream.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

                // If any of the methods have the desired name and are defined on the base Stream
                // Type, then the method was not overridden.  If none of them were defined on the
                // base Stream, then it must have been overridden.
                // 如果任何方法所需的基本流类型名称和定义,然后没有覆盖的方法。
                // 如果没有一个定义在基流,那么它必须被覆盖。
                foreach (var method in methods)
                {
                    if (method.DeclaringType == typeof(Stream) &&
                        method.Name == methodName)
                    {
                        return false;
                    }
                }
                return true;
            }
        
            [HostProtection(ExternalThreading=true)]
            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                // Lazily-initialize whether the wrapped stream overrides BeginRead
                if (_overridesBeginRead == null)
                {
                    _overridesBeginRead = OverridesBeginMethod(_stream, "BeginRead");
                }

                lock (_stream)
                {
                    // If the Stream does have its own BeginRead implementation, then we must use that override.
                    // If it doesn't, then we'll use the base implementation, but we'll make sure that the logic
                    // which ensures only one asynchronous operation does so with an asynchronous wait rather
                    // than a synchronous wait.  A synchronous wait will result in a deadlock condition, because
                    // the EndXx method for the outstanding async operation won't be able to acquire the lock on
                    // _stream due to this call blocked while holding the lock.
                    // 如果流有自己的BeginRead实现,那么我们必须使用覆盖。如果没有,那么我们将使用基本实现,
                    // 但我们会确保只有一个异步操作的逻辑与异步等待这样做,而不是一个同步等。
                    // 同步会导致死锁条件等,因为优秀的异步操作的EndXx方法无法获得锁_stream由于这个调用阻塞而持有的锁。
                    return _overridesBeginRead.Value ?
                        _stream.BeginRead(buffer, offset, count, callback, state) :
                        _stream.BeginReadInternal(buffer, offset, count, callback, state, serializeAsynchronously: true);
                }
            }
        
            public override int EndRead(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.EndContractBlock();

                lock(_stream)
                    return _stream.EndRead(asyncResult);
            }
        
            public override long Seek(long offset, SeekOrigin origin)
            {
                lock(_stream)
                    return _stream.Seek(offset, origin);
            }
        
            public override void SetLength(long length)
            {
                lock(_stream)
                    _stream.SetLength(length);
            }
        
            public override void Write(byte[] bytes, int offset, int count)
            {
                lock(_stream)
                    _stream.Write(bytes, offset, count);
            }
        
            public override void WriteByte(byte b)
            {
                lock(_stream)
                    _stream.WriteByte(b);
            }
        
            [HostProtection(ExternalThreading=true)]
            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                // Lazily-initialize whether the wrapped stream overrides BeginWrite
                if (_overridesBeginWrite == null)
                {
                    _overridesBeginWrite = OverridesBeginMethod(_stream, "BeginWrite");
                }

                lock (_stream)
                {
                    // If the Stream does have its own BeginWrite implementation, then we must use that override.
                    // If it doesn't, then we'll use the base implementation, but we'll make sure that the logic
                    // which ensures only one asynchronous operation does so with an asynchronous wait rather
                    // than a synchronous wait.  A synchronous wait will result in a deadlock condition, because
                    // the EndXx method for the outstanding async operation won't be able to acquire the lock on
                    // _stream due to this call blocked while holding the lock.
                    return _overridesBeginWrite.Value ?
                        _stream.BeginWrite(buffer, offset, count, callback, state) :
                        _stream.BeginWriteInternal(buffer, offset, count, callback, state, serializeAsynchronously: true);
                }
            }
            
            public override void EndWrite(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                Contract.EndContractBlock();

                lock(_stream)
                    _stream.EndWrite(asyncResult);
            }
        }
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(Stream))]
    internal abstract class StreamContract : Stream
    {
        public override long Seek(long offset, SeekOrigin origin)
        {
            Contract.Ensures(Contract.Result<long>() >= 0);
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Position {
            get {
                Contract.Ensures(Contract.Result<long>() >= 0);
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override bool CanRead {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite {
            get { throw new NotImplementedException(); }
        }

        public override bool CanSeek {
            get { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get {
                Contract.Ensures(Contract.Result<long>() >= 0);
                throw new NotImplementedException();
            }
        }
    }
#endif  // CONTRACTS_FULL
}
