using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Foody
{
    [TestFixture]
    public class FoodyTests
    {
        private RestClient client;
        private static string createdFoodId;
        private static string baseURL = "http://144.91.123.158:81";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("asq92", "123456");

            var options = new RestClientOptions(baseURL)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseURL);

            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString();
        }

        [Test, Order(1)]
        public void CreateFood_ShouldReturnCreated()
        {
            var food = new
            {
                Name = "New Food",
                Description = "Test food description",
                Url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodId = json.GetProperty("foodId").GetString();
        }

        [Test, Order(2)]
        public void EditFoodTitle_ShouldReturnOk()
        {
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "Updated Food Name"}
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);

            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnList()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(foods, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFoodWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var food = new
            {
                Name = "",
                Description = ""
            };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            string fakeId = "123";
            var changes = new[]
            {
                new { path = "/name", op = "replace", value = "Non-existing Food" }
            };
            var request = new RestRequest($"/api/Food/Edit/{fakeId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues.."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnBadRequest()
        {
            string fakeId = "123";
            var request = new RestRequest($"/api/Food/Delete/{fakeId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}
