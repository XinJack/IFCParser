using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace IFCParser
{
    public partial class Form1 : Form
    {
        IFCParser parser;
        public Form1()
        {
            InitializeComponent();
            parser = new IFCParser(ifcTree);
        }

        private void 打开文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            parser.resetDict();
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "IFC文件|*.ifc";
            dialog.Title = "打开";

            if (dialog.ShowDialog() == DialogResult.OK) {
                parser.parseIfcFile(dialog.FileName);
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //paser = new IFCParser(ifcTree);
        }

        private void 显示ifc信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            StringBuilder sb = new StringBuilder();
            Dictionary<string, IFCItem> ifc = parser.getIFCDict();
            Dictionary<string, string> attributes;
            if (ifc != null)
            {
                foreach (string key in ifc.Keys)
                {
                    sb.Append(key).Append('\n').Append("{");
                    IFCItem item = ifc[key];
                    attributes = item.getAttributes();
                    foreach (string key1 in attributes.Keys)
                    {
                        sb.Append("  " + key1 + ":" + attributes[key1] + ";").Append('\n');
                    }
                    sb.Append("}").Append('\n');
                }

            }
            //MessageBox.Show(sb.ToString());
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "文本文档|*.txt";
            if (dialog.ShowDialog() == DialogResult.OK) {
                StreamWriter sw = new StreamWriter(dialog.FileName);
                sw.Write(sb.ToString());
                sw.Close();
                MessageBox.Show("输入完成");
            }  
        }

        private void toolStripDropDownButton2_Click(object sender, EventArgs e)
        {

        }
    }
}
