using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowHelper
{
    /// <summary>
    /// 鼠标、键盘监听钩子组件
    /// </summary>
    public class UserActivityHook
    {
        #region 成员变量

        /// <summary>
        /// 
        /// </summary>
        private int hMouseHook = 0;
        /// <summary>
        /// 
        /// </summary>
        private int hKeyboardHook = 0;
        /// <summary>
        /// 
        /// </summary>
        private static HookProc MouseHookProcedure;
        /// <summary>
        /// 
        /// </summary>
        private static HookProc KeyboardHookProcedure;

        /// <summary>
        /// 鼠标钩子回调函数集合
        /// </summary>
        private static Dictionary<int, HookProc> DicMouseHookProc = new Dictionary<int, HookProc>();

        /// <summary>
        /// 键盘钩子回调函数集合
        /// </summary>
        private static Dictionary<int, HookProc> DivKeyboardHook = new Dictionary<int, HookProc>();

        /// <summary>
        /// 缓存上次按键的时间
        /// </summary>
        private DateTime PreKeyTime = DateTime.Now;

        /// <summary>
        /// 连续按键时，缓存每次的值
        /// </summary>
        private List<KeyboardHookStruct> ContinuityKeyboard = new List<KeyboardHookStruct>();
        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public UserActivityHook()
        {
        }

        #endregion

        #region 自定义事件

        /// <summary>
        /// 鼠标事件
        /// </summary>
        public event MouseEventHandler OnMouseActivity;

        /// <summary>
        /// 按下任意键盘键时发生
        /// </summary>
        public event KeyEventHandler KeyDown;

        /// <summary>
        /// 按下任意键盘键时发生
        /// </summary>
        public event KeyPressEventHandler KeyPress;

        /// <summary>
        /// 按下任意键盘键并释放时发生
        /// </summary>
        public event KeyEventHandler KeyUp;

        /// <summary>
        /// 模拟键盘被连续按下，通常是模拟设备，比如：扫码枪，门禁卡等~
        /// </summary>
        public event EventHandler<string> KeyContinuity;
        #endregion      

        #region 私有方法

        #region Windows structure definitions

        /// <summary>
        /// 鼠标点xy坐标
        /// </summary>
        /// <remarks>
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private class POINT
        {
            /// <summary>
            /// 
            /// </summary>
            public int x;
            /// <summary>
            /// 
            /// </summary>
            public int y;
        }

        /// <summary>
        /// 鼠标钩子结构体
        /// </summary>
        /// <remarks>
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private class MouseHookStruct
        {
            /// <summary>
            /// Specifies a POINT structure that contains the x- and y-coordinates of the cursor, in screen coordinates. 
            /// </summary>
            public POINT pt;
            /// <summary>
            /// Handle to the window that will receive the mouse message corresponding to the mouse event. 
            /// </summary>
            public int hwnd;
            /// <summary>
            /// Specifies the hit-test value. For a list of hit-test values, see the description of the WM_NCHITTEST message. 
            /// </summary>
            public int wHitTestCode;
            /// <summary>
            /// Specifies extra information associated with the message. 
            /// </summary>
            public int dwExtraInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private class MouseLLHookStruct
        {
            /// <summary>
            /// Specifies a POINT structure that contains the x- and y-coordinates of the cursor, in screen coordinates. 
            /// </summary>
            public POINT pt;
            /// <summary>
            /// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta. 
            /// The low-order word is reserved. A positive value indicates that the wheel was rotated forward, 
            /// away from the user; a negative value indicates that the wheel was rotated backward, toward the user. 
            /// One wheel click is defined as WHEEL_DELTA, which is 120. 
            ///If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
            /// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
            /// and the low-order word is reserved. This value can be one or more of the following values. Otherwise, mouseData is not used. 
            ///XBUTTON1
            ///The first X button was pressed or released.
            ///XBUTTON2
            ///The second X button was pressed or released.
            /// </summary>
            public int mouseData;
            /// <summary>
            /// Specifies the event-injected flag. An application can use the following value to test the mouse flags. Value Purpose 
            ///LLMHF_INJECTED Test the event-injected flag.  
            ///0
            ///Specifies whether the event was injected. The value is 1 if the event was injected; otherwise, it is 0.
            ///1-15
            ///Reserved.
            /// </summary>
            public int flags;
            /// <summary>
            /// Specifies the time stamp for this message.
            /// </summary>
            public int time;
            /// <summary>
            /// Specifies extra information associated with the message. 
            /// </summary>
            public int dwExtraInfo;
        }

        /// <summary>
        /// 键盘钩子结构
        /// </summary>
        /// <remarks>
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardHookStruct
        {
            /// <summary>
            /// Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
            /// </summary>
            public int vkCode;
            /// <summary>
            /// Specifies a hardware scan code for the key. 
            /// </summary>
            public int scanCode;
            /// <summary>
            /// Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
            /// </summary>
            public int flags;
            /// <summary>
            /// Specifies the time stamp for this message.
            /// </summary>
            public int time;
            /// <summary>
            /// Specifies extra information associated with the message. 
            /// </summary>
            public int dwExtraInfo;
        }
        #endregion

        #region Windows function imports
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idHook"></param>
        /// <param name="lpfn"></param>
        /// <param name="hMod"></param>
        /// <param name="dwThreadId"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto,
           CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            int dwThreadId);


        /// <summary>
        /// 获取模块实例句柄
        /// </summary>
        /// <param name="name">模块名称</param>
        /// <returns>模块实例句柄</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);

        /// <summary>
        /// 释放钩子
        /// </summary>
        /// <param name="idHook"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idHook"></param>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto,
             CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(
            int idHook,
            int nCode,
            int wParam,
            IntPtr lParam);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uVirtKey"></param>
        /// <param name="uScanCode"></param>
        /// <param name="lpbKeyState"></param>
        /// <param name="lpwTransKey"></param>
        /// <param name="fuState"></param>
        /// <returns></returns>
        [DllImport("user32")]
        private static extern int ToAscii(
            int uVirtKey,
            int uScanCode,
            byte[] lpbKeyState,
            byte[] lpwTransKey,
            int fuState);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pbKeyState"></param>
        /// <returns></returns>
        [DllImport("user32")]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        #endregion

        #region Windows constants

        /// <summary>
        /// 
        /// </summary>
        private const int WH_MOUSE_LL = 14;

        /// <summary>
        /// 
        /// </summary>
        private const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// 
        /// </summary>
        private const int WH_MOUSE = 7;

        /// <summary>
        /// 
        /// </summary>
        private const int WH_KEYBOARD = 2;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_MOUSEMOVE = 0x200;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_LBUTTONDOWN = 0x201;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_RBUTTONDOWN = 0x204;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_MBUTTONDOWN = 0x207;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_LBUTTONUP = 0x202;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_RBUTTONUP = 0x205;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_MBUTTONUP = 0x208;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_LBUTTONDBLCLK = 0x203;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_RBUTTONDBLCLK = 0x206;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_MBUTTONDBLCLK = 0x209;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_MOUSEWHEEL = 0x020A;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_KEYDOWN = 0x100;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_KEYUP = 0x101;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_SYSKEYDOWN = 0x104;

        /// <summary>
        /// 
        /// </summary>
        private const int WM_SYSKEYUP = 0x105;

        private const byte VK_SHIFT = 0x10;
        private const byte VK_CAPITAL = 0x14;
        private const byte VK_NUMLOCK = 0x90;

        #endregion

        /// <summary>
        /// 鼠标钩子回调函数
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            // if ok and someone listens to our events
            if ((nCode >= 0) && (OnMouseActivity != null))
            {
                //Marshall the data from callback.
                MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

                //detect button clicked
                MouseButtons button = MouseButtons.None;
                short mouseDelta = 0;
                switch (wParam)
                {
                    case WM_LBUTTONDOWN:
                        //case WM_LBUTTONUP: 
                        //case WM_LBUTTONDBLCLK: 
                        button = MouseButtons.Left;
                        break;
                    case WM_RBUTTONDOWN:
                        //case WM_RBUTTONUP: 
                        //case WM_RBUTTONDBLCLK: 
                        button = MouseButtons.Right;
                        break;
                    case WM_MOUSEWHEEL:
                        //If the message is WM_MOUSEWHEEL, the high-order word of mouseData member is the wheel delta. 
                        //One wheel click is defined as WHEEL_DELTA, which is 120. 
                        //(value >> 16) & 0xffff; retrieves the high-order word from the given 32-bit value
                        mouseDelta = (short)((mouseHookStruct.mouseData >> 16) & 0xffff);
                        //TODO: X BUTTONS (I havent them so was unable to test)
                        //If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, 
                        //or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
                        //and the low-order word is reserved. This value can be one or more of the following values. 
                        //Otherwise, mouseData is not used. 
                        break;
                }

                //double clicks
                int clickCount = 0;
                if (button != MouseButtons.None)
                    if (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK) clickCount = 2;
                    else clickCount = 1;

                //generate event 
                MouseEventArgs e = new MouseEventArgs(
                                                   button,
                                                   clickCount,
                                                   mouseHookStruct.pt.x,
                                                   mouseHookStruct.pt.y,
                                                   mouseDelta);

                if (clickCount >= 1)
                {
                    //raise it
                    if (OnMouseActivity != null)
                        OnMouseActivity(this, e);
                }

            }
            //call next hook
            return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
        }

        /// <summary>
        /// 键盘钩子回调函数
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            //indicates if any of underlaing events set e.Handled flag
            bool handled = false;
            //it was ok and someone listens to events
            if ((nCode >= 0) && (KeyDown != null || KeyUp != null || KeyPress != null))
            {
                //read structure KeyboardHookStruct at lParam
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                //raise KeyDown
                if (KeyDown != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyDown(this, e);
                    handled = handled || e.Handled;
                }

                // raise KeyPress
                if (KeyPress != null && wParam == WM_KEYDOWN)
                {
                    bool isDownShift = ((GetKeyState(VK_SHIFT) & 0x80) == 0x80 ? true : false);
                    bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0 ? true : false);

                    byte[] keyState = new byte[256];
                    GetKeyboardState(keyState);
                    byte[] inBuffer = new byte[2];
                    if (ToAscii(MyKeyboardHookStruct.vkCode,
                              MyKeyboardHookStruct.scanCode,
                              keyState,
                              inBuffer,
                              MyKeyboardHookStruct.flags) == 1)
                    {
                        char key = (char)inBuffer[0];
                        if ((isDownCapslock ^ isDownShift) && Char.IsLetter(key)) key = Char.ToUpper(key);
                        KeyPressEventArgs e = new KeyPressEventArgs(key);
                        KeyPress(this, e);
                        handled = handled || e.Handled;
                    }
                }

                // raise KeyUp
                if (KeyUp != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
                {
                    Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyUp(this, e);
                    handled = handled || e.Handled;
                }
            }

            //设备的连续模拟按键
            if ((nCode >= 0) && KeyContinuity != null)
            {
                var nowtime = DateTime.Now;
                var Milliseconds = (nowtime - PreKeyTime).TotalMilliseconds;
                //Debug.WriteLine($"间隔：{Milliseconds}");
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                //Debug.WriteLine($"dwExtraInfo：{MyKeyboardHookStruct.dwExtraInfo}\ttime：{MyKeyboardHookStruct.time}");

                if (Milliseconds <= 50)//连续按键，间隔小于20毫秒，视为模拟设备
                {
                    if (MyKeyboardHookStruct.flags > 0)//只处理按下并抬起的
                    {
                        Debug.WriteLine((char)MyKeyboardHookStruct.vkCode);
                        if (MyKeyboardHookStruct.vkCode == 13 && ContinuityKeyboard.Count != 0)
                        {
                            var str = string.Join("", ContinuityKeyboard.Select(c => (char)c.vkCode));
                            Debug.WriteLine($"事件通知：{str}");
                            KeyContinuity?.Invoke(this, str);
                            ContinuityKeyboard.Clear();
                        }
                        else
                        {
                            ContinuityKeyboard.Add(MyKeyboardHookStruct);
                        }
                    }

                    //过滤回车
                    if (MyKeyboardHookStruct.vkCode == 13)
                    {
                        return -1;
                    }
                }
                else
                {
                    ContinuityKeyboard.Clear();
                }
                PreKeyTime = nowtime;
            }

            //if event handled in application do not handoff to other listeners
            if (handled)
                return 1;
            else
                return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 启动鼠标监听钩子
        /// </summary>
        public void Start()
        {
            this.Start(true, false);
        }

        /// <summary>
        /// 启动监听钩子
        /// </summary>
        /// <param name="InstallMouseHook">是否启动鼠标监听钩子</param>
        /// <param name="InstallKeyboardHook">是否启动键盘监听钩子</param>
        public void Start(bool InstallMouseHook, bool InstallKeyboardHook)
        {
            IntPtr mInstance = GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName);

            // install Mouse hook only if it is not installed and must be installed
            if (hMouseHook == 0 && InstallMouseHook)
            {
                if (!DicMouseHookProc.ContainsKey(WH_MOUSE))
                {
                    MouseHookProcedure = new HookProc(MouseHookProc);
                    DicMouseHookProc.Add(WH_MOUSE, MouseHookProcedure);
                }
                else
                {
                    MouseHookProcedure = DicMouseHookProc[WH_MOUSE];
                }

                //这里只能使用AppDomain.GetCurrentThreadId()获取线程ID，这个方法返回的是系统线程ID；不能用Thread.CurrentThread.ManagedThreadId获取线程ID，这个返回的是托管线程ID
                hMouseHook = SetWindowsHookEx(
                    WH_MOUSE,
                    MouseHookProcedure,
                    IntPtr.Zero,
                    AppDomain.GetCurrentThreadId());

                //If SetWindowsHookEx fails.
                if (hMouseHook == 0)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    int errorCode = Marshal.GetLastWin32Error();
                    //do cleanup
                    Stop(true, false, false);
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }

            // install Keyboard hook only if it is not installed and must be installed
            if (InstallKeyboardHook && hKeyboardHook == 0)
            {
                if (!DivKeyboardHook.ContainsKey(WH_KEYBOARD_LL))
                {
                    KeyboardHookProcedure = new HookProc(KeyboardHookProc);
                    DivKeyboardHook.Add(WH_KEYBOARD_LL, KeyboardHookProcedure);
                }
                else
                {
                    KeyboardHookProcedure = DivKeyboardHook[WH_KEYBOARD_LL];
                }

                // Create an instance of HookProc.
                //KeyboardHookProcedure = new HookProc(KeyboardHookProc);
                //install hook
                hKeyboardHook = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    KeyboardHookProcedure,
                    mInstance,
                    0);
                //If SetWindowsHookEx fails.
                if (hKeyboardHook == 0)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    int errorCode = Marshal.GetLastWin32Error();
                    //do cleanup
                    Stop(false, true, false);
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }
        }

        /// <summary>
        /// 停止鼠标监听钩子
        /// </summary>
        public void Stop()
        {
            this.Stop(true, false, true);
        }

        /// <summary>
        /// 停止钩子
        /// </summary>
        /// <param name="UninstallMouseHook">是否停止鼠标监听钩子</param>
        /// <param name="UninstallKeyboardHook">是否停止键盘监听钩子</param>
        /// <param name="ThrowExceptions"></param>
        public void Stop(bool UninstallMouseHook, bool UninstallKeyboardHook, bool ThrowExceptions=false)
        {
            //if mouse hook set and must be uninstalled
            if (hMouseHook != 0 && UninstallMouseHook)
            {
                //uninstall hook
                int retMouse = UnhookWindowsHookEx(hMouseHook);
                DicMouseHookProc.Remove(WH_MOUSE);
                //reset invalid handle
                hMouseHook = 0;
                //if failed and exception must be thrown
                if (retMouse == 0 && ThrowExceptions)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    int errorCode = Marshal.GetLastWin32Error();
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }

            //if keyboard hook set and must be uninstalled
            if (hKeyboardHook != 0 && UninstallKeyboardHook)
            {
                //uninstall hook
                int retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                DivKeyboardHook.Remove(WH_KEYBOARD_LL);
                //reset invalid handle
                hKeyboardHook = 0;
                //if failed and exception must be thrown
                if (retKeyboard == 0 && ThrowExceptions)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    int errorCode = Marshal.GetLastWin32Error();
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }
        }

        #endregion

        /// <summary>
        /// 析构
        /// </summary>
        ~UserActivityHook()
        {
            //uninstall hooks and do not throw exceptions
            Stop(true, false, false);
        }
    }
}
