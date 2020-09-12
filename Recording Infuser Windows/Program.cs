using Recording_Infuser_Windows;
using Recording_Infuser_Windows.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Recording_Infuser_Windows.Database.Schemas;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace Recording_Infuser
{
    class Program
    {
        private static string connectionString;
        private static readonly string dbName = "Avior";
        private static readonly string clientCollection = "clients";
        private static readonly string jobCollection = "jobs";
        private static readonly string confFile = "InfuserDB.conf";

        static void Main(string[] args)
        {
            Console.WriteLine("Infuser Version 1.03 - \"tetrising\"");
            //Heartbeat(args);

            Log lgr = new Log();

            if (UseDatabase())
            {
                lgr.DatabaseLogs(true);
                Infuser inf = new Infuser(args, lgr, new MongoDBManager(connectionString, dbName, clientCollection, jobCollection));
                bool res = inf.InfuseRemote(10);
                inf.LogWrite(res);
            } 
            else
            {
                Infuser inf = new Infuser(args, lgr);
                bool res = inf.InfuseLocal(10);
                inf.LogWrite(res);
            }
            Environment.Exit(0);
        }

        static bool UseDatabase()
        {
            string workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string confPath = Path.Combine(workingDir, confFile);
            try
            {
                if (!File.Exists(confPath))
                {
                    var list = new List<String>();
                    list.Add("# Remove the hashtag below to enable MongoDB support. Customize your login string as needed");
                    list.Add("# mongodb://localhost");
                    File.AppendAllLines(confPath, list);
                }
                else
                {
                    var content = File.ReadAllLines(confPath);
                    foreach (String line in content)
                    {
                        if (!line.StartsWith("#"))
                        {
                            connectionString = line.Trim();
                            return true;
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e);
                return false;
            }

            return false;
        }

        public static void TestDbClient(MongoDBManager mdm)
        {
            Client client = new Client();
            client.AvailabilityStart = "0:00";
            client.AvailabilityEnd = "0:00";
            client.Name = "VDR";
            client.Priority = 1;
            client.MaximumJobs = 2;
            client.Online = false;
            mdm.InsertClient(client);
        }

        public static void TestDbJob(MongoDBManager mdm)
        {
            List<Client> clients = mdm.GetClients();
            Job job = new Job {
                Name = "Neva Give üp",
                Subtitle = "Der einzig wahre Japaner",
                Path = "D:\\Recording\\Neva Give üp - Der einzig wahre Japaner.mkv",
                AssignedClient = new MongoDB.Driver.MongoDBRef("clients", clients.First().Id),
            };
            mdm.InsertJob(job);
        }

        static void Heartbeat(string[] args)
        {
            var culture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = culture;
            Console.WriteLine(CultureInfo.CurrentCulture);
            using (TextWriter w = File.AppendText("Heartbeat.log"))
            {
                w.WriteLine("Heartbeat at " + DateTime.Now.ToString());
                foreach (String param in args)
                {
                    w.WriteLine(param);
                }
                w.WriteLine("");
            }
        }
    }


}
