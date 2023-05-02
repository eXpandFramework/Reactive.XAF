using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Transform.System.Net;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class JsonExtensionsTests {
        private const string JsonArray = @"[
            {""id"": 1, ""name"": ""Alice""},
            {""id"": 2, ""name"": ""Bob""},
            {""id"": 3, ""name"": ""Charlie""},
            {""id"": 4, ""name"": ""David""},
            {""id"": 5, ""name"": ""Eve""},
            {""id"": 6, ""name"": ""Frank""},
            {""id"": 7, ""name"": ""Grace""},
            {""id"": 8, ""name"": ""Heidi""},
            {""id"": 9, ""name"": ""Ivan""},
            {""id"": 10, ""name"": ""Judy""}
        ]";
        private const string APIUrl = "https://jsonplaceholder.typicode.com/todos"; // Replace with your API URL
        [Test]
        public async Task ReadJsonArrayInChunks_ShouldReadAllItems() {
            var jsonBytes = Encoding.UTF8.GetBytes(JsonArray);
            var stream = new MemoryStream(jsonBytes);
            stream.Position = 0;
            // (await stream.ReadJsonArrayInChunks().CountAsync()).ShouldBe(10);
            
            var httpClient = new HttpClient();
            await httpClient.WhenResponseDocumentInChunks(APIUrl).Select(element => element);
        }
    }
}