﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace PSash
{
    /// <remarks>
    /// Ideas from:
    /// http://www.pinvoke.net/default.aspx/user32/registerhotkey.html
    /// http://outcoldman.ru/en/blog/show/240
    /// </remarks>
    internal static class HotKeyWinApi
    {
        #region fields
        private static uint TILDE = 0xC0;
        private static uint MOD_WIN = 0x8;

        /// <summary>
        /// Posted when the user presses a hot key registered by the RegisterHotKey function. 
        /// The message is placed at the top of the message queue associated with the thread 
        /// that registered the hot key.
        /// </summary>
        internal static int WM_HOTKEY = 0x312;
        #endregion

        /// <summary> The RegisterHotKey function defines a system-wide hot key </summary>
        /// <param name="hwnd">Handle to the window that will receive WM_HOTKEY messages  generated by the hot key.</param>
        /// <param name="id">Specifies the identifier of the hot key.</param>
        /// <param name="fsModifiers">Specifies keys that must be pressed in combination with the key  specified by the 'vk' parameter in order to generate the WM_HOTKEY message.</param>
        /// <param name="vk">Specifies the virtual-key code of the hot key</param>
        /// <returns><c>true</c> if the function succeeds, otherwise <c>false</c></returns>
        /// <seealso cref="http://msdn.microsoft.com/en-us/library/ms646309(VS.85).aspx"/>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        //TODO: allow for other hotkey
        internal static void RegisterKeysFor(Window window)
        {
            Contract.Requires(window != null);
            int id = window.GetHashCode() + TILDE.GetHashCode() + MOD_WIN.GetHashCode();
            var hWnd = new WindowInteropHelper(window).Handle;
            window.Closing += (_, e) => UnregisterHotKey(hWnd, id);
            RegisterHotKey(hWnd, id, MOD_WIN, TILDE);
        }
    }
}
