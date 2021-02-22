using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SkinSoft.OSSkin;
using Microsoft.Win32;
using System.Threading;
using System.Management;
using System.Net;

namespace KazyCrypter
{
    public partial class Login : Form
    {
        OSSkin skin;
        private string status;
        private string Status
        {
            get { return status; }
            set { status = value; toolStripLabel1.Text = status; }
        }

        string hwid;
        string host = "http://deakgegi.uw.hu/kazycrypter/";
        string host2 = "http://deakgegi.no-ip.org/kazycrypter/";

        public Login()
        {
            InitializeComponent();
            //Text += Program.Version;
            CheckForIllegalCrossThreadCalls = false;
            skin = new OSSkin();
            LoadSettings();
        }

        private void CheckUpdate()
        {
            Status = "Checking updates...";
            WebClient c = new WebClient();
            try
            {
                string newver = c.DownloadString(host + "version.txt");
             //   if (newver == Program.Version)
                {
                    Status = "You are up to date";
                    while (label3.Text == "Getting HWID...")
                        Thread.Sleep(10); 
                    button1.Enabled = true;
                    if (checkBox2.Checked)
                        button1_Click(null, null);
                }
              //  else
                {
                 //   MessageBox.Show("New update available!\nYour version: " + Program.Version + "\nNew version: " + newver + "\nPlease choose save location!", "New Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    saveFileDialog1.FileName += " " + newver;
                    System.Windows.Forms.DialogResult res = System.Windows.Forms.DialogResult.None;
                    Invoke(new MethodInvoker(delegate() { res = saveFileDialog1.ShowDialog(); }));
                    if (res == System.Windows.Forms.DialogResult.OK)
                    {
                        Status = "Downloading update, please wait!";
                        c.DownloadFile(host + "KazyCrypter" + newver + ".exe", saveFileDialog1.FileName);
                        MessageBox.Show("New update has been downloaded to:\n" + saveFileDialog1.FileName, "Update done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Environment.Exit(0);
                    }
                }
            }
            catch
            {
                host = host2;
                new Thread(CheckUpdate).Start();
            }
        }

        private void SaveSetting()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey("KazyCrypter");
            key.SetValue("RememberMe", checkBox1.Checked.ToString());
            key.SetValue("Username", textBox1.Text);
            key.SetValue("Password", textBox2.Text);
            key.SetValue("AutoLogin", (checkBox2.Checked && checkBox1.Checked).ToString());
            key.Close();
        }

        private void LoadSettings()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("KazyCrypter");
            SkinStyle style = SkinStyle.Office2007Black;
            if (key != null)
            {
                try
                {
                    string name = key.GetValue("Skin").ToString();
                    if (name == "None")
                    {
                        skin.Shutdown();
                    }
                    else
                    {
                        foreach (SkinStyle s in Enum.GetValues(typeof(SkinStyle)))
                            if (s.ToString() == name)
                                style = s;
                        skin.Shutdown();
                        skin.Style = style;
                    }
                    checkBox1.Checked = bool.Parse(key.GetValue("RememberMe").ToString());
                    if (checkBox1.Checked)
                    {
                        textBox1.Text = key.GetValue("Username").ToString();
                        textBox2.Text = key.GetValue("Password").ToString();
                        checkBox2.Checked = bool.Parse(key.GetValue("AutoLogin").ToString());
                    }
                }
                catch { }
                key.Close();
            }
            else
            {
                skin.Shutdown();
                skin.Style = style;
            }
        }

        private void LogMeIn()
        {
            button1.Enabled = false;
            Status = "Logging in...";
            WebClient c = new WebClient();
            string data = c.DownloadString(string.Format(host + "login.php?action={0}&username={1}&password={2}&hwid={3}", "login", textBox1.Text, textBox2.Text, hwid));
            if (!string.IsNullOrEmpty(data))
            {
                string[] datas = data.Split('|');
                if (int.Parse(datas[1]) < 0)
                    MessageBox.Show("Your account has expired!\nValid until: " + datas[0], "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    SaveSetting();
          //          Invoke(new MethodInvoker(delegate() { new Main(skin, textBox1.Text, datas[2], datas[0], c.DownloadString(host + "news.txt"), int.Parse(datas[1])).Show(); }));
                    Hide();
                }
            }
            else
                MessageBox.Show("You are not registered!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            button1.Enabled = true;
            Status = "You are up to date";
        }

        static string UniqueID()
        {
            ManagementObject dsk = new ManagementObject(@"win32_logicaldisk.deviceid=""C:""");
            dsk.Get();
            string volumeSerial = dsk["VolumeSerialNumber"].ToString();
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                if (cpuInfo == "")
                {
                    cpuInfo = mo.Properties["processorID"].Value.ToString();
                    break;
                }
            }
            return cpuInfo.Substring(0, 4) + "-" + volumeSerial.Substring(0, 4) + "-" + cpuInfo.Substring(4, 4) + "-" + volumeSerial.Substring(4, 4);
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSetting();
            Environment.Exit(0);
        }

        private void Login_Shown(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                hwid = UniqueID();
                label3.Text = "HWID: " + hwid;
            }).Start();
            new Thread(CheckUpdate).Start();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox2.Enabled = checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Thread(LogMeIn).Start();
        }
    }
}
