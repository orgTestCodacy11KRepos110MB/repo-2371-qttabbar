﻿//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Runtime.InteropServices;

namespace QTTabBarLib.Interop {
    [StructLayout(LayoutKind.Sequential)]
    public struct TBBUTTON {
        public int iBitmap;
        public int idCommand;
        [StructLayout(LayoutKind.Explicit)]
        private struct TBBUTTON_U {
            [FieldOffset(0)]
            public byte fsState;
            [FieldOffset(1)]
            public byte fsStyle;
            [FieldOffset(0)]
            private IntPtr bReserved;
        }
        private TBBUTTON_U union;
        public byte fsState { get { return union.fsState; } set { union.fsState = value; } }
        public byte fsStyle { get { return union.fsStyle; } set { union.fsStyle = value; } }
        public UIntPtr dwData;
        public IntPtr iString;
    }
}
