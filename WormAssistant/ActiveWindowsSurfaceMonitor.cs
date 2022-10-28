using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WormAssistant
{
    static class ActiveWindowsSurfaceMonitor
    {
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
        private const uint GW_HWNDNEXT = 2;
        private static WindowInfo[] activeWindows;
        private static Timer desktopWindowsChangedDelay;

        static ActiveWindowsSurfaceMonitor()
        {
            SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, IntPtr.Zero, DesktopWindowsChangedDelegate, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            desktopWindowsChangedDelay = new Timer { Interval = 80 };
            desktopWindowsChangedDelay.Tick += new EventHandler(DesktopWindowsChangedDelay_Tick);
        }

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        private static readonly WinEventDelegate DesktopWindowsChangedDelegate = OnDesktopWindowsChanged;
        public static event EventHandler DesktopWindowsChanged;

        private enum WindowState
        {
            Hidden = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        private class WindowInfo
        {
            public string Name { get; set; }
            public IntPtr Handle { get; set; }
            public Rectangle Bounds { get; set; }
            public List<Rectangle> VisibleSurfaces { get; set; }
            public WindowState State { get; set; }
            public IntPtr[] HookHandles { get; set; }
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "GetWindow")]
        private static extern IntPtr GetNextWindow(IntPtr hwnd, /*[MarshalAs(UnmanagedType.U4)]*/ uint wFlag);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private static void DesktopWindowsChangedDelay_Tick(object sender, EventArgs e)
        {
            desktopWindowsChangedDelay.Stop();
            DesktopWindowsChanged?.Invoke(null, null);
        }

        public static Rectangle[] GetActiveSurfaces()
        {
            UnHookWindows();
            activeWindows = HookUpActiveWindows();
            if (activeWindows != null)
            {
                return CalculateLandSurfaces(activeWindows).OrderBy(p => p.Y).ToArray();
            }

            return new Rectangle[0];
        }

        private static void UnHookWindows()
        {
            if (activeWindows != null && activeWindows.Length > 0)
            {
                foreach (var window in activeWindows)
                {
                    foreach (var hookHandle in window.HookHandles)
                    {
                        UnhookWinEvent(hookHandle);
                    }
                }
            }
        }

        private static Rectangle[] CalculateLandSurfaces(WindowInfo[] activeWindows)
        {
            var visibleWindowsRects = new List<Rectangle>();
            foreach (var visibleWindow in activeWindows.Where(vw => vw.State != WindowState.Minimized))
            {
                if (visibleWindow.State == WindowState.Maximized)
                {
                    break;
                }

                var currentWindowSurfaceLeftEdge = visibleWindow.Bounds.Left < Space.Surface.Left ? Space.Surface.Left : visibleWindow.Bounds.Left;
                var currentWindowtSurfaceWidth = (visibleWindow.Bounds.Right > Space.Surface.Right ? Space.Surface.Right : visibleWindow.Bounds.Right) - currentWindowSurfaceLeftEdge;
                visibleWindow.VisibleSurfaces.Add(new Rectangle(currentWindowSurfaceLeftEdge, visibleWindow.Bounds.Y, currentWindowtSurfaceWidth, 1));
                foreach (var windowRect in visibleWindowsRects)
                {
                    if (visibleWindow.VisibleSurfaces.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        for (int i = 0; i < visibleWindow.VisibleSurfaces.Count; i++)
                        {
                            var intersection = Rectangle.Intersect(windowRect, visibleWindow.VisibleSurfaces[i]);
                            if (!intersection.IsEmpty)
                            {
                                if (visibleWindow.VisibleSurfaces[i].Width > intersection.Width)
                                {
                                    if (visibleWindow.VisibleSurfaces[i].Left == intersection.Left)
                                    {
                                        visibleWindow.VisibleSurfaces[i] = new Rectangle(intersection.Right, visibleWindow.VisibleSurfaces[i].Y, visibleWindow.VisibleSurfaces[i].Width - intersection.Width, 1);
                                    }
                                    else if (intersection.Right == visibleWindow.VisibleSurfaces[i].Right)
                                    {
                                        visibleWindow.VisibleSurfaces[i] = new Rectangle(visibleWindow.VisibleSurfaces[i].X, visibleWindow.VisibleSurfaces[i].Y, visibleWindow.VisibleSurfaces[i].Width - intersection.Width, 1);
                                    }
                                    else if (visibleWindow.VisibleSurfaces[i].Left < intersection.Left)
                                    {
                                        visibleWindow.VisibleSurfaces.Add(new Rectangle(intersection.Right, visibleWindow.VisibleSurfaces[i].Y, visibleWindow.VisibleSurfaces[i].Right - intersection.Right, 1));
                                        visibleWindow.VisibleSurfaces[i] = new Rectangle(visibleWindow.VisibleSurfaces[i].X, visibleWindow.VisibleSurfaces[i].Y, intersection.X - visibleWindow.VisibleSurfaces[i].X, 1);
                                        i++;
                                    }
                                }
                                else
                                {
                                    visibleWindow.VisibleSurfaces.RemoveAt(i);
                                }
                            }
                        }
                    }
                }

                visibleWindowsRects.Add(visibleWindow.Bounds);
            }

            var landSurfaces = activeWindows.Where(aw => aw.State == WindowState.Normal).SelectMany(aw => aw.VisibleSurfaces).ToList();
            if (landSurfaces.Count == 0)
            {
                landSurfaces.Add(Space.Surface);
            }

            return landSurfaces.ToArray();
        }

        static WindowInfo[] HookUpActiveWindows()
        {
            var windowsToHook = new List<WindowInfo>();
            var windowHandle = FindWindow(null, "WormAssistant");
            windowHandle = GetNextWindow(windowHandle, GW_HWNDNEXT);
            while (windowHandle != IntPtr.Zero)
            {
                StringBuilder temp = new StringBuilder(256);
                GetWindowText(windowHandle, temp, 256);
                var name = temp.ToString();
                GetClassName(windowHandle, temp, 256);
                var classname = temp.ToString();
                if (string.Equals(classname, "Progman", StringComparison.Ordinal))
                {
                    break;
                }

                if (!string.IsNullOrEmpty(name) && IsWindowVisible(windowHandle) && !string.Equals(classname, "Windows.UI.Core.CoreWindow", StringComparison.Ordinal) &&
                    !string.Equals(classname, "ApplicationFrameWindow", StringComparison.Ordinal))
                {
                    var windowPlacement = GetWindowPlacement(windowHandle);
                    switch (windowPlacement.State)
                    {
                        case WindowState.Normal:
                            windowsToHook.Add(new WindowInfo
                            {
                                Name = name,
                                Bounds = new Rectangle(windowPlacement.NormalPosition.Left, windowPlacement.NormalPosition.Top, windowPlacement.NormalPosition.Right - windowPlacement.NormalPosition.Left, windowPlacement.NormalPosition.Bottom - windowPlacement.NormalPosition.Top),
                                VisibleSurfaces = new List<Rectangle>(),
                                Handle = windowHandle,
                                State = windowPlacement.State
                            });
                            break;
                        case WindowState.Minimized:
                            windowsToHook.Add(new WindowInfo
                            {
                                Name = name,
                                Handle = windowHandle,
                                State = windowPlacement.State
                            });
                            break;
                        case WindowState.Maximized:
                            windowsToHook.Add(new WindowInfo
                            {
                                Name = name,
                                Bounds = new Rectangle(0, 0, Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height),
                                Handle = windowHandle,
                                State = windowPlacement.State
                            });
                            break;
                    }
                }

                windowHandle = GetNextWindow(windowHandle, GW_HWNDNEXT);
            }

            for (int i = 0; i < windowsToHook.Count; i++)
            {
                uint process, thread;
                thread = GetWindowThreadProcessId(windowsToHook[i].Handle, out process);
                windowsToHook[i].HookHandles = new IntPtr[3];
                windowsToHook[i].HookHandles[0] = SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, DesktopWindowsChangedDelegate, process, thread, 0);
                windowsToHook[i].HookHandles[1] = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, DesktopWindowsChangedDelegate, process, thread, 0);
                windowsToHook[i].HookHandles[2] = SetWinEventHook(EVENT_OBJECT_DESTROY, EVENT_OBJECT_DESTROY, IntPtr.Zero, DesktopWindowsChangedDelegate, process, thread, 0);
            }

            return windowsToHook.ToArray();
        }

        private static WINDOWPLACEMENT GetWindowPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.Length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        static void OnDesktopWindowsChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (!desktopWindowsChangedDelay.Enabled && hwnd != IntPtr.Zero && activeWindows != null)
            {
                if (activeWindows.Any(p => p.Handle == hwnd) && idChild == 0 && idObject == 0)
                {
                    DesktopWindowsChanged?.Invoke(null, null);
                }
                else if (eventType == EVENT_OBJECT_CREATE)
                {
                    if ((idObject == -4 && idChild == 1 && IsWindowVisible(hwnd)) || (idObject == 0 && idChild == 0))
                    {
                        StringBuilder temp = new StringBuilder(256);
                        GetWindowText(hwnd, temp, 256);
                        var name = temp.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            RECT rect;
                            GetWindowRect(hwnd, out rect);
                            if (DesktopWindowsChanged != null && !rect.IsEmpty)
                            {
                                desktopWindowsChangedDelay.Start();
                            }
                        }
                    }
                }
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int Length;
            public int Flags;
            public WindowState State;
            public Point MinPosition;
            public Point MaxPosition;
            public RECT NormalPosition;
        }

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public bool IsEmpty => this.Left == 0 && this.Right == 0 && this.Top == 0 && this.Bottom == 0;
        }
    }
}