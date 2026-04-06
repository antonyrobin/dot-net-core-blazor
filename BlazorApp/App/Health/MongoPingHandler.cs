using System.Text.Encodings.Web;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BlazorApp.App.Health
{
    public class MongoPingHandler
    {
        private readonly IMongoDatabase _database;

        public MongoPingHandler(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<IResult> HandleAsync()
        {
            try
            {
                var command = new BsonDocument("ping", 1);
                var result = await _database.RunCommandAsync<BsonDocument>(command);

                var collections = await (await _database.ListCollectionNamesAsync()).ToListAsync();

                return Results.Ok(new
                {
                    status = "healthy",
                    ping = result.ToString(),
                    database = _database.DatabaseNamespace.DatabaseName,
                    collectionsCount = collections.Count,
                    checkedAt = DateTimeOffset.UtcNow
                });
            }
            catch (MongoException mex)
            {
                return Results.Problem(
                    detail: mex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "MongoDB connection failed"
                );
            }
        }

        public async Task<IResult> Ping()
        {
            string username = HttpUtility.UrlEncode("robin123");
            string password = HttpUtility.UrlEncode("robin123");
            string connectionUri = "mongodb+srv://" + username + ":" + password + "@clusterrobin.z5udayq.mongodb.net/?appName=ClusterRobin";
            var settings = MongoClientSettings.FromConnectionString(connectionUri);
            // Set the ServerApi field of the settings object to set the version of the Stable API on the client
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            // Create a new client and connect to the server
            var client = new MongoClient(settings);
            // Send a ping to confirm a successful connection
            try
            {
                var result = client.GetDatabase("dynamics").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "MongoDB connection failed"
                );
            }
        }
    }
}
