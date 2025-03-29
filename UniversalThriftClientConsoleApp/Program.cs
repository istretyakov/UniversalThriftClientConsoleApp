using System.Text.Json.Serialization;

namespace UniversalThriftClientConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var client = new UniversalThriftClient("http://localhost:9090");

            var result1 = await client.CallMethodAsync<int>("GetUser3", 3, new User
            {
                ID = 4,
                Name = "TestUser",
                Faction = new Faction
                {
                    ID = 100,
                    Name = "Red Team",
                    Rank = "Captain"
                }
            });

            var result2 = await client.CallMethodAsync<User>("GetUser2", 2, "test");
        }
    }

    public class User
    {
        [JsonPropertyName("field_1")]
        public int ID { get; set; }

        [JsonPropertyName("field_2")]
        public string Name { get; set; }

        [JsonPropertyName("field_3")]
        public Faction Faction { get; set; }
    }

    public class Faction
    {
        [JsonPropertyName("field_1")]
        public int ID { get; set; }

        [JsonPropertyName("field_2")]
        public string Name { get; set; }

        [JsonPropertyName("field_3")]
        public string Rank { get; set; }
    }
}
