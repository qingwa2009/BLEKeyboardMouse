using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BLEKeyboardMouse
{
    static class MyWin32Api {
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        const int VK_NUMLOCK = 0x90;
        const int VK_CAPITAL = 0x14;
        const int VK_SCROLL = 0x91;
        const int VK_COMPOSE = 0xFF20;
        const int VK_KANA = 0x15;

        //获取键盘灯状态
        public static byte GetKeyboardLED() {
            return (byte)(((ushort)GetKeyState(VK_KANA)) << 4 | ((ushort)GetKeyState(VK_COMPOSE)) << 3 | ((ushort)GetKeyState(VK_SCROLL)) << 2 | ((ushort)GetKeyState(VK_CAPITAL)) << 1 | (ushort)GetKeyState(VK_NUMLOCK));
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        private const int KEYEVENTF_EXTENDEDKEY = 0x1;
        private const int KEYEVENTF_KEYUP = 0x2;

        public static bool isProgramInputNumlockKey=false;
        public static bool isProgramInputCapKey = false;
        public static bool isProgramInputScrollKey = false;
        public static void SetKeyboardLED(byte ledState) {
            

            byte n0 = (byte)GetKeyState(VK_NUMLOCK);
            byte c0 = (byte)GetKeyState(VK_CAPITAL);
            byte s0 = (byte)GetKeyState(VK_SCROLL);

            byte n1 = (byte)(ledState & 0x01);
            byte c1 = (byte)((ledState >> 1) & 0x01);
            byte s1 = (byte)((ledState >> 2) & 0x01);

            if (n0 != n1) {
                isProgramInputNumlockKey = true;
                MyConsole.WriteLine($"set numlock led {n1}");
                keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | 0, 0);
                keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }
            if (c0 != c1) {
                isProgramInputCapKey = true;
                MyConsole.WriteLine($"set capslock led {c1}");
                keybd_event(VK_CAPITAL, 0x3A, KEYEVENTF_EXTENDEDKEY | 0, 0);
                keybd_event(VK_CAPITAL, 0x3A, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }
            if (s0 != s1) {
                isProgramInputScrollKey = true;
                MyConsole.WriteLine($"set scroll led {s1}");
                keybd_event(VK_SCROLL, 0x46, KEYEVENTF_EXTENDEDKEY | 0, 0);
                keybd_event(VK_SCROLL, 0x46, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }

        }

        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
        public static extern int MapVirtualKey(int wCode, int wMapType);

        public const int MAPVK_VK_TO_VSC = 0;//virtual-key code->scan code
        public const int MAPVK_VSC_TO_VK = 1;
        public const int MAPVK_VK_TO_CHAR = 2;
        public const int MAPVK_VSC_TO_VK_EX = 3;
        public const int MAPVK_VK_TO_VSC_EX = 4;//Windows Vista and later

        //虚拟键码转扫描码
        public static int VirtualKeyCode2ScanKeyCode(int vk) {
            return MapVirtualKey(vk, MAPVK_VK_TO_VSC_EX);
        }




        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetKeyboardState(byte[] lpKeyState);
        public static byte[] ScaneKeyboard() {
            byte[] ks = new byte[256];

            byte[] bs = new byte[8];

            if (MyWin32Api.GetKeyboardState(ks)) {
                if((ks[(int)VirtualKeyCode.VK_LCONTROL] & 0x80) != 0) {
                    bs[0] |= 1<<0;
                }
                if ((ks[(int)VirtualKeyCode.VK_LSHIFT] & 0x80) != 0) {
                    bs[0] |= 1<<1;
                }
                if ((ks[(int)VirtualKeyCode.VK_LMENU] & 0x80) != 0) {
                    bs[0] |= 1 << 2;
                }
                if ((ks[(int)VirtualKeyCode.VK_LWIN] & 0x80) != 0) {
                    bs[0] |= 1 << 3;
                }
                if ((ks[(int)VirtualKeyCode.VK_RCONTROL] & 0x80) != 0) {
                    bs[0] |= 1 << 4;
                }
                if ((ks[(int)VirtualKeyCode.VK_RSHIFT] & 0x80) != 0) {
                    bs[0] |= 1 << 5;
                }
                if ((ks[(int)VirtualKeyCode.VK_RMENU] & 0x80) != 0) {
                    bs[0] |= 1 << 6;
                }
                if ((ks[(int)VirtualKeyCode.VK_RWIN] & 0x80) != 0) {
                    bs[0] |= 1 << 7;
                }

                int j = 2;
                for (int i = 0; i < ks.Length; i++) {
                    byte k = ks[i];
                    if ((k & 0x80) != 0) {
                        if (VK2USBKeyCode[i] != 0) {
                            bs[j] = (byte)VK2USBKeyCode[i];
                            j++;
                            if (j >= bs.Length) break;
                            //Console.WriteLine($"{Enum.GetName(typeof(VirtualKeyCode), i)} {ks[i]}");
                        }
                    }
                }

            }
            

            return bs;
        }
        public static readonly int[] VK2USBKeyCode=new int[255];
        static MyWin32Api() {
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LBUTTON] = 0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_RBUTTON ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_CANCEL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MBUTTON ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_XBUTTON1 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_XBUTTON2 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BACK ]=42;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_TAB ]=43;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_CLEAR ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_RETURN ]=40;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SHIFT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_CONTROL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MENU ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_PAUSE ]=72;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_CAPITAL ]=57;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_KANA ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_HANGUEL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_HANGUL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_IME_ON ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_JUNJA ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_FINAL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_HANJA ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_KANJI ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_IME_OFF ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_ESCAPE ]=41;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_CONVERT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NONCONVERT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_ACCEPT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MODECHANGE ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SPACE ]=44;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_PRIOR ]=75;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NEXT ]=78;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_END ]=77;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_HOME ]=74;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LEFT ]=80;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_UP ]=82;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_RIGHT ]=79;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_DOWN ]=81;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SELECT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_PRINT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_EXECUTE ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SNAPSHOT ]=70;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_INSERT ]=73;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_DELETE ]=76;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_HELP ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_0 ]=39;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_1 ]=30;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_2 ]=31;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_3 ]=32;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_4 ]=33;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_5 ]=34;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_6 ]=35;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_7 ]=36;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_8 ]=37;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_9 ]=38;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_A ]=4;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_B ]=5;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_C ]=6;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_D ]=7;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_E ]=8;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F ]=9;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_G ]=10;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_H ]=11;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_I ]=12;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_J ]=13;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_K ]=14;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_L ]=15;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_M ]=16;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_N ]=17;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_O ]=18;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_P ]=19;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_Q ]=20;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_R ]=21;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_S ]=22;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_T ]=23;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_U ]=24;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_V ]=25;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_W ]=26;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_X ]=27;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_Y ]=28;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_Z ]=29;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LWIN ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_RWIN ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_APPS ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SLEEP ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD0 ]=98;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD1 ]=89;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD2 ]=90;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD3 ]=91;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD4 ]=92;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD5 ]=93;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD6 ]=94;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD7 ]=95;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD8 ]=96;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMPAD9 ]=97;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MULTIPLY ]=85;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_ADD ]=87;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SEPARATOR ]=88;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SUBTRACT ]=86;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_DECIMAL ]=99;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_DIVIDE ]=84;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F1 ]=58;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F2 ]=59;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F3 ]=60;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F4 ]=61;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F5 ]=62;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F6 ]=63;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F7 ]=64;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F8 ]=65;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F9 ]=66;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F10 ]=67;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F11 ]=68;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F12 ]=69;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F13 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F14 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F15 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F16 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F17 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F18 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F19 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F20 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F21 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F22 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F23 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_F24 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMLOCK ]=83;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_SCROLL ]=71;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LSHIFT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_RSHIFT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LCONTROL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_RCONTROL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LMENU ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_RMENU ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BROWSER_BACK ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BROWSER_FORWARD ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BROWSER_REFRESH ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BROWSER_STOP ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BROWSER_SEARCH ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BROWSER_FAVORITES ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_BROWSER_HOME ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_VOLUME_MUTE ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_VOLUME_DOWN ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_VOLUME_UP ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MEDIA_NEXT_TRACK ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MEDIA_PREV_TRACK ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MEDIA_STOP ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_MEDIA_PLAY_PAUSE ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LAUNCH_MAIL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LAUNCH_MEDIA_SELECT ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LAUNCH_APP1 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_LAUNCH_APP2 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_1 ]=51;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_PLUS ]=46;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_COMMA ]=54;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_MINUS ]=45;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_PERIOD ]=55;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_2 ]=56;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_3 ]=53;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_4 ]=47;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_5 ]=49;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_6 ]=48;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_7 ]=52;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_8 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_102 ]=100;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_PROCESSKEY ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_PACKET ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_ATTN ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_CRSEL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_EXSEL ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_EREOF ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_PLAY ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_ZOOM ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_NONAME ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_PA1 ]=0;
            VK2USBKeyCode[(int)VirtualKeyCode.VK_OEM_CLEAR ]=0;

        }

    }
}
