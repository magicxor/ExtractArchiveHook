﻿using System;
using System.Runtime.InteropServices;

namespace ExtractArchiveHook
{
    public static class WindowsApi
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    }
}
