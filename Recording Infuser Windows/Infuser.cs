using Recording_Infuser_Windows;
using Recording_Infuser_Windows.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Recording_Infuser_Windows.Database.Schemas;
using MongoDB.Driver;

namespace Recording_Infuser
{
    class Infuser
    {
        private readonly string ToCodeFilename = "toCode.txt";
        private readonly string PathTag = "path=";
        private readonly string NameTag = "name=";
        private readonly string SubTag = "sub=";
        private readonly string StartSeparator = "__s__";
        private readonly string EndSeparator = "__e__";
        private readonly string LockType = ".lck";
        private string location;
        private List<string> parameters;
        public delegate void LogAddDel(string message);
        public delegate void LogWriteDel(bool success);
        public LogAddDel LogAdd;
        public LogWriteDel LogWrite;
        private MongoDBManager mdm;

        /// <summary>
        /// Initializes the Infuser with command line parameters 
        /// and retrieves the console application's execution directory.
        /// </summary>
        /// <param name="args">Command line parameters from Main</param>
        public Infuser(string[] args, Log lgr)
        {
            location = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Console.WriteLine("Execution Directory: " + location);
            parameters = new List<string>();
            foreach (String elem in args) 
            {
                parameters.Add(elem);
            }
            LogAdd = lgr.LogAdd;
            LogWrite = lgr.LogWrite;
        }

        /// <summary>
        /// Initializes the Infuser with command line parameters and database storage capabilities 
        /// and retrieves the console application's execution directory.
        /// </summary>
        /// <param name="args">Command line parameters from Main</param>
        /// <param name="lgr">A Logger</param>
        /// <param name="mdm">MongoDB Wrapper for data manipulation</param>
        public Infuser(string[] args, Log lgr, MongoDBManager mdm) : this(args, lgr)
        {
            this.mdm = mdm;
        }

        /// <summary>
        /// Pushes job to database if clients are available and online
        /// </summary>
        /// <param name="maxTries">Maximum tries for fallback behavior (lock removal)</param>
        /// <returns></returns>
        public Boolean InfuseRemote(int maxTries)
        {
            if (parameters.Count != 3)
            {
                LogAdd("Invalid parameter count");
                return false;
            }

            if (!mdm.Connect())
            {
                LogAdd("Error connecting to database. Fallback to local infusion");
                return InfuseLocal(maxTries);
            }
            //Program.TestDbClient(mdm);
            //Program.TestDbJob(mdm);

            if (mdm.checkDuplicate(parameters.ElementAt(0))) {
                LogAdd("Duplicate found in db");
                LogAdd("Path: " + parameters.ElementAt(0));
                LogAdd("Job not added to queue");
                return false;
            }

            List<Client> availableClients = mdm.GetAvailableClientsOnlineOnly();
            if (availableClients.Count == 0)
            {
                LogAdd("No available Clients. Fallback to local infusion");
                return InfuseLocal(maxTries);
            }

            int highestPrio = -1;
            Client eligible = null;
            foreach (Client client in availableClients)
            {
                if (client.Priority < 0)
                {
                    continue;
                }
                //Always set eligible client to highest priority with free jobs
                if (client.Priority > highestPrio)
                {
                    if ((mdm.GetClientJobCount(client) < client.MaximumJobs) || client.MaximumJobs == -1)
                    {
                        eligible = client;
                        highestPrio = client.Priority;
                    }                    
                }
                //If priority is the same, only assign if current client's number of jobs is smaller than currently set eligible client
                else if (client.Priority == highestPrio)
                {
                    int count = mdm.GetClientJobCount(client);
                    if ((count < client.MaximumJobs) || client.MaximumJobs == -1)
                    {
                        if (eligible != null)
                        {
                            if (count < mdm.GetClientJobCount(eligible))
                            {
                                eligible = client;
                            }
                        }
                    }
                }
            }
            if (eligible != null)
            {
               return PushJobToDatabase(eligible, maxTries);
            }
            List<Client> query = availableClients.Where(e => e.Name.Equals(Environment.MachineName)).ToList();
            Client local = null;
            if (query.Count == 1)
            {
                local = query.First();
            }
            if (local != null)
            {
                LogAdd("No Eligible Client found. Pushing to local machine instead");
                return PushJobToDatabase(local, maxTries);
            }
            else
            {
                LogAdd("No Eligible Client found. Fallback to local infusion.");
                return InfuseLocal(maxTries);
            }            
        }

        private Boolean PushJobToDatabase(Client eligible, int maxTries)
        {
            Job job = new Job
            {
                AssignedClient = new MongoDBRef(mdm.ClientCollectionName, eligible.Id),
                Path = parameters.ElementAt(0),
                Name = parameters.ElementAt(1),
                Subtitle = parameters.ElementAt(2)
            };
            if (mdm.InsertJob(job))
            {
                LogAdd(StartSeparator);
                LogAdd(PathTag + job.Path);
                LogAdd(NameTag + job.Name);
                LogAdd(SubTag + job.Subtitle);
                LogAdd(EndSeparator);
                LogAdd("Job pushed to client " + eligible.Name);
                return true;
            }
            else
            {
                LogAdd("Error inserting job. Fallback to local infusion");
                return InfuseLocal(maxTries);
            }
        }

        /// <summary>
        /// Transmorphs command line parameters into a format readable by Avior.
        /// Creates a lock before writing to the ToCode file. 
        /// </summary>
        /// <param name="maxTries">Number of maximum wait for a file unlock before returning an error</param>
        /// <returns></returns>
        public Boolean InfuseLocal(int maxTries)
        {
            if (parameters.Count != 3)
            {
                LogAdd("Invalid parameter count");
                return false;
            }
            List<string> output = new List<string>();
            output.Add("");
            output.Add(StartSeparator);
            output.Add(PathTag + parameters.ElementAt(0));
            output.Add(NameTag + parameters.ElementAt(1));
            output.Add(SubTag + parameters.ElementAt(2));
            output.Add(EndSeparator);
            Console.WriteLine("finished converting parameters:");
            output.Where(e => (e.Count() != 0)).ToList().ForEach(e => LogAdd(e));

            string lockFile = ToCodeFilename + LockType;
            int tries = 0;
            bool hasLock = false;
            while (tries < maxTries)
            {
                if (File.Exists(lockFile))
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    try
                    {
                        File.Create(lockFile).Dispose();
                    }
                    catch (IOException e)
                    {
                        LogAdd("Couldn't create lock file. " + e.Message);
                        return false;
                    }
                    hasLock = true;
                    break;
                }                
            }

            if (!hasLock)
            {
                LogAdd("Permanent file lock engaged. Can't modify " + ToCodeFilename);
                return false;
            }

            try
            {
                File.AppendAllLines(ToCodeFilename, output, Encoding.Default);
                LogAdd("Job added");
            } 
            catch (IOException e)
            {
                LogAdd("Couldn't add job to " + ToCodeFilename + " file " + e.Message);
                return false;
            }
            finally
            {
                try
                {
                    File.Delete(lockFile);
                }
                catch (IOException e)
                {
                    LogAdd("Couldn't remove lock: " + e.Message);
                }
            }
            return true;
        }
    }
}
