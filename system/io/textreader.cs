// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  TextReader
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: Abstract base class for all Text-only Readers.
** Subclasses will include StreamReader & StringReader.
**
**
===========================================================*/

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
#if FEATURE_ASYNC_IO
using System.Threading;
using System.Threading.Tasks;
#endif

namespace System.IO {
    // This abstract base class represents a reader that can read a sequential
    // stream of characters.  This is not intended for reading bytes -
    // there are methods on the Stream class to read bytes.
    // A subclass must minimally implement the Peek() and Read() methods.
    //
    // This class is intended for character input, not bytes.  
    // There are methods on the Stream class for reading bytes. 
    [Serializable]
    [ComVisible(true)]
#if FEATURE_REMOTING
    public abstract class TextReader : MarshalByRefObject, IDisposable {
#else // FEATURE_REMOTING
    public abstract class TextReader : IDisposable {
#endif // FEATURE_REMOTING

#if FEATURE_ASYNC_IO
        //[NonSerialized]
        //private static Func<object, string> _ReadLineDelegate = state => ((TextReader)state).ReadLine();
        /// <summary>
        /// 读取行的委托
        /// </summary>
        [NonSerialized]
        private static Func<object, string> _ReadLineDelegate = state => ((TextReader)state).ReadLine();

        /// <summary>
        /// 读取的委托
        /// </summary>
        private static Func<object, int> _ReadDelegate = state =>
         {
             Tuple<TextReader, char[], int, int> tuple = (Tuple<TextReader, char[], int, int>)state;
             return tuple.Item1.Read(tuple.Item2, tuple.Item3, tuple.Item4);
         };
#endif //FEATURE_ASYNC_IO
        /// <summary>
        /// 初始化空TextReader
        /// </summary>
        public static readonly TextReader Null = new NullTextReader();
    
        protected TextReader() {}

        // Closes this TextReader and releases any system resources associated with the
        // TextReader. Following a call to Close, any operations on the TextReader
        // may raise exceptions.
        // 
        // This default method is empty, but descendant classes can override the
        // method to provide the appropriate functionality.

        // Returns the next available character without actually reading it from
        // the input stream. The current position of the TextReader is not changed by
        // this operation. The returned value is -1 if no further characters are
        // available.
        // 
        // This default method simply returns -1.
        //

        /// <summary>
        /// 读取下一个字符，而不更改读取器状态或字符源。
        /// 返回下一个可用字符，而实际上并不从读取器中读取此字符。
        /// </summary>
        /// <returns>一个表示下一个要读取的字符的整数；如果没有更多可读取的字符或该读取器不支持查找，则为 -1。</returns>
        [Pure]
        public virtual int Peek()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            return -1;
        }

        /// <summary>
        /// 关闭 TextReader 并释放与该 TextReader 关联的所有系统资源。
        /// </summary>
        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放由 TextReader 对象使用的所有资源。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放由 TextReader 占用的非托管资源，还可以释放托管资源。
        /// </summary>
        /// <param name="disposing">真正的释放托管和非托管资源;如果为 false 则释放只有非托管的资源。</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        // Reads the next character from the input stream. The returned value is
        // -1 if no further characters are available.
        // 
        // This default method simply returns -1.
        /// <summary>
        /// 读取文本读取器中的下一个字符并使该字符的位置前移一个字符。
        /// </summary>
        /// <returns>文本读取器中的下一个字符，或为 -1（如果没有更多可用字符）。默认实现将返回 -1。</returns>
        public virtual int Read()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            return -1;
        }

        // Reads a block of characters. This method will read up to
        // count characters from this TextReader into the
        // buffer character array starting at position
        // index. Returns the actual number of characters read.
        // 读取字符的块。
        // 此方法将读取指望从这输送字符到缓冲区的字符数组的位置索引处开始。返回读取的字符的实际数目。
        /// <summary>
        /// 从当前读取器中读取指定数目的字符并从指定索引开始将该数据写入缓冲区。
        /// </summary>
        /// <param name="buffer">此方法返回时，包含指定的字符数组，该数组的 index 和 (index + count - 1) 之间的值由从当前源中读取的字符替换。</param>
        /// <param name="index">在 buffer 中开始写入的位置。</param>
        /// <param name="count">要读取的最大字符数。如果在将指定数量的字符读入缓冲区之前就已达读取器的末尾，则返回该方法。</param>
        /// <returns></returns>
        public virtual int Read([In,Out] char[] buffer,int index,int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= Contract.OldValue(count));

