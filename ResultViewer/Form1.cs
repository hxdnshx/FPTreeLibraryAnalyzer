using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryDatabase;
using System.Threading;
using System.Text.RegularExpressions;

namespace ResultViewer
{
    public partial class Form1 : Form
    {
        public SystemContext data;
        public List<ItemSet> FreqSet;
        public List<BookData> books;
        public Form1()
        {
            InitializeComponent();
            
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }
            string file = openFileDialog1.FileName;
            LoadFile(file);
        }

        private void LoadFile(string file)
        {
            LabelFileName.Text = file;
            data = new SystemContext("Data Source=\"" + file + "\"");
            var bookdata = from c in data.BookData
                           orderby c.KeyStr
                           select c;
            books = new List<BookData>();
            foreach (var ele in bookdata)
            {
                books.Add(ele);
            }
            var solutions = from c in data.MineResult
                            orderby c.ResultID
                            select c;
            FreqSet = new List<ItemSet>();
            int prevID = -1;
            ItemSet iset = null;
            foreach (var val in solutions)
            {
                if (iset == null || val.ResultID != prevID)
                {
                    if (iset != null)
                        FreqSet.Add(iset);
                    iset = new ItemSet()
                    {
                        support = val.support
                    };
                    prevID = val.ResultID;
                }
                if (!iset.KeyStrs.Contains(val.KeyStr))
                    iset.KeyStrs.Add(val.KeyStr);
            }
            if (iset != null)
                FreqSet.Add(iset);
            FreqSet.Sort((ItemSet a, ItemSet b) => -a.support.CompareTo(b.support));
            listBox1.Items.Clear();
            foreach (var ele in FreqSet)
            {
                listBox1.Items.Add(ele);
            }
            listBox1.SelectedIndex = 0;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            var query = from c in ((ItemSet)listBox1.SelectedItem).KeyStrs
                        select c;
            foreach (var ele in query)
                listBox2.Items.Add(ele);
            listBox2.SelectedIndex = 0;
        }

        object prevsel2=null;
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == prevsel2)
                return;
            prevsel2 = listBox2.SelectedItem;
            listBox3.Items.Clear();
            var query = from c in books
                        where c.KeyStr == listBox2.SelectedItem.ToString()
                        select c;
            foreach (var ele in query)
                listBox3.Items.Add(ele);
        }

        private void 开始分析ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = dialogXls.ShowDialog();
            if (result != DialogResult.OK)
                return;
            result = dialogSaveDb.ShowDialog();
            if (result != DialogResult.OK)
                return;
            string xls = dialogXls.FileName;
            string db = dialogSaveDb.FileName;
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            if(books!=null)
                books.Clear();
            if(FreqSet!=null)
                FreqSet.Clear();
            data = null;
            string regex = txtRegex.Text;
            string expr = txtCategory.SelectedItem.ToString();
            string sup = txtSupport.Text;
            Thread thr = new Thread(() =>
              {
                  status = "读入Excel中";
                  DatabaseTool.ReadFromExcel(xls, db, regex,expr );
                  status= "分析数据中";
                  DatabaseTool.Analyze(db, sup);
                  status = "End";
                  this.Invoke(Delegate.CreateDelegate(typeof(Action<string>),this,"LoadFile"), new object[] { db });
              });
            thr.Start();
            timer1.Enabled = true;
        }

        private string status;


        private void Form1_Load(object sender, EventArgs e)
        {
            Type t = typeof(Record);
            foreach(var ele in t.GetProperties())
            {
                txtCategory.Items.Add(ele.Name);
            }
            txtCategory.Text = "Category";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (status == "End")
                timer1.Enabled = false;
            else
            {
                LabelFileName.Text = status;
                status += ".";
            }
        }

        private void FilterReset1_Click(object sender, EventArgs e)
        {
            Filter1.Text = "";
        }

        private void listBox3_DoubleClick(object sender, EventArgs e)
        {
            if(listBox3.SelectedItem!=null)
            {
                Type book = typeof(BookData);
                var list = book.GetProperties();
                string r = "";
                foreach(var ele in list)
                {
                    r += string.Format("{0} : {1}\r\n", ele.Name, ele.GetValue(listBox3.SelectedItem));
                }
                MessageBox.Show(r);
            }
        }

        private void Filter1_TextChanged(object sender, EventArgs e)
        {
            if (FreqSet == null)
                return;
            Regex r = new Regex(Filter1.Text);
            listBox1.Items.Clear();
            foreach(var ele in FreqSet)
            {
                if(r.Match(ele.ToString()).Success)
                {
                    listBox1.Items.Add(ele);
                }
            }
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem != null)
                Filter1.Text = listBox2.SelectedItem.ToString();
        }

        public void ExportData()
        {
            string savePath;
            if (books == null)
                return;
            if (FreqSet == null)
                return;
            var Result = netSave.ShowDialog();
            if (Result != DialogResult.OK)
                return;
            savePath = netSave.FileName;
            Dictionary<string, int> ids=new Dictionary<string, int>();
            Dictionary<string, int> value = new Dictionary<string, int>();
            Dictionary<KeyValuePair<int, int>, int> edges=new Dictionary<KeyValuePair<int, int>, int>();
            int cnt=0;
            foreach(var item in FreqSet)
            {
                foreach(var ele in item.KeyStrs)
                {
                    if (!ids.ContainsKey(ele))
                        ids[ele] = ++cnt;
                }
            }
            foreach(var item in FreqSet)
            {
                int i, j;
                for(i=0;i<item.KeyStrs.Count;++i)
                {
                    for(j=i+1;j<item.KeyStrs.Count;++j)
                    {
                        KeyValuePair<int,int> pair=new KeyValuePair<int, int>(
                        ids[item.KeyStrs[i]],ids[item.KeyStrs[j]]);
                        if (edges.ContainsKey(pair) && edges[pair]>=item.support)
                            continue;
                        edges[pair] = item.support;
                        if (!value.ContainsKey(item.KeyStrs[i]))
                            value[item.KeyStrs[i]] = item.support;
                        else
                            value[item.KeyStrs[i]] += item.support;
                        if (!value.ContainsKey(item.KeyStrs[j]))
                            value[item.KeyStrs[j]] = item.support;
                        else
                            value[item.KeyStrs[j]] += item.support;
                    }
                }
            }
            string result = "*Vertices " + ids.Count;
            foreach(var ele in ids)
            {
                result += "\n" + ele.Value + " \"" + ele.Key + "\"" + " 0.4 0.4 " + value[ele.Key]/80;
            }
            result += "\n" + "*Edges";
            foreach(var ele in edges)
            {
                result += "\n" + ele.Key.Key + " " + ele.Key.Value + " " + ele.Value;
            }
            System.IO.File.WriteAllText(savePath, result);
        }

        private void 导出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportData();
        }
    }
}
