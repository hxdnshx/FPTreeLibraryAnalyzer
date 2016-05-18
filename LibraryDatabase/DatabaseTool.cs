using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data.SQLite.Linq;
using System.Linq.Expressions;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Core.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.IO;
using LinqToExcel.Attributes;
using LinqToExcel;
using System.Reflection;

namespace LibraryDatabase
{
    public class Record
    {
        [ExcelColumn("证件号")]
        public string UserID { get; set; }
        [ExcelColumn("财产号")]
        public string BookID { get; set; }
        [ExcelColumn("题名")]
        public string BookName { get; set; }
        [ExcelColumn("ISBN号")]
        public string ISBN { get; set; }
        [ExcelColumn("索书号")]
        public string Category { get; set; }
    }
    [Table("UserData")]
    public class UserData
    {
        public string KeyStr { get; set; }
        
        public string UserID { get; set; }
        [Key]
        public int ID { get; set; }
    };
    [Table("Frequency_1")]
    public class Freq_1
    {
        [Key]
        public string KeyStr { get; set; }
        public int Amount { get; set; }
    }
    [Table("BookData")]
    public class BookData
    {
        public string KeyStr { get; set; }
        [Key]
        public string BookID { get; set; }
        public string BookName { get; set; }
        public string ISBN { get; set; }
        public string Category { get; set; }

        public override string ToString()
        {
            return BookName;
        }
    };

    [Table("MineResult")]
    public class MineResult
    {
        [Key]
        public int ID { get; set; }
        public int ResultID { get; set; }
        public string KeyStr { get; set; }
        public int support { get; set; }
    }

    public class Tree
    {
        public List<TreeNode> nodes;
        public Dictionary<string, TreeNode> next;
        public TreeNode root;
        public Tree()
        {
            nodes = new List<TreeNode>();
            next = new Dictionary<string, TreeNode>();
            root = new TreeNode()
            {
                KeyStr="Root",
                support=0,
                parent=null,
                next=null
            };
        }
        public TreeNode Insert(TreeNode parent,TreeNode ins)
        {
            parent.childs.Add(ins);
            ins.parent = parent;
            nodes.Add(ins);
            return ins;
        }
    }

    public class TreeNode
    {
        public string KeyStr;
        public int support;
        public TreeNode parent;
        public TreeNode next;//下一个相同KeyStr的节点
        public List<TreeNode> childs;
        public TreeNode()
        {
            childs = new List<TreeNode>();
            support = 0;
        }
    }

    public class ItemSet
    {
        public int support;
        public List<string> KeyStrs;
        public ItemSet()
        {
            KeyStrs = new List<string>();
        }
        public override string ToString()
        {
            string items = null;
            foreach(var ele in KeyStrs)
            {
                if (items == null)
                    items = ele;
                else
                    items += "," + ele;
            }
            return "支持：" + support + "  " + items;
        }

    }

    public class SystemContext : DbContext
    {
       
        public DbSet<BookData> BookData { get; set; }
        
        public DbSet<UserData> UserData { get; set; }
        public DbSet<Freq_1> Frequency_1 { get; set; }

        public DbSet<MineResult> MineResult { get; set; }
        public SystemContext(string conn)
            : base(new SQLiteConnection(conn),true)
        {
        }
    }
    public class DatabaseTool
    {
        public static void InitializeDatabase(string filepath)
        {
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + filepath);
            conn.Open();
            using (SQLiteCommand cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = "create table UserData(ID int,UserID varchar(255) NOT NULL,sortStr varchar(255))";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "create table BookData(BookID varchar(255) NOT NULL,BookName varchar(255),ISBN varchar(255),CategoryID varchar(255),Lookcnt int,sortStr varchar(255))";
                cmd.ExecuteNonQuery();
            }
        }
        float minacc;
        int minsup;
        float minsuppercent;
        //public Dictionary<string, int> FreqOrder;
        public List<ItemSet> Result;

        public void SetArgs(IQueryable<Freq_1> freq,float minsupport,float minacc)
        {
            //FreqOrder = new Dictionary<string, int>();
            this.minacc = minacc;
            //var orderedfreq = freq;//.OrderBy(a => a.Amount);
            minsuppercent = minsupport;
        }



        private void SubmitResult(TreeNode node,List<string> Prefix)
        {
            ItemSet r = new ItemSet() {
                support=node.support
            };
            for(;;)
            {
                if (node == null ||node.parent==null)//没有Parent，即root节点
                    break;
                r.KeyStrs.Add(node.KeyStr);
                node = node.parent;
            }
            foreach(var item in Prefix)
            {
                if (r.KeyStrs.Contains(item))
                    continue;
                r.KeyStrs.Add(item);
            }
            if (Result == null)
                Result = new List<ItemSet>();
            Result.Add(r);
        }

        public void FPgrowth(IEnumerable<ItemSet> itemsets,List<string> PrefixKeyStr)
        {
            Tree fptree = new Tree();
            Dictionary<string,int> Freq;
            if (true)
            {
                Freq = new Dictionary<string, int>();
                foreach(var item in itemsets)
                {
                    foreach(var KeyStr in item.KeyStrs)
                    {
                        if (Freq.ContainsKey(KeyStr))
                            Freq[KeyStr] += item.support;
                        else
                            Freq[KeyStr] = item.support;
                    }
                }
            }
            if(PrefixKeyStr.Count==0)
            {
                int i = 0;
                foreach(var item in Freq)
                {
                    i += item.Value;
                }
                minsup = (int)(minsuppercent * i);
            }
            //Generate FPTree
            foreach(var item in itemsets)
            {
                TreeNode node = fptree.root;
                foreach(var ele in item.KeyStrs.OrderByDescending(a=>Freq[a]))
                {
                    if (Freq[ele] < minsup)
                        continue;
                    var childnode = node.childs.Find(x => x.KeyStr == ele);
                    if(childnode==null)
                    {
                        childnode = fptree.Insert(node, new TreeNode() {
                            KeyStr=ele,
                            support=0
                        });
                        if (fptree.next.ContainsKey(ele))
                            childnode.next = fptree.next[ele];
                        fptree.next[ele] = childnode;
                    }
                    childnode.support += item.support;
                    node = childnode;
                }
            }
            itemsets = null;

            //Mining Frequency Set
            Tree tree = fptree;
            if (tree.root.childs.Count == 0)
                return;
            if (tree.root.childs.Count == 1)
            {
                TreeNode node = tree.root.childs.First();
                List<TreeNode> nodes = new List<TreeNode>();
                for (;;)
                {
                    if (node == null)
                        break;
                    if (node.childs.Count > 1)
                    {
                        break;
                    }
                    if (node.support < minsup)
                        break;
                    nodes.Add(node);
                    if (node.childs.Count == 0)
                        break;
                    node = node.childs.First();
                }
                if (node == tree.root || node.childs.Count>=1)//如果没有足够的支持/有分叉
                    return;
                SubmitResult(node, PrefixKeyStr);
                /*
                for(;;)
                {
                    if (nodes.Count == 0)
                        break;
                    foreach (var item in nodes)
                        SubmitResult(item, PrefixKeyStr);
                    PrefixKeyStr.Add(nodes.First().KeyStr);
                    nodes.Remove(nodes.First());
                }
                */
                return;
            }
            else
            {
                foreach(var ele in tree.next)//遍历每个Header
                {
                    TreeNode node=ele.Value;
                    List<ItemSet> conditional = new List<ItemSet>();
                    List<string> PrefixList = new List<string>();
                    PrefixList.AddRange(PrefixKeyStr);
                    PrefixList.Add(node.KeyStr);
                    for (;;)//遍历Header连接的每个对应node
                    {
                        if (node == null)
                            break;
                        ItemSet newset=new ItemSet() { support=node.support};
                        TreeNode item = node.parent;
                        for(;;)//遍历node的根部
                        {
                            if (item == null)
                                break;
                            if (item == tree.root)
                                break;
                            newset.KeyStrs.Add(item.KeyStr);
                            item = item.parent;
                        }
                        if (newset.KeyStrs.Count ()!=0)
                            conditional.Add(newset);
                        node = node.next;
                    }
                    if (conditional.Count() == 0)
                        continue;
                    FPgrowth(conditional, PrefixList);
                }
            }
           
        }

