using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BLEKeyboardMouse
{
    public partial class Form1 : Form
    {
        public Form1() {
            InitializeComponent();
            SetCursorInMid();
        }
        
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;
        private const int WM_MOUSEMOVE = 0x200;


        protected override void WndProc(ref Message m) {
            
            switch (m.Msg) {
                case WM_KEYDOWN:
                case WM_KEYUP:
                case WM_SYSKEYDOWN:
                case WM_SYSKEYUP:
                    //Console.WriteLine($"{m.Msg} {m.WParam}");
                    
                    byte[] bs;
                    int key = m.WParam.ToInt32();
                    //没法监听到按下截屏键，但是可以监听到释放，所以监听到释放的时候，单独触发一次截屏按下
                    if (key == (int)VirtualKeyCode.VK_SNAPSHOT) {
                        bs = MyWin32Api.ScaneKeyboard();
                        for (int i = 2; i < bs.Length; i++) {
                            if (bs[i] == 0) { 
                                bs[i] =(byte)MyWin32Api.VK2USBKeyCode[(int)VirtualKeyCode.VK_SNAPSHOT];
                                break;
                            }
                        }
                        MyBLEKeyboardMouse.SendKeysIfChange(bs);
                    }
                    
                    bs = MyWin32Api.ScaneKeyboard();
                    //过滤掉程序输入的改变led的按键事件
                    if (MyWin32Api.isProgramInputNumlockKey) {
                        //MyConsole.WriteLine("this event is set by program!");
                        byte n = (byte)MyWin32Api.VK2USBKeyCode[(int)VirtualKeyCode.VK_NUMLOCK];
                        for (int i = 0; i < bs.Length; i++) {
                            if (bs[i] == n) {
                                bs[i] = 0;
                                break;
                            }
                        }
                        MyWin32Api.isProgramInputNumlockKey = false;
                    }
                    if (MyWin32Api.isProgramInputCapKey) {
                        byte n = (byte)MyWin32Api.VK2USBKeyCode[(int)VirtualKeyCode.VK_CAPITAL];
                        for (int i = 0; i < bs.Length; i++) {
                            if (bs[i] == n) {
                                bs[i] = 0;
                                break;
                            }
                        }
                        MyWin32Api.isProgramInputCapKey = false;
                    }
                    if (MyWin32Api.isProgramInputScrollKey) {
                        byte n = (byte)MyWin32Api.VK2USBKeyCode[(int)VirtualKeyCode.VK_SCROLL];
                        for (int i = 0; i < bs.Length; i++) {
                            if (bs[i] == n) {
                                bs[i] = 0;
                                break;
                            }
                        }
                        MyWin32Api.isProgramInputScrollKey = false;
                    }

                    MyBLEKeyboardMouse.SendKeysIfChange(bs);
                    base.WndProc(ref m);
                    break;

               
                default:
                    
                    //Console.WriteLine($"{m.Msg} {m.WParam}");
                    base.WndProc(ref m);
                    break;
            }
            

        }

        protected override void OnLostFocus(EventArgs e) {
            byte[] bs=new byte[8];
            MyBLEKeyboardMouse.SendKeysIfChange(bs);
            MyBLEKeyboardMouse.SendMouse(0, 0, 0, 0, 0, 0);
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            //base.OnKeyDown(e);
            Console.WriteLine($"{e.KeyValue} down");
            //byte[] bs = MyWin32Api.ScaneKeyboard();
            //MyBLEKeyboardMouse.SendKeysIfChange(bs);
            //e.Handled = true;
            //e.SuppressKeyPress = true;
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            //base.OnKeyUp(e);
            //Console.WriteLine($"{e.KeyCode} up");
            //byte[] bs = MyWin32Api.ScaneKeyboard();
            //MyBLEKeyboardMouse.SendKeysIfChange(bs);
            //e.Handled = true;
            //e.SuppressKeyPress = true;
        }



        private void Form1_KeyDown(object sender, KeyEventArgs e) {

            //byte[] bs = MyWin32Api.ScaneKeyboard();
            //MyBLEKeyboardMouse.SendKeysIfChange(bs);
            //e.Handled = true;
            //e.SuppressKeyPress = true;
            
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e) {
            
            //byte[] bs = MyWin32Api.ScaneKeyboard();
            //MyBLEKeyboardMouse.SendKeysIfChange(bs);
            //e.Handled = true;
            //e.SuppressKeyPress = true;

        }

        bool lockMouse = false;
        private void label1_Click(object sender, EventArgs e) {
            lockMouse = !lockMouse;
            if (lockMouse) {
                //Cursor.Clip = new Rectangle(Location, Size);
                
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e) {
            
        }

        bool isProgramMoveMouse = false;
        private void SetCursorInMid() {
            var p = Location;
            p.X += Size.Width / 2;
            p.Y += Size.Height / 2;
            Cursor.Position = p;
            isProgramMoveMouse = true;
        }

        int px, py;
        int wheel;
        private void label1_MouseMove(object sender, MouseEventArgs e) {



            //if (isProgramMoveMouse) {
            //    isProgramMoveMouse = false;
            //    return;
            //}

            var p1 = Cursor.Position;

            SetCursorInMid();
            int dx = p1.X - Cursor.Position.X;
            int dy = p1.Y - Cursor.Position.Y;

            //Console.WriteLine($"{dx} {dy} {wheel}");

            byte left = (byte)(e.Button.HasFlag(MouseButtons.Left) ? 1 : 0);
            byte right = (byte)(e.Button.HasFlag(MouseButtons.Right) ? 1 : 0);
            byte mid = (byte)(e.Button.HasFlag(MouseButtons.Middle) ? 1 : 0);
            MyBLEKeyboardMouse.SendMouse(left, right, mid, dx/2, dy/2, wheel);
            wheel = 0;

        }
        
        protected override void OnMouseWheel(MouseEventArgs e) {
            //Console.WriteLine($"{e.Delta}");
            base.OnMouseWheel(e);
            wheel = e.Delta;
        }
    }
}
