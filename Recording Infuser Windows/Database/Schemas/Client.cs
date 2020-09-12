using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recording_Infuser_Windows.Database.Schemas
{
    [BsonIgnoreExtraElements]
    public class Client
    {
        [BsonId]
        public ObjectId Id { get; private set; }
        [BsonElement("Name")]
        public String Name { get; set; }
        public String AvailabilityStart { get; set; }
        public String AvailabilityEnd { get; set; }
        public int MaximumJobs { get; set; }
        public int Priority { get; set; }
        public bool Online { get; set; }
        public bool IgnoreOnline { get; set; }
    }
}
