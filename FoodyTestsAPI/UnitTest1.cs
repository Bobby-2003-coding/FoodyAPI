using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace FoodyTestsAPI
{
    public record TokenResponse
    {
        public string? Accesstoken { get; set; }
    }

    [TestFixture]
    public class Tests
    {
        private RestClient? client;
        private static string? createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        private JsonElement DeserializeResponse(RestResponse response)
        {
            if (string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException("Response content is null or empty");
            }
            return JsonSerializer.Deserialize<JsonElement>(response.Content);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetToken("Tester2", "tester2");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token),
            };

            client = new RestClient(options);
        }

        private string GetToken(string username, string password)
        {
            var ClientLogin = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            var response = ClientLogin.Execute(request);
            
            var json = DeserializeResponse(response);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test,Order(1)]
        public void CreateFood()
        {
            var food = new
            {
                name = "TestFood",
                description = "TestDescription",
                url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client!.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = DeserializeResponse(response);
            createdFoodId = json.GetProperty("foodId").GetString();
            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food ID should not be null or empty.");
        }

        [Test, Order(2)]
        public void EditTittleOfLastCreatedFood()
        {
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "EditedFoodName" }
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
            var json = DeserializeResponse(response);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods()
        {
            var request = new RestRequest($"/api/Food/All", Method.Get);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var json = DeserializeResponse(response);
            Assert.That(json.GetArrayLength(), Is.GreaterThan(0), "The response should not contain null array");
        }

        [Test, Order(4)]
        public void DeleteTheFoodThatYouEdited()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var json = DeserializeResponse(response);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFoodWithoutRequiermentFields()
        {
            var food = new 
            {
                name = "",
                description = "",
                url = ""
            };

            var request = new RestRequest($"/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood()
        {
            var changes = new[]
            {
                new{path = "/name", op = "repalce", value = "EditedFoodName"}
            };

            var request = new RestRequest($"/api/Food/Edit/123", Method.Patch);
            request.AddJsonBody(changes);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            var json = DeserializeResponse(response);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood()
        {
            var FakeID = "123";
            var request = new RestRequest($"/api/Food/Delete/{FakeID}", Method.Delete);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            var json = DeserializeResponse(response);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Unable to delete this food revue!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}