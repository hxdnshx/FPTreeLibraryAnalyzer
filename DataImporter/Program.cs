#define IMP
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data.Entity;
using System.Data.SQLite.Linq;
using LinqToExcel;
using System.Data.SQLite;
using LinqToExcel.Attributes;
using System.Text.RegularExpressions;
using LibraryDatabase;
using System.Data.Entity.Core.Common;

namespace DataImporter
{

    



    
    class Program
    {
#if OLDIMP
        static void Main(string[] args)
        {
            string categoryregex = "([A-Za-z]+[0-9]+\\.?[0-9]*/[0-9]+).*?";
            Regex r = new Regex(categoryregex);

            if (!File.Exists(args[0]))
                return;
            var excel = new ExcelQueryFactory(args[0]);
            var Records = from c in excel.Worksheet<Record>("njupt_2014")
                          orderby c.Category
                          select c;
            //var targetdb = new System.Data.SQLite.
            SQLiteConnection conn=new SQLiteConnection("Data Source=" + "result.db");
            conn.Open();

            string previd="";
            int cnt = 0;
            int insertcnt = 0;
            string insertsql="";
            int insertcnt2 = 0;
            string insertsql2 = "";
            Record recx=new Record();
            foreach (var rec in Records)
            {
                Match mat = r.Match(rec.Category);
                if (!mat.Success)
                    continue;
                string sortStr = mat.Result("$1");
                recx = rec;
                if(sortStr!=previd)
                {
                    if(previd!="")
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(conn))
                        {
                            
                            if(rec.ISBN==null)
                                rec.ISBN = "";
                            if (rec.BookName != null)
                            {
                                if (insertcnt == 0)
                                    insertsql = "INSERT INTO BookData(BookID,BookName,ISBN,CategoryID,Lookcnt,sortStr) VALUES";
                                else
                                    insertsql += ",";
                                insertsql += string.Format("('{0}','{1}','{2}','{3}',{4},'{5}')", rec.BookID, rec.BookName.Replace("'", "''"), rec.ISBN.Replace("'", "''"), rec.Category, cnt.ToString(),sortStr);
                                insertcnt++;
                                if (insertcnt >= 100)
                                {
                                    cmd.CommandText = insertsql;
                                    cmd.ExecuteNonQuery();
                                    insertcnt = 0;
                                }
                            }
                        }
                    }
                    previd=sortStr;
                    cnt = 1;
                }
                else
                {
                    cnt++;
                }
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    if (insertcnt2 == 0)
                        insertsql2 = "INSERT INTO UserData(UserID,sortStr) VALUES";
                    else
                        insertsql2 += ",";
                    insertsql2+= string.Format("('{0}','{1}')", rec.UserID,sortStr);
                    insertcnt2++;
                    if(insertcnt2>=1000)
                    {
                        cmd.CommandText = insertsql2;
                        cmd.ExecuteNonQuery();
                        Console.WriteLine(rec.UserID + "\t" + rec.BookID);
                        insertcnt2 = 0;
                    }
                }
            }
            if (previd != "")
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    if (insertcnt == 0)
                        insertsql = "INSERT INTO BookData(BookID,BookName,ISBN,CategoryID,Lookcnt,sortStr) VALUES";
                    else
                        insertsql += ",";
                    
                    cmd.CommandText = insertsql + string.Format("('{0}','{1}','{2}','{3}',{4},'{5}')", recx.BookID.Replace("'", "''"), recx.BookName.Replace("'", "''"), recx.ISBN, recx.Category, cnt.ToString(),
                        r.Match(recx.Category).Result("$1"));
                    cmd.ExecuteNonQuery();
                }
            }
            if(insertcnt2!=0)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = insertsql2;
                    cmd.ExecuteNonQuery();
                }
            }
            conn.Close();
        }
#else
        static void Main(string[] args)
        {
            string categoryregex = "([A-Za-z]+[0-9]+\\.?[0-9]*/[0-9]+).*?";
            Regex r = new Regex(categoryregex);

            if (!File.Exists(args[0]))
                return;
            var excel = new ExcelQueryFactory(args[0]);
            var Records = from c in excel.Worksheet<Record>("njupt_2014")
                          orderby c.ISBN
                          select c;
            File.Copy("EmptyDB.db", "resultLinq.db", true);
            SystemContext db = new SystemContext(@"Data Source=resultLinq.db");
            int i = 1;
            string prevISBN="";
            UserData tmpu=new UserData();
            BookData tmpb = new BookData();
            db.Configuration.AutoDetectChangesEnabled = false;
            foreach (var rec in Records)
            {
                if (rec.ISBN == null)
                    continue;
                
                Match mat = r.Match(rec.Category);
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
                    db.BookData.Add(new BookData(){
                        BookID = rec.BookID,
                        KeyStr = sortStr,
                        BookName = rec.BookName,
                        ISBN = rec.ISBN,
                        Category = rec.Category,
                    });
                    prevISBN = rec.ISBN;
                }
            }
            db.SaveChanges();
            DatabaseTool.Calc1_Set(db);
            return;

        }
#endif
    }
}
