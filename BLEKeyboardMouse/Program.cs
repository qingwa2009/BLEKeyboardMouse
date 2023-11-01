using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.IO;

namespace BLEKeyboardMouse
{


    

    static class Program
    {
        const string CONFIG_FILE = "./config.json";
      
        public class Setting
        {
            public int version = 1;
            public string UARTPort = "com3";
            public int UARTBaudrate = 250000;
            public bool UARTEnable = true;
        }


        static JavaScriptSerializer json=new JavaScriptSerializer();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static  void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 f = new Form1();
            MyConsole.label = f.label1;

            MyBLEKeyboardMouse.MyBTInformation();

            Setting setting;
            if (File.Exists(CONFIG_FILE)) {
                setting = LoadJSONFile<Setting>(CONFIG_FILE);
            } else {
                setting = new Setting();
                SaveAsJSONFile(setting, CONFIG_FILE);
            }

            if (setting.UARTEnable) {
                MyBLEKeyboardMouse.EnableUART(setting.UARTPort, setting.UARTBaudrate);
            }
            MyBLEKeyboardMouse.BLERun();
            

            Application.Run(f);
            MyBLEKeyboardMouse.Stop();
        }

        /// <summary>从json文件创建对象</summary>
		public static T LoadJSONFile<T>(string path) {
            string s;
            using (FileStream fs = new FileStream(path, FileMode.Open)) {
                StreamReader rd = new StreamReader(fs, System.Text.Encoding.UTF8);
                s = rd.ReadToEnd();
            }
            return json.Deserialize<T>(s);
        }

        /// <summary>保存为json文件</summary>
        public static void SaveAsJSONFile(object obj, string path) {
            string s = json.Serialize(obj);
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write)) {
                StreamWriter wt = new StreamWriter(fs, System.Text.Encoding.UTF8);
                wt.Write(s);
                wt.Flush();
            }
        }
    }
}
