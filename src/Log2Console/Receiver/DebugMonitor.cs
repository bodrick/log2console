using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Log2Console.Receiver
{
    /// <summary>
    ///     Delegate used when firing DebugMonitor.OnOutputDebug event
    /// </summary>
    public delegate void OnOutputDebugStringHandler(int pid, string text);

    /// <summary>
    ///     This class captures all strings passed to <c>OutputDebugString</c> when
    ///     the application is not debugged.
    /// </summary>
    /// <remarks>
    ///     This class is a port of Microsofts Visual Studio's C++ example "dbmon", which
    ///     can be found at
    ///     <c>http://msdn.microsoft.com/library/default.asp?url=/library/en-us/vcsample98/html/vcsmpdbmon.asp</c>.
    /// </remarks>
    /// <remarks>
    ///     <code>
    /// 			public static void Main(string[] args) {
    /// 				DebugMonitor.Start();
    /// 				DebugMonitor.OnOutputDebugString += new OnOutputDebugStringHandler(OnOutputDebugString);
    /// 				Console.WriteLine("Press 'Enter' to exit.");
    /// 				Console.ReadLine();
    /// 				DebugMonitor.Stop();
    /// 			}
    /// 			
    /// 			private static void OnOutputDebugString(int pid, string text) {
    /// 				Console.WriteLine(DateTime.Now + ": " + text);
    /// 			}
    /// 		</code>
    /// </remarks>
    public static class DebugMonitor
    {
        /// <summary>
        ///     Event handle for slot 'DBWIN_BUFFER_READY'
        /// </summary>
        private static IntPtr _mAckEvent = IntPtr.Zero;

        /// <summary>
        ///     Event handle for slot 'DBWIN_DATA_READY'
        /// </summary>
        private static IntPtr _mReadyEvent = IntPtr.Zero;

        /// <summary>
        ///     Handle for our shared file
        /// </summary>
        private static IntPtr _mSharedFile = IntPtr.Zero;

        /// <summary>
        ///     Handle for our shared memory
        /// </summary>
        private static IntPtr _mSharedMem = IntPtr.Zero;

        /// <summary>
        ///     Our capturing thread
        /// </summary>
        private static Thread _mCapturer;

        /// <summary>
        ///     Our synchronization root
        /// </summary>
        private static readonly object MSyncRoot = new object();

        /// <summary>
        ///     Mutex for singleton check
        /// </summary>
        private static Mutex _mMutex;

        /// <summary>
        ///     Fired if an application calls <c>OutputDebugString</c>
        /// </summary>
        public static event OnOutputDebugStringHandler OnOutputDebugString;

        /// <summary>
        ///     Starts this debug monitor
        /// </summary>
        public static void Start()
        {
            lock (MSyncRoot)
            {
                if (_mCapturer != null)
                {
                    throw new ApplicationException("This DebugMonitor is already started.");
                }

                // Check for supported operating system. Mono (at least with *nix) won't support
                // our P/Invoke calls.
                if (!Environment.OSVersion.ToString().Contains("Microsoft"))
                {
                    throw new NotSupportedException(
                        "This DebugMonitor is only supported on Microsoft operating systems.");
                }

                // Check for multiple instances. As the README.TXT of the msdn 
                // example notes it is possible to have multiple debug monitors
                // listen on OutputDebugString, but the message will be randomly
                // distributed among all running instances so this won't be
                // such a good idea.				
                _mMutex = new Mutex(false, typeof(DebugMonitor).Namespace, out var createdNew);
                if (!createdNew)
                {
                    throw new ApplicationException("There is already an instance of 'DbMon.NET' running.");
                }

                var sd = new SECURITY_DESCRIPTOR();

                // Initialize the security descriptor.
                if (!InitializeSecurityDescriptor(ref sd, SECURITY_DESCRIPTOR_REVISION))
                {
                    throw CreateApplicationException("Failed to initializes the security descriptor.");
                }

                // Set information in a discretionary access control list
                if (!SetSecurityDescriptorDacl(ref sd, true, IntPtr.Zero, false))
                {
                    throw CreateApplicationException("Failed to initializes the security descriptor");
                }

                var sa = new SECURITY_ATTRIBUTES();

                // Create the event for slot 'DBWIN_BUFFER_READY'
                _mAckEvent = CreateEvent(ref sa, false, false, "DBWIN_BUFFER_READY");
                if (_mAckEvent == IntPtr.Zero)
                {
                    throw CreateApplicationException("Failed to create event 'DBWIN_BUFFER_READY'");
                }

                // Create the event for slot 'DBWIN_DATA_READY'
                _mReadyEvent = CreateEvent(ref sa, false, false, "DBWIN_DATA_READY");
                if (_mReadyEvent == IntPtr.Zero)
                {
                    throw CreateApplicationException("Failed to create event 'DBWIN_DATA_READY'");
                }

                // Get a handle to the readable shared memory at slot 'DBWIN_BUFFER'.
                _mSharedFile = CreateFileMapping(new IntPtr(-1), ref sa, PageProtection.ReadWrite, 0, 4096,
                    "DBWIN_BUFFER");
                if (_mSharedFile == IntPtr.Zero)
                {
                    throw CreateApplicationException("Failed to create a file mapping to slot 'DBWIN_BUFFER'");
                }

                // Create a view for this file mapping so we can access it
                _mSharedMem = MapViewOfFile(_mSharedFile, SECTION_MAP_READ, 0, 0, 512);
                if (_mSharedMem == IntPtr.Zero)
                {
                    throw CreateApplicationException("Failed to create a mapping view for slot 'DBWIN_BUFFER'");
                }

                // Start a new thread where we can capture the output
                // of OutputDebugString calls so we don't block here.
                _mCapturer = new Thread(Capture);
                _mCapturer.Start();
            }
        }

        /// <summary>
        ///     Captures
        /// </summary>
        private static void Capture()
        {
            try
            {
                // Everything after the first DWORD is our debugging text
                var pString = new IntPtr(_mSharedMem.ToInt32() + Marshal.SizeOf(typeof(int)));

                while (true)
                {
                    SetEvent(_mAckEvent);

                    var ret = WaitForSingleObject(_mReadyEvent, INFINITE);

                    // if we have no capture set it means that someone
                    // called 'Stop()' and is now waiting for us to exit
                    // this endless loop.
                    if (_mCapturer == null)
                    {
                        break;
                    }

                    if (ret == WAIT_OBJECT_0)
                        // The first DWORD of the shared memory buffer contains
                        // the process ID of the client that sent the debug string.
                    {
                        FireOnOutputDebugString(Marshal.ReadInt32(_mSharedMem),
                            Marshal.PtrToStringAnsi(pString));
                    }
                }
            }
            finally
            {
                Dispose();
            }
        }

        private static void FireOnOutputDebugString(int pid, string text)
        {
            // Raise event if we have any listeners

#if !DEBUG
			try {
#endif
            OnOutputDebugString?.Invoke(pid, text);
#if !DEBUG
			} catch (Exception ex) {
				Console.WriteLine("An 'OnOutputDebugString' handler failed to execute: " + ex.ToString());
			}
#endif
        }

        /// <summary>
        ///     Dispose all resources
        /// </summary>
        private static void Dispose()
        {
            // Close AckEvent
            if (_mAckEvent != IntPtr.Zero)
            {
                if (!CloseHandle(_mAckEvent)) throw CreateApplicationException("Failed to close handle for 'AckEvent'");
                _mAckEvent = IntPtr.Zero;
            }

            // Close ReadyEvent
            if (_mReadyEvent != IntPtr.Zero)
            {
                if (!CloseHandle(_mReadyEvent))
                {
                    throw CreateApplicationException("Failed to close handle for 'ReadyEvent'");
                }

                _mReadyEvent = IntPtr.Zero;
            }

            // Close SharedFile
            if (_mSharedFile != IntPtr.Zero)
            {
                if (!CloseHandle(_mSharedFile))
                {
                    throw CreateApplicationException("Failed to close handle for 'SharedFile'");
                }

                _mSharedFile = IntPtr.Zero;
            }


            // Unmap SharedMem
            if (_mSharedMem != IntPtr.Zero)
            {
                if (!UnmapViewOfFile(_mSharedMem))
                {
                    throw CreateApplicationException("Failed to unmap view for slot 'DBWIN_BUFFER'");
                }

                _mSharedMem = IntPtr.Zero;
            }

            // Close our mutex
            if (_mMutex != null)
            {
                _mMutex.Close();
                _mMutex = null;
            }
        }

        /// <summary>
        ///     Stops this debug monitor. This call we block the executing thread
        ///     until this debug monitor is stopped.
        /// </summary>
        public static void Stop()
        {
            lock (MSyncRoot)
            {
                if (_mCapturer == null)
                {
                    throw new ObjectDisposedException("DebugMonitor", "This DebugMonitor is not running.");
                }

                _mCapturer = null;
                PulseEvent(_mReadyEvent);
                while (_mAckEvent != IntPtr.Zero)
                {
                }
            }
        }

        /// <summary>
        ///     Helper to create a new application exception, which has automatically the
        ///     last win 32 error code appended.
        /// </summary>
        /// <param name="text">text</param>
        private static ApplicationException CreateApplicationException(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text), "'text' may not be empty or null.");
            }

            return new ApplicationException($"{text}. Last Win32 Error was {Marshal.GetLastWin32Error()}");
        }

        #region Win32 API Imports

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_DESCRIPTOR
        {
            public readonly byte revision;
            public readonly byte size;
            public readonly short control;
            public readonly IntPtr owner;
            public readonly IntPtr group;
            public readonly IntPtr sacl;
            public readonly IntPtr dacl;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public readonly int nLength;
            public readonly IntPtr lpSecurityDescriptor;
            public readonly int bInheritHandle;
        }

        [Flags]
        private enum PageProtection : uint
        {
            NoAccess = 0x01,
            Readonly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            Guard = 0x100,
            NoCache = 0x200,
            WriteCombine = 0x400
        }


        private const int WAIT_OBJECT_0 = 0;
        private const uint INFINITE = 0xFFFFFFFF;
        private const int ERROR_ALREADY_EXISTS = 183;

        private const uint SECURITY_DESCRIPTOR_REVISION = 1;

        private const uint SECTION_MAP_READ = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint
                dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow,
            uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool InitializeSecurityDescriptor(ref SECURITY_DESCRIPTOR sd, uint dwRevision);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetSecurityDescriptorDacl(ref SECURITY_DESCRIPTOR sd, bool daclPresent, IntPtr dacl,
            bool daclDefaulted);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateEvent(ref SECURITY_ATTRIBUTES sa, bool bManualReset, bool bInitialState,
            string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool PulseEvent(IntPtr hEvent);

        [DllImport("kernel32.dll")]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(IntPtr hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes, PageProtection flProtect, uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        private static extern int WaitForSingleObject(IntPtr handle, uint milliseconds);

        #endregion
    }
}