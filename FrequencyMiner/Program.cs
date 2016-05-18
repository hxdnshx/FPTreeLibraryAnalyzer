using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryDatabase;
using System.Data.Entity;
using System.Data.SQLite.EF6;
using System.Data.SQLite;
using System.Data.Entity.Core.Common;

namespace FrequencyMiner
{
    class Program
    {
        static List<string> GenKeys(IEnumerable<UserData> g)
        {
            var list = new List<string>();
            foreach(var item in g)
            {
                if(list.Contains(item.KeyStr)==false)
                    list.Add(item.KeyStr);
            }
            return list;
        }
        static void Main(string[] args)
        {
            if (args.Count() < 2)
                return;
            if (!File.Exists(args[0]))
                return;

            
        }
    }
    public class SQLiteConfiguration : DbConfiguration
    {
        public SQLiteConfiguration()
        {
            SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
            SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
            SetProviderServices("System.Data.SQLite", (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)));
        }
    }
}
