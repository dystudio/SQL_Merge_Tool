using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace 合并sql
{
    public partial class MergeForm : Form
    {
        string FileName = "";
        public MergeForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            comboBox.SelectedIndex = 0;
        }

        private void ChooseFile_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = dlg.SelectedPath;
                }
            }
        }

        private void MergeFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            //设置文件类型 
            sfd.Filter = "SQL文件（*.sql）|*.sql|文本文件（*.txt）|*.txt";

            //设置默认文件类型显示顺序 
            sfd.FilterIndex = 1;

            //保存对话框是否记忆上次打开的目录 
            sfd.RestoreDirectory = true;

            //设置默认的文件名
            sfd.FileName = DateTime.Now.ToString("yyyyMMdd");// in wpf is  sfd.FileName = "YourFileName";

            //点了保存按钮进入 

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string localFilePath = sfd.FileName.ToString(); //获得文件路径 
                string fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1); //获取文件名，不带路径
                FileName = localFilePath;
            }
            else {
                return;
            }
            //string FileName = @"D:\Update" + DateTime.Now.ToString("yyyyMM") + ".sql";
            Thread waitT = new Thread(new ThreadStart(Progress));
            if (textBox.Text == "")
            {
                MessageBox.Show("请选择文件后再合并！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            waitT.IsBackground = true;
            waitT.Start();
        }

        public void Progress()
        {
            int dsa = comboBox.SelectedIndex;
            try
            {
                var selectedFullPath = textBox.Text;
                var appendBeginTransactionAndCommit = false;


                List<string> allFiles = new List<string>();
                List<FileInfo> fileInfoList = new List<FileInfo>();
                GetAllFiles(selectedFullPath, allFiles);

                allFiles.ForEach(file =>
                {
                    FileInfo fileInfo = new FileInfo(file);
                    fileInfoList.Add(fileInfo);
                });

                FileInfo[] fileInfos = fileInfoList.ToArray();
                StringBuilder builder = new StringBuilder();

                if (this.chkBoxTransaction.Checked) {
                    appendBeginTransactionAndCommit = true;
                }

                //空文件夹提示
                if (fileInfos.Length == 0) {
                    MessageBox.Show("此文件夹内未找到SQL文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (appendBeginTransactionAndCommit)
                {
                    builder.Append("BEGIN TRANSACTION;");
                }

                for (int i = 0; i < fileInfos.Length; i++)
                {
                    builder.Append(String.Format("\r\n\r\n /*******************分割线*for*文件名:{0}*start**************/ \r\n\r\n", fileInfos[i].Name));
                    var fileFullPath = fileInfos[i].FullName;
                    StreamReader Strsw = new StreamReader(fileFullPath, comboBox.SelectedIndex == 0 ? Encoding.UTF8 : Encoding.GetEncoding(54936));
                    builder.Append(Strsw.ReadToEnd() + String.Format("\r\n\r\n /*******************分割线*for*文件名:{0}*end**************/ \r\n\r\n", fileInfos[i].Name));
                    Strsw.Close();
                }
                
                FileStream fs = null;
                if (!File.Exists(FileName))
                {
                    fs = new FileStream(FileName, FileMode.Create);
                }
                else
                {
                    fs = new FileStream(FileName, FileMode.Open);
                    MessageBox.Show("文件已存在，将会在原有基础上添加。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    StreamReader sr = new StreamReader(fs, comboBox.SelectedIndex == 0 ? Encoding.UTF8 : Encoding.GetEncoding(54936));
                    builder.Append(sr.ReadToEnd() + "\r\n\r\n /*******************分割线***************/ \r\n\r\n");

                }

                if (appendBeginTransactionAndCommit)
                {
                    builder.Append("COMMIT;");
                    builder.Append("--ROLLBACK;"); 
                }
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                var content = builder.ToString();
                sw.Write(content);
                sw.Close();
                MessageBox.Show("合并完毕，文件长度" + content.Length.ToString("n").Split('.')[0] + "KB大小", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GetAllFiles(string dir, List<string> list)
        {
            DirectoryInfo d = new DirectoryInfo(dir);
            FileInfo[] files = d.GetFiles("*.sql");//文件
            DirectoryInfo[] directs = d.GetDirectories();//文件夹
            foreach (FileInfo f in files)
            {
                list.Add(f.FullName);//添加文件名到列表中  
            }
            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                GetAllFiles(dd.FullName, list);
            }
        }
    }
}