        public static void ReadFromExcel(string FilePath,string SavePath, string CategoryRegex = "([A-Za-z]+[0-9]+\\.?[0-9]*/[0-9]+).*?",string KeyProp="Category")
        {
            Type excelinfotype = typeof(Record);
            PropertyInfo info = excelinfotype.GetProperty(KeyProp);
            var getProp=  (Func<Record, string>)info.GetGetMethod().CreateDelegate(typeof(Func<Record, string>));
            Regex r = new Regex(CategoryRegex);

            if (!File.Exists(FilePath))
                return;
            var excel = new ExcelQueryFactory(FilePath);
            var Records = from c in excel.Worksheet<Record>("njupt_2014")
                          orderby c.ISBN
                          select c;
            File.Copy("EmptyDB.db", SavePath, true);
            SystemContext db = new SystemContext(@"Data Source=" + SavePath);
            int i = 1;
            string prevISBN = "";
            UserData tmpu = new UserData();
            BookData tmpb = new BookData();
            db.Configuration.AutoDetectChangesEnabled = false;
            foreach (var rec in Records)
            {
                if (rec.ISBN == null)
                    continue;

                Match mat = r.Match(getProp(rec));
                if (!mat.Success)
                    continue;
                string sortStr = mat.Result("$1");
                db.UserData.Add(new UserData()
                {
                    UserID = rec.UserID,
                    ID = i++,
                    KeyStr = sortStr,
                });
                if (prevISBN != rec.ISBN)
                {
                    db.BookData.Add(new BookData()
                    {
                        BookID = rec.BookID,
                        KeyStr = sortStr,
                        BookName = rec.BookName,
                        ISBN = rec.ISBN,
                        Category = rec.Category,
                    });
                    prevISBN = rec.ISBN;
                }
                if (i % 40000 == 0)
                    db.SaveChanges();
            }
            db.SaveChanges();
            //DatabaseTool.Calc1_Set(db);
        }

        public static void Analyze(string FilePath,string support)
        {
            Database.SetInitializer<SystemContext>(null);
            SystemContext db = new SystemContext(@"Data Source=" + FilePath);
            DatabaseTool miner = new DatabaseTool();
            db.Configuration.AutoDetectChangesEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.UseDatabaseNullSemantics = false;
            db.Configuration.ValidateOnSaveEnabled = false;
            var freq_1 = from c in db.Frequency_1
                         select c;
            miner.SetArgs(freq_1, float.Parse(support), 50);
            var itemsets = from c in db.UserData
                           orderby c.UserID
                           select c;
            List<ItemSet> ilist = new List<ItemSet>();
            ItemSet newitem = null;
            string UserID = "";
            foreach (var item in itemsets)
            {
                if (newitem == null || item.UserID != UserID)
                {
                    if (newitem != null)
                        ilist.Add(newitem);
                    newitem = new ItemSet()
                    {
                        support = 1
                    };
                    UserID = item.UserID;
                }
                if (!newitem.KeyStrs.Contains(item.KeyStr))
                    newitem.KeyStrs.Add(item.KeyStr);
                else
                    newitem.support += 1;
            }
            if (newitem != null)
                ilist.Add(newitem);
            itemsets = null;
            miner.FPgrowth(ilist, new List<string>());
            db.Configuration.AutoDetectChangesEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
            int i = 1;
            int j = 0;
            ilist = null;
            newitem = null;
            freq_1 = null;
            GC.Collect();
            if (miner.Result != null)
            {
                foreach (var ele in miner.Result)
                {
                    foreach (var key in ele.KeyStrs)
                    {
                        db.MineResult.Add(new MineResult()
                        {
                            support = ele.support,
                            KeyStr = key,
                            ResultID = i
                        });
                    }
                    ++i;
                    if (j++ % 40000 == 0)
                    {
                        db.SaveChanges();
                    }
                }

            }
            db.SaveChanges();
        }
        public static void Calc1_Set(SystemContext conn)
        {
            var lst = from c in conn.UserData
                      group c by c.KeyStr into q
                      select q;
            conn.Configuration.AutoDetectChangesEnabled = false;
            foreach (var ele in lst)
            {
                conn.Frequency_1.Add(new Freq_1() {
                    KeyStr=ele.First().KeyStr,
                    Amount=ele.Count()
                });
            }
            conn.Configuration.AutoDetectChangesEnabled = true;
            conn.SaveChanges();
        }
    }
    public class SQLiteConfiguration : DbConfiguration
    {
        public SQLiteConfiguration()
        {
            SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
            SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
            //SetProviderServices("System.Data.SQLite", (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)));
        }
    }

    
}
