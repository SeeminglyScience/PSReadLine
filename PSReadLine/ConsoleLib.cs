﻿/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Text;

namespace Microsoft.PowerShell.Internal
{
    internal class VirtualTerminal : IConsole
    {
        public int CursorLeft
        {
            get => Console.CursorLeft;
            set => Console.CursorLeft = value;
        }

        public int CursorTop
        {
            get => Console.CursorTop;
            set => Console.CursorTop = value;
        }

        // .NET doesn't implement this API, so we fake it with a commonly supported escape sequence.
        protected int _unixCursorSize = 25;
        public virtual int CursorSize
        {
            get => PlatformWindows.IsConsoleApiAvailable(input: false, output: true) ? Console.CursorSize : _unixCursorSize;
            set
            {
                if (PlatformWindows.IsConsoleApiAvailable(input: false, output: true))
                {
                    Console.CursorSize = value;
                }
                else
                {
                    _unixCursorSize = value;
                    // Solid blinking block or blinking vertical bar
                    Write(value > 50 ? "\x1b[2 q" : "\x1b[5 q");
                }
            }
        }

        public bool CursorVisible
        {
            get => Console.CursorVisible;
            set => Console.CursorVisible = value;
        }

        public int BufferWidth
        {
            get => Console.BufferWidth;
            set => Console.BufferWidth = value;
        }

        public int BufferHeight
        {
            get => Console.BufferHeight;
            set => Console.BufferHeight = value;
        }

        public int WindowWidth
        {
            get => Console.WindowWidth;
            set => Console.WindowWidth = value;
        }

        public int WindowHeight
        {
            get => Console.WindowHeight;
            set => Console.WindowHeight = value;
        }

        public int WindowTop
        {
            get => Console.WindowTop;
            set => Console.WindowTop = value;
        }

        public ConsoleColor BackgroundColor
        {
            get => Console.BackgroundColor;
            set => Console.BackgroundColor = value;
        }

        public ConsoleColor ForegroundColor
        {
            get => Console.ForegroundColor;
            set => Console.ForegroundColor = value;
        }

        public Encoding OutputEncoding
        {
            // Catch and ignore exceptions - if we can't change the encoding, output is
            // probably redirected, but it can also happen with the legacy console on Windows.
            get { try { return Console.OutputEncoding; } catch { return Encoding.Default; } }
            set { try { Console.OutputEncoding = value; } catch { } }
        }

        private void WaitForKeyAvailable()
        {
            // On Unix platforms input echo is on by default. If we wait for KeyAvailable without
            // disabling it, all input will be echoed.
            UnixConsoleEcho.InputEcho.Disable();
            try
            {
                while (!Console.KeyAvailable)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            finally
            {
                UnixConsoleEcho.InputEcho.Enable();
            }
        }

        public ConsoleKeyInfo ReadKey()
        {
            // Wait for a key to be pressed before calling ReadKey to avoid locking stdin on
            // non-Windows platforms.
            WaitForKeyAvailable();
            return Console.ReadKey(true);
        }

        public bool KeyAvailable                         => Console.KeyAvailable;
        public void SetWindowPosition(int left, int top) => Console.SetWindowPosition(left, top);
        public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);
        public virtual void Write(string value)          => Console.Write(value);
        public virtual void WriteLine(string value)      => Console.WriteLine(value);
        public virtual void ScrollBuffer(int lines)      => Console.Write("\x1b[" + lines + "S");
        public virtual void BlankRestOfLine()            => Console.Write("\x1b[K");

        private int _savedX, _savedY;

        public void SaveCursor()
        {
            _savedX = Console.CursorLeft;
            _savedY = Console.CursorTop;
        }

        public void RestoreCursor() => Console.SetCursorPosition(_savedX, _savedY);
    }
}
