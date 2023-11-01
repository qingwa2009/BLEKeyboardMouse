using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLEKeyboardMouse
{
    class MyConsole {
        public static System.Windows.Forms.Label label;
        static Action<string> act = SetLabel;
        static string[] ss = new string[30];
        static int ip = 0;
        public static void WriteLine(string s) {
            s += "\r\n";
            if (label.InvokeRequired) {
                label.Invoke(act,s);
            } else {
                SetLabel(s);
            }
            Console.Write(s);
            
        }
        public static void WriteLine(string s, params object[] args) {
            WriteLine(String.Format(s, args));
        }

        private static void SetLabel(string s) {
            ss[ip] = s;
            ip++;
            if (ip >= ss.Length) ip = 0;
            

            string[] s1 = new string[ss.Length - ip];
            Array.Copy(ss, ip, s1, 0, s1.Length);
            s = string.Join("", s1);

            
            if (ip != 0) {
                string[] s0 = new string[ip];
                Array.Copy(ss, s0, s0.Length);
                s += string.Join("", s0);
            }


            label.Text = s;
        }
        
    }
}
