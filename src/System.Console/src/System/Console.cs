// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public static class Console
    {
        private const int DefaultConsoleBufferSize = 256; // default size of buffer used in stream readers/writers
        private static readonly object InternalSyncObject = new object(); // for synchronizing changing of Console's static fields
        private static TextReader _in;
        private static TextWriter _out, _error;

        private static ConsoleCancelEventHandler _cancelCallbacks;
        private static ConsolePal.ControlCHandlerRegistrar _registrar;

        internal static T EnsureInitialized<T>(ref T field, Func<T> initializer) where T : class
        {
            lock (InternalSyncObject)
            {
                T result = Volatile.Read(ref field);
                if (result == null)
                {
                    result = initializer();
                    Volatile.Write(ref field, result);
                }
                return result;
            }
        }

        public static TextReader In
        {
            get
            {
                return Volatile.Read(ref _in) ?? EnsureInitialized(ref _in, () =>
                {
                    return ConsolePal.GetOrCreateReader();
                });
            }
        }

        public static bool KeyAvailable
        {
            get
            {
                if (IsInputRedirected)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ConsoleKeyAvailableOnFile);
                }

                return ConsolePal.KeyAvailable;
            }
        }

        public static ConsoleKeyInfo ReadKey()
        {
            return ConsolePal.ReadKey(false);
        }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            return ConsolePal.ReadKey(intercept);
        }

        public static TextWriter Out
        {
            get { return Volatile.Read(ref _out) ?? EnsureInitialized(ref _out, () => CreateOutputWriter(OpenStandardOutput())); }
        }

        public static TextWriter Error
        {
            get { return Volatile.Read(ref _error) ?? EnsureInitialized(ref _error, () => CreateOutputWriter(OpenStandardError())); }
        }

        private static TextWriter CreateOutputWriter(Stream outputStream)
        {
            return SyncTextWriter.GetSynchronizedTextWriter(outputStream == Stream.Null ?
                StreamWriter.Null :
                new StreamWriter(
                    stream: outputStream,
                    encoding: ConsolePal.OutputEncoding,
                    bufferSize: DefaultConsoleBufferSize,
                    leaveOpen: true) { AutoFlush = true });
        }

        private static StrongBox<bool> _isStdInRedirected;
        private static StrongBox<bool> _isStdOutRedirected;
        private static StrongBox<bool> _isStdErrRedirected;

        public static bool IsInputRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdInRedirected) ??
                    EnsureInitialized(ref _isStdInRedirected, () => new StrongBox<bool>(ConsolePal.IsInputRedirectedCore()));
                return redirected.Value;
            }
        }

        public static bool IsOutputRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdOutRedirected) ??
                    EnsureInitialized(ref _isStdOutRedirected, () => new StrongBox<bool>(ConsolePal.IsOutputRedirectedCore()));
                return redirected.Value;
            }
        }

        public static bool IsErrorRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdErrRedirected) ??
                    EnsureInitialized(ref _isStdErrRedirected, () => new StrongBox<bool>(ConsolePal.IsErrorRedirectedCore()));
                return redirected.Value;
            }
        }

        public static ConsoleColor BackgroundColor
        {
            get { return ConsolePal.BackgroundColor; }
            set { ConsolePal.BackgroundColor = value; }
        }

        public static ConsoleColor ForegroundColor
        {
            get { return ConsolePal.ForegroundColor; }
            set { ConsolePal.ForegroundColor = value; }
        }

        public static void ResetColor()
        {
            ConsolePal.ResetColor();
        }

        public static int WindowWidth
        {
            get
            {
                return ConsolePal.WindowWidth;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, SR.ArgumentOutOfRange_NeedPosNum);
                }
                ConsolePal.WindowWidth = value;
            }
        }

        public static int WindowHeight
        {
            get
            {
                return ConsolePal.WindowHeight;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, SR.ArgumentOutOfRange_NeedPosNum);
                }
                ConsolePal.WindowHeight = value;
            }
        }

        public static bool CursorVisible
        {
            get { return ConsolePal.CursorVisible; }
            set { ConsolePal.CursorVisible = value; }
        }

        public static int CursorLeft
        {
            get { return ConsolePal.CursorLeft; }
            set { SetCursorPosition(value, CursorTop); }
        }

        public static int CursorTop
        {
            get { return ConsolePal.CursorTop; }
            set { SetCursorPosition(CursorLeft, value); }
        }

        private const int MaxConsoleTitleLength = 24500; // same value as in .NET Framework

        public static string Title
        {
            get { return ConsolePal.Title; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Length > MaxConsoleTitleLength)
                {
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_ConsoleTitleTooLong);
                }

                ConsolePal.Title = value;
            }
        }

        private const int MinBeepFrequency = 37;
        private const int MaxBeepFrequency = 32767;

        public static void Beep()
        {
            Beep(800, 200);
        }

        public static void Beep(int frequency, int duration)
        {
            if (frequency < MinBeepFrequency || frequency > MaxBeepFrequency)
            {
                throw new ArgumentOutOfRangeException("frequency", frequency, SR.Format(SR.ArgumentOutOfRange_BeepFrequency, MinBeepFrequency, MaxBeepFrequency));
            }
            if (duration <= 0)
            {
                throw new ArgumentOutOfRangeException("duration", duration, SR.ArgumentOutOfRange_NeedPosNum);
            }

            ConsolePal.Beep(frequency, duration);
        }

        public static void Clear()
        {
            ConsolePal.Clear();
        }

        public static void SetCursorPosition(int left, int top)
        {
            // Basic argument validation.  The PAL implementation may provide further validation.
            if (left < 0 || left >= short.MaxValue)
                throw new ArgumentOutOfRangeException("left", left, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (top < 0 || top >= short.MaxValue)
                throw new ArgumentOutOfRangeException("top", top, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);

            ConsolePal.SetCursorPosition(left, top);
        }

        public static event ConsoleCancelEventHandler CancelKeyPress
        {
            add
            {
                lock (InternalSyncObject)
                {
                    _cancelCallbacks += value;

                    // If we haven't registered our control-C handler, do it.
                    if (_registrar == null)
                    {
                        _registrar = new ConsolePal.ControlCHandlerRegistrar();
                        _registrar.Register();
                    }
                }
            }
            remove
            {
                lock (InternalSyncObject)
                {
                    _cancelCallbacks -= value;
                    if (_registrar != null && _cancelCallbacks == null)
                    {
                        _registrar.Unregister();
                        _registrar = null;
                    }
                }
            }
        }

        public static Stream OpenStandardInput()
        {
            return ConsolePal.OpenStandardInput();
        }

        public static Stream OpenStandardOutput()
        {
            return ConsolePal.OpenStandardOutput();
        }

        public static Stream OpenStandardError()
        {
            return ConsolePal.OpenStandardError();
        }

        public static void SetIn(TextReader newIn)
        {
            CheckNonNull(newIn, "newIn");
            newIn = SyncTextReader.GetSynchronizedTextReader(newIn);
            lock (InternalSyncObject) { _in = newIn; }
        }

        public static void SetOut(TextWriter newOut)
        {
            CheckNonNull(newOut, "newOut");
            newOut = SyncTextWriter.GetSynchronizedTextWriter(newOut);
            lock (InternalSyncObject) { _out = newOut; }
        }

        public static void SetError(TextWriter newError)
        {
            CheckNonNull(newError, "newError");
            newError = SyncTextWriter.GetSynchronizedTextWriter(newError);
            lock (InternalSyncObject) { _error = newError; }
        }

        private static void CheckNonNull(object obj, string paramName)
        {
            if (obj == null)
                throw new ArgumentNullException(paramName);
        }

        //
        // Give a hint to the code generator to not inline the common console methods. The console methods are 
        // not performance critical. It is unnecessary code bloat to have them inlined.
        //
        // Moreover, simple repros for codegen bugs are often console-based. It is tedious to manually filter out 
        // the inlined console writelines from them.
        //
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static int Read()
        {
            return In.Read();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static String ReadLine()
        {
            return In.ReadLine();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine()
        {
            Out.WriteLine();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(bool value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(char value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(char[] buffer)
        {
            Out.WriteLine(buffer);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(char[] buffer, int index, int count)
        {
            Out.WriteLine(buffer, index, count);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(decimal value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(double value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(float value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(int value)
        {
            Out.WriteLine(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(uint value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(long value)
        {
            Out.WriteLine(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(ulong value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(Object value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, Object arg0)
        {
            Out.WriteLine(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, Object arg0, Object arg1)
        {
            Out.WriteLine(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, Object arg0, Object arg1, Object arg2)
        {
            Out.WriteLine(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, params Object[] arg)
        {
            if (arg == null)                       // avoid ArgumentNullException from String.Format
                Out.WriteLine(format, null, null); // faster than Out.WriteLine(format, (Object)arg);
            else
                Out.WriteLine(format, arg);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, Object arg0)
        {
            Out.Write(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, Object arg0, Object arg1)
        {
            Out.Write(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, Object arg0, Object arg1, Object arg2)
        {
            Out.Write(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, params Object[] arg)
        {
            if (arg == null)                   // avoid ArgumentNullException from String.Format
                Out.Write(format, null, null); // faster than Out.Write(format, (Object)arg);
            else
                Out.Write(format, arg);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(bool value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(char value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(char[] buffer)
        {
            Out.Write(buffer);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(char[] buffer, int index, int count)
        {
            Out.Write(buffer, index, count);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(double value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(decimal value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(float value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(int value)
        {
            Out.Write(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(uint value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(long value)
        {
            Out.Write(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(ulong value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(Object value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String value)
        {
            Out.Write(value);
        }

        private sealed class ControlCDelegateData
        {
            private readonly ConsoleSpecialKey _controlKey;
            private readonly ConsoleCancelEventHandler _cancelCallbacks;

            internal bool Cancel;
            internal bool DelegateStarted;

            internal ControlCDelegateData(ConsoleSpecialKey controlKey, ConsoleCancelEventHandler cancelCallbacks)
            {
                _controlKey = controlKey;
                _cancelCallbacks = cancelCallbacks;
            }

            // This is the worker delegate that is called on the Threadpool thread to fire the actual events. It sets the DelegateStarted flag so
            // the thread that queued the work to the threadpool knows it has started (since it does not want to block indefinitely on the task
            // to start).
            internal void HandleBreakEvent()
            {
                DelegateStarted = true;
                var args = new ConsoleCancelEventArgs(_controlKey);
                _cancelCallbacks(null, args);
                Cancel = args.Cancel;
            }
        }

        internal static bool HandleBreakEvent(ConsoleSpecialKey controlKey)
        {
            // The thread that this gets called back on has a very small stack on some systems. There is
            // not enough space to handle a managed exception being caught and thrown. So, run a task
            // on the threadpool for the actual event callback.

            // To avoid the race condition between remove handler and raising the event
            ConsoleCancelEventHandler cancelCallbacks = Console._cancelCallbacks;
            if (cancelCallbacks == null)
            {
                return false;
            }

            var delegateData = new ControlCDelegateData(controlKey, cancelCallbacks);
            Task callBackTask = Task.Factory.StartNew(
                d => ((ControlCDelegateData)d).HandleBreakEvent(),
                delegateData,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            // Block until the delegate is done. We need to be robust in the face of the task not executing
            // but we also want to get control back immediately after it is done and we don't want to give the
            // handler a fixed time limit in case it needs to display UI. Wait on the task twice, once with a
            // timout and a second time without if we are sure that the handler actually started.
            TimeSpan controlCWaitTime = new TimeSpan(0, 0, 30); // 30 seconds
            callBackTask.Wait(controlCWaitTime);
            
            if (!delegateData.DelegateStarted)
            {
                Debug.Assert(false, "The task to execute the handler did not start within 30 seconds.");
                return false;
            }

            callBackTask.Wait();
            return delegateData.Cancel;
        }
    }
}