            int n = 0;
            do// 将textreader读取出来
            {
                int ch = Read();
                if (ch == 1) break;
                buffer[index + n++] = (char)ch;
            } while (n < count);
            return n;
        }

        /// <summary>
        /// 读取从当前位置到文本读取器末尾的所有字符并将它们作为一个字符串返回。
        /// </summary>
        /// <returns>一个包含从当前位置到文本读取器末尾的所有字符的字符串。</returns>
        public virtual String ReadToEnd()
        {
            Contract.Ensures(Contract.Result<String>() != null);

            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            while((len=Read(chars,0,chars.Length))!=0)// 将textreader中的字节按4096的数组长度读取出来，直到完毕
            {
                sb.Append(chars, 0, len);
            }
            return sb.ToString();// 返回字符串
        }

        // Blocking version of read.  Returns only when count
        // characters have been read or the end of the file was reached.
        /// <summary>
        /// 从当前文本读取器中读取指定的最大字符数并从指定索引处开始将该数据写入缓冲区。
        /// </summary>
        /// <param name="buffer">此方法返回时，此参数包含指定的字符数组，该数组中从 index 到 (index + count -1) 之间的值由从当前源中读取的字符替换。</param>
        /// <param name="index">在 buffer 中开始写入的位置。</param>
        /// <param name="count">要读取的最大字符数。</param>
        /// <returns>已读取的字符数。该数字将小于或等于 count，具体取决于是否所有的输入字符都已读取。</returns>
        public virtual int ReadBlock([In,Out] char[] buffer,int index,int count)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);

            int i, n = 0;
            do
            {
                n += (i = Read(buffer, index + n, count - n));
            } while (i > 0 && n < count);
            return n;
        }

        // Reads a line. A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //
        /// <summary>
        /// 从文本读取器中读取一行字符并将数据作为字符串返回。
        /// </summary>
        /// <returns>读取器中的下一行，或为 null （如果已读取了所有字符）。</returns>
        public virtual String ReadLine()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int ch = Read();
                if (ch == -1) break;
                if(ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && Peek() == '\n') Read();
                    return sb.ToString();
                }
                sb.Append((char)ch);
            }
            if (sb.Length > 0) return sb.ToString();
            return null;
        }

