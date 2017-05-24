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

        /// <summary>
        /// 以json的格式输出到txt文件中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 显示ifc信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            StringBuilder sb = new StringBuilder();
            Dictionary<string, IFCItem> ifc = parser.getIFCDict();
            Dictionary<string, string> attributes;
            sb.Append("{\n");
            int instanceCount = 0;
            if (ifc != null)
            {
                foreach (string guid in ifc.Keys)
                {
                    sb.Append(" \"" + guid + "\": {\n");
                    IFCItem instance = ifc[guid];
                    attributes = instance.getAttributes();
                    int attributesCount = 0;
                    foreach (string field in attributes.Keys)
                    {
                        string value = attributes[field];
                        if (value != null)
                        {
                            value = value.Trim().Replace('\\', '/').Replace('\n', ' ');
                        }

                        sb.Append("     \"" + field + "\":  \"" + value + "\"");
                        if (attributesCount < attributes.Count - 1)
                        {
                            sb.Append(",");
                        }
                        sb.Append("\n");
                        attributesCount++;
                    }
                    sb.Append("     }");
                    if (instanceCount < ifc.Count - 1)
                    {
                        sb.Append(",");
                    }
                    sb.Append("\n");
                    instanceCount++;
                }
            }
            sb.Append(" }");
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "文本文档|*.txt|json文件|*.json";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                StreamWriter sw = new StreamWriter(dialog.FileName);
                sw.Write(sb.ToString());
                sw.Close();
                MessageBox.Show("输入完成");
            }  
        }

    }
}
