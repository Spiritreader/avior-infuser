using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recording_Infuser_Windows.Database.Schemas;

namespace Recording_Infuser_Windows.Database
{
    class MongoDBManager
    {
        private String url;
        private MongoClient MongoClient;
        public readonly String ClientCollectionName;
        public readonly String JobCollectionName;
        private String DatabaseName;

        private IMongoDatabase Database;
        private IMongoCollection<Client> Clients;
        private IMongoCollection<Job> Jobs;
        /// <summary>
        /// Connects to MongoDB and loads/intializes the Avior database and collections
        /// </summary>
        /// <param name="url">Database address</param>
        /// <param name="DatabaseName">Desired Name for the Avior DB</param>
        /// <param name="ClientCollectionName">Desired Collection Name for Clients</param>
        /// <param name="JobCollectionName">Desired Collection name for Jobs</param>
        public MongoDBManager(String url, String DatabaseName, String ClientCollectionName, String JobCollectionName)
        {
            this.url = url;
            this.DatabaseName = DatabaseName;
            this.ClientCollectionName = ClientCollectionName;
            this.JobCollectionName = JobCollectionName;
        }

        public Boolean Connect()
        {
            var options = new CreateIndexOptions()
            {
                Unique = true
            };
            try
            {
                MongoClientSettings settings = new MongoClientSettings();
                MongoClient = new MongoClient(url);
                Database = MongoClient.GetDatabase(DatabaseName);
                Clients = Database.GetCollection<Client>(ClientCollectionName);
                var clientIndexKeysBuilder = Builders<Client>.IndexKeys;
                var indexModel = new CreateIndexModel<Client>(clientIndexKeysBuilder.Ascending(_ => _.Name), options);
                Clients.Indexes.CreateOne(indexModel);
                Jobs = Database.GetCollection<Job>(JobCollectionName);
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns all clients that have reported themselves as available
        /// </summary>
        /// <returns>List of available clients</returns>
        public List<Client> GetAvailableClients()
        {
            List<Client> clients = GetClients();
            List<Client> availableClients = new List<Client>();
            foreach (Client client in clients)
            {
                if (client.Online)
                {
                    if (Extensions.NowIsBetweenTimes(client.AvailabilityStart, client.AvailabilityEnd))
                    {
                        availableClients.Add(client);
                    }
                }
            }
            return availableClients;
        }

        /// <summary>
        /// Returns all clients that have reported themselves as available
        /// Only accounts online state, not availability
        /// </summary>
        /// <returns>List of available clients</returns>
        public List<Client> GetAvailableClientsOnlineOnly()
        {
            List<Client> clients = GetClients();
            List<Client> availableClients = new List<Client>();
            foreach (Client client in clients)
            {
                if (client.Online || client.IgnoreOnline)
                {
                    availableClients.Add(client);
                }
            }
            return availableClients;
        }


        /// <summary>
        /// Follows a Client reference in the job Schema
        /// </summary>
        /// <param name="job"></param>
        /// <returns>A client object</returns>
        public Client GetClientForJob(Job job)
        {
            return Extensions.FetchDBRef<Client>(Database, job.AssignedClient);
        }

        /// <summary>
        /// Determines how many jobs a client currently has active
        /// </summary>
        /// <returns>Job Count for a Client</returns>
        public int GetClientJobCount(Client client)
        {
            List<Job> jobs = GetJobs();
            int count = 0;
            foreach (Job job in jobs)
            {            
                if ((job.AssignedClientLoaded != null) && client.Name.Equals(job.AssignedClientLoaded.Name))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Returns all clients
        /// </summary>
        /// <returns>A job object</returns>
        public List<Client> GetClients()
        {
            try
            {
                return Clients.Find(Builders<Client>.Filter.Empty).ToList();
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return new List<Client>();
            }            
        }

        /// <summary>
        /// Returns all jobs and populates the AssignedClientLoaded object by following its reference
        /// </summary>
        /// <returns>Fully populated Job object</returns>
        public List<Job> GetJobs()
        {
            try
            {
                List<Job> jobs = Jobs.Find(Builders<Job>.Filter.Empty).ToList();
                jobs.ForEach(j => j.AssignedClientLoaded = GetClientForJob(j));
                return jobs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new List<Job>();
            }
        }

        /// <summary>
        /// Adds a new client
        /// </summary>
        /// <param name="client">Client object</param>
        /// <returns>true if no errors; false otherwise</returns>
        public bool InsertClient(Client client)
        {
            try
            {
                Clients.InsertOne(client);
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Inserts a new job
        /// </summary>
        /// <param name="job">Job object</param>
        /// <returns>true if no errors; false otherwise</returns>
        public bool InsertJob(Job job)
        {
            try
            {
                Jobs.InsertOne(job);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for a duplicate entry in the database
        /// </summary>
        /// <param name="path">Path to check for duplicates.</param>
        /// <returns>True if a duplicate was found; false otherwise</returns>
        public bool checkDuplicate(string path)
        {
            var filter = new BsonDocument("Path", path);
            List<Job> jobs = Jobs.Find(filter).ToList();
            if (jobs.Count != 0)
            {
                return true;
            }
            return false;
        }
    }
}