#if FEATURE_ASYNC_IO
        #region Task based Async APIs
        /// <summary>
        /// 异步读取一行字符并将数据作为字符串返回。
        /// </summary>
        /// <returns>表示异步读取操作的任务。 TResult 参数的值包含来自文本读取器的下一行或为 null 如果读取所有字符。</returns>
        [HostProtection(ExternalThreading =true)]
        [ComVisible(false)]
        public virtual Task<String> ReadLineAsync()
        {
            return Task<String>.Factory.StartNew(_ReadLineDelegate, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        /// <summary>
        /// 异步读取从当前位置到文本读取器末尾的所有字符并将它们作为一个字符串返回。
        /// </summary>
        /// <returns>表示异步读取操作的任务。 TResult 参数值包括字符串来自当前位置到结束文本读取器字符。</returns>
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public async virtual Task<String> ReadToEndAsync()
        {
            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder();
            while((len = await ReadAsyncInternal(chars,0,chars.Length).ConfigureAwait(false))!=0)
            {
                sb.Append(chars, 0, len);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 异步从当前文本读取器中读取指定最大字符数并从指定索引开始将该数据写入缓冲区。
        /// </summary>
        /// <param name="buffer">此方法返回时，包含指定的字符数组，该数组的 index 和 (index + count - 1) 之间的值由从当前源中读取的字符替换。</param>
        /// <param name="index">在 buffer 中开始写入的位置。</param>
        /// <param name="count">要读取的最大字符数。如果在将指定数量的字符读入缓冲区之前已到达文本的末尾，则当前方法将返回。</param>
        /// <returns></returns>
        [HostProtection(ExternalThreading =true)]
        [ComVisible(false)]
        public virtual Task<int> ReadAsync(char[] buffer,int index,int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if(index < 0|| count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            return ReadAsyncInternal(buffer, index, count);
        }

        /// <summary>
        /// 内部的异步读取
        /// </summary>
        /// <param name="buffer">此方法返回时，包含指定的字符数组，该数组的 index 和 (index + count - 1) 之间的值由从当前源中读取的字符替换。</param>
        /// <param name="index">在 buffer 中开始写入的位置。</param>
        /// <param name="count">要读取的最大字符数。如果在将指定数量的字符读入缓冲区之前已到达文本的末尾，则当前方法将返回。</param>
        /// <returns></returns>
        internal virtual Task<int> ReadAsyncInternal(char[] buffer,int index,int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - index >= count);

            Tuple<TextReader, char[], int, int> tuple = new Tuple<TextReader, char[], int, int>(this, buffer, index, count);
            return Task<int>.Factory.StartNew(_ReadDelegate, tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        /// <summary>
        /// 异步从当前文本读取器中读取指定最大字符数并从指定索引开始将该数据写入缓冲区。
        /// </summary>
        /// <param name="buffer">此方法返回时，包含指定的字符数组，该数组的 index 和 (index + count - 1) 之间的值由从当前源中读取的字符替换。</param>
        /// <param name="index">在 buffer 中开始写入的位置。</param>
        /// <param name="count">要读取的最大字符数。如果在将指定数量的字符读入缓冲区之前已到达文本的末尾，则当前方法将返回。</param>
        /// <returns></returns>
        [HostProtection(ExternalThreading=true)]
        [ComVisible(false)]
        public virtual Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (buffer==null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

            Contract.EndContractBlock();

            return ReadBlockAsyncInternal(buffer, index, count);
         }

        [HostProtection(ExternalThreading=true)]
        private async Task<int> ReadBlockAsyncInternal(char[] buffer, int index, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - index >= count);

            int i, n = 0;
            do
            {
                i = await ReadAsyncInternal(buffer, index + n, count - n).ConfigureAwait(false);
                n += i;
            } while (i > 0 && n < count);

            return n;
        }
        #endregion
#endif //FEATURE_ASYNC_IO
        /// <summary>
        /// 在指定 TextReader 周围创建线程安全包装。
        /// </summary>
        /// <param name="reader">要同步的 TextReader。</param>
        /// <returns>一个线程安全的 TextReader。</returns>
        [HostProtection(Synchronization=true)]
        public static TextReader Synchronized(TextReader reader) 
        {
            if (reader==null)
                throw new ArgumentNullException("reader");
            Contract.Ensures(Contract.Result<TextReader>() != null);
            Contract.EndContractBlock();

            if (reader is SyncTextReader)
                return reader;
            
            return new SyncTextReader(reader);
        }
        
        /// <summary>
        /// 空的TextReader
        /// </summary>
        [Serializable]
        private sealed class NullTextReader : TextReader
        {
            public NullTextReader(){}

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int Read(char[] buffer, int index, int count) 
            {
                return 0;
            }
            
            public override String ReadLine() 
            {
                return null;
            }
        }

        /// <summary>
        /// 同步TextReader
        /// 对用于线程同步的方法添加了[MethodImplAttribute(MethodImplOptions.Synchronized)]
        /// </summary>
        [Serializable]
        internal sealed class SyncTextReader : TextReader 
        {
            internal TextReader _in;
            
            internal SyncTextReader(TextReader t) 
            {
                _in = t;        
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override void Close() 
            {
                // So that any overriden Close() gets run
                _in.Close();
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            protected override void Dispose(bool disposing) 
            {
                // Explicitly pick up a potentially methodimpl'ed Dispose
                if (disposing)
                    ((IDisposable)_in).Dispose();
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int Peek() 
            {
                return _in.Peek();
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int Read() 
            {
                return _in.Read();
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int Read([In, Out] char[] buffer, int index, int count) 
            {
                return _in.Read(buffer, index, count);
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int ReadBlock([In, Out] char[] buffer, int index, int count) 
            {
                return _in.ReadBlock(buffer, index, count);
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override String ReadLine() 
            {
                return _in.ReadLine();
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override String ReadToEnd() 
            {
                return _in.ReadToEnd();
            }
#if FEATURE_ASYNC_IO

            //
            // On SyncTextReader all APIs should run synchronously, even the async ones.
            //

            [ComVisible(false)]
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<String> ReadLineAsync()
            {
                return Task.FromResult(ReadLine());
            }

            [ComVisible(false)]
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<String> ReadToEndAsync()
            {
                return Task.FromResult(ReadToEnd());
            }

            [ComVisible(false)]
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
            {
                if (buffer==null)
                    throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (buffer.Length - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

                Contract.EndContractBlock();

                return Task.FromResult(ReadBlock(buffer, index, count));
            }

            [ComVisible(false)]
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                if (buffer==null)
                    throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (buffer.Length - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                return Task.FromResult(Read(buffer, index, count));
            }
#endif //FEATURE_ASYNC_IO
        }
    }
}
