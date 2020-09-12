using MongoDB.Driver;
using Recording_Infuser_Windows.Database.Schemas;
using System;
using System.Threading.Tasks;

namespace Recording_Infuser_Windows
{
    public static class Extensions
    {
        public static Client FetchDBRef<T>(this IMongoDatabase database, MongoDBRef reference) where T : Client
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, reference.Id);
            return database.GetCollection<T>(reference.CollectionName).Find(filter).FirstOrDefault();
        }

        public static bool NowIsBetweenTimes(string startTime, string endTime)
        {
            if (startTime == "" || endTime == "")
            {
                return false;
            }

            if (startTime == endTime)
            {
                return true;
            }

            try
            {
                TimeSpan start = TimeSpan.Parse(startTime);
                TimeSpan end = TimeSpan.Parse(endTime);
                TimeSpan now = DateTime.Now.TimeOfDay;

                if (start <= end)
                {
                    // start and stop times are in the same day
                    if (now >= start && now <= end)
                    {
                        // current time is between start and stop
                        return true;
                    }
                }
                else
                {
                    // start and stop times are in different days
                    if (now >= start || now <= end)
                    {
                        // current time is between start and stop
                        return true;
                    }
                }
            }
            catch (FormatException e)
            {
                Console.WriteLine(e);
                return false;
            }
            return false;
        }
    }
}
