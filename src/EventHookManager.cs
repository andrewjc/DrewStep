namespace ConsoleApp
{
    using System;
    using System.Runtime.InteropServices;

    public delegate void WindowEvent(IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    public class WindowEventHookManager
    {
        private static WindowEventHookManager _instance;
        private static readonly object Lock = new object();
        private WinEventDelegate _winEventDelegate;
        private IntPtr _hookCreateIntPtr;
        private IntPtr _hookDestroyIntPtr;
        public event WindowEvent OnWindowCreated;
        public event WindowEvent OnWindowDestroyed;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        private WindowEventHookManager()
        {
            _winEventDelegate = new WinEventDelegate(WinEventProc);
            _hookCreateIntPtr = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
            _hookDestroyIntPtr = SetWinEventHook(EVENT_OBJECT_DESTROY, EVENT_OBJECT_DESTROY, IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        public static WindowEventHookManager Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new WindowEventHookManager();
                    }
                    return _instance;
                }
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_OBJECT_CREATE)
            {
                OnWindowCreated?.Invoke(hwnd, idObject, idChild, dwEventThread, dwmsEventTime);
            }
            else if (eventType == EVENT_OBJECT_DESTROY)
            {
                OnWindowDestroyed?.Invoke(hwnd, idObject, idChild, dwEventThread, dwmsEventTime);
            }
        }

        public void Dispose()
        {
            if (_hookCreateIntPtr != IntPtr.Zero)
            {
                UnhookWinEvent(_hookCreateIntPtr);
                _hookCreateIntPtr = IntPtr.Zero;
            }
            if (_hookDestroyIntPtr != IntPtr.Zero)
            {
                UnhookWinEvent(_hookDestroyIntPtr);
                _hookDestroyIntPtr = IntPtr.Zero;
            }
        }
    }
}
