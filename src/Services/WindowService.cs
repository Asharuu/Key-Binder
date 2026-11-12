using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SafeKeyBinder.Native;

namespace SafeKeyBinder.Services
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string ProcessName { get; set; }
        public string Title { get; set; }

        public string DisplayLabel
        {
            get
            {
                if (string.IsNullOrEmpty(ProcessName))
                {
                    return Title;
                }
                return string.Format("[{0}] {1}", ProcessName, Title);
            }
        }
    }

    public class WindowService
    {
        public WindowInfo GetForegroundWindowInfo()
        {
            IntPtr hwnd = Win32Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return new WindowInfo { Handle = IntPtr.Zero, ProcessName = string.Empty, Title = string.Empty };
            }

            return ReadWindowInfo(hwnd);
        }

        public List<WindowInfo> GetOpenWindows(int excludeProcessId)
        {
            List<WindowInfo> results = new List<WindowInfo>();
            HashSet<string> seenLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Win32Native.EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (!Win32Native.IsWindowVisible(hwnd))
                {
                    return true;
                }

                int length = Win32Native.GetWindowTextLength(hwnd);
                if (length <= 0)
                {
                    return true;
                }

                uint pid = 0;
                Win32Native.GetWindowThreadProcessId(hwnd, out pid);
                if (excludeProcessId > 0 && (int)pid == excludeProcessId)
                {
                    return true;
                }

                WindowInfo info = ReadWindowInfo(hwnd);
                if (!string.IsNullOrEmpty(info.Title) && !seenLabels.Contains(info.DisplayLabel))
                {
                    seenLabels.Add(info.DisplayLabel);
                    results.Add(info);
                }

                return true;
            }, IntPtr.Zero);

            results.Sort(delegate(WindowInfo a, WindowInfo b)
            {
                return string.Compare(a.DisplayLabel, b.DisplayLabel, StringComparison.OrdinalIgnoreCase);
            });

            return results;
        }

        private WindowInfo ReadWindowInfo(IntPtr hwnd)
        {
            int len = Win32Native.GetWindowTextLength(hwnd);
            string title = string.Empty;
            if (len > 0)
            {
                StringBuilder sb = new StringBuilder(len + 1);
                Win32Native.GetWindowText(hwnd, sb, sb.Capacity);
                title = sb.ToString();
            }

            string processName = string.Empty;
            uint pid = 0;
            Win32Native.GetWindowThreadProcessId(hwnd, out pid);
            if (pid > 0)
            {
                try
                {
                    using (Process proc = Process.GetProcessById((int)pid))
                    {
                        processName = proc.ProcessName;
                    }
                }
                catch
                {
                    processName = string.Empty;
                }
            }

            return new WindowInfo
            {
                Handle = hwnd,
                ProcessName = processName ?? string.Empty,
                Title = title ?? string.Empty
            };
        }
    }
}
