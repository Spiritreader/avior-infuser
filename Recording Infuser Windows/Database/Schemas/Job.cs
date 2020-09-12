using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Recording_Infuser_Windows.Database.Schemas
{
    [BsonIgnoreExtraElements]
    public class Job
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public String Path { get; set; }
        public String Name { get; set; }
        public String Subtitle { get; set; }
        public MongoDBRef AssignedClient { get; set; }
        [BsonIgnore]
        public Client AssignedClientLoaded { get; set; }
    }
}
