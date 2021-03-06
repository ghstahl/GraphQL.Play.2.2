using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Self.Validator;
using Shouldly;
using TestServerFixture;

namespace UnitTestProject_OAuth2
{
    [TestClass]
    public class UnitTest_SimpleObjects
    {
        [TestMethod]
        public async Task SelfValidator_ValidateTokenAsync_null()
        {
            var d = new SelfValidator(null, null);
            (await d.ValidateTokenAsync("malformed")).ShouldBeNull();
        }
    }
    [TestClass]
    public class UnitTest_OAuth2_Grants
    {
        public class ClientCredentialsResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }

        }

        public class ArbitraryResourceOwnerResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
            public string refresh_token { get; set; }

        }
        public static ITestServerFixture _fixture;

        [ClassInitialize]
        public static void IntializeClass(TestContext testContext)
        {
            _fixture = TestServerContainer.TestServerFixture;
            
        }
        [TestMethod]
        public async Task fail_client_credentials()
        {
            var client = _fixture.Client;

            var dict = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
                {"client_id", "b2b-client"},
                {"client_secret", "bad"}
            };



            var req = new HttpRequestMessage(HttpMethod.Post, "connect/token")
            {
                Content = new FormUrlEncodedContent(dict)
            };
            var response = await client.SendAsync(req);
            response.StatusCode.ShouldNotBe(System.Net.HttpStatusCode.OK);

        }

        [TestMethod]
        public async Task success_client_credentials()
        {
            var client = _fixture.Client;

            var dict = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
                {"client_id", "b2b-client"},
                {"client_secret", "secret"}
            };



            var req = new HttpRequestMessage(HttpMethod.Post, "connect/token")
            {
                Content = new FormUrlEncodedContent(dict)
            };
            var response = await client.SendAsync(req);
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

            var jsonString = await response.Content.ReadAsStringAsync();
            jsonString.ShouldNotBeNullOrWhiteSpace();
            var clientCredentialsResponse = JsonConvert.DeserializeObject<ClientCredentialsResponse>(jsonString);


            clientCredentialsResponse.ShouldNotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(clientCredentialsResponse.access_token) as JwtSecurityToken;

            tokenS.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task success_arbitrary_resource_owner()
        {
            var client = _fixture.Client;

            var dict = new Dictionary<string, string>
            {
                {"grant_type", "arbitrary_resource_owner"},
                {"client_id", "arbitrary-resource-owner-client"},
                {"client_secret", "secret"},
                {"scope", "offline_access wizard"},
                {"arbitrary_claims", "{\"top\":[\"TopDog\"]}"},
                {"subject", "BugsBunny"},
                {"access_token_lifetime", "3600"},
                {"arbitrary_amrs", "[\"A\",\"D\",\"C\"]"},
                {"arbitrary_audiences", "[\"cat\",\"dog\"]"},
                {
                    "custom_payload",
                    "{\"some_string\": \"data\",\"some_number\": 1234,\"some_object\": { \"some_string\": \"data\",\"some_number\": 1234},\"some_array\": [{\"a\": \"b\"},{\"b\": \"c\"}]}"
                }
            };



            var req = new HttpRequestMessage(HttpMethod.Post, "connect/token")
            {
                Content = new FormUrlEncodedContent(dict)
            };
            var response = await client.SendAsync(req);
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

            var jsonString = await response.Content.ReadAsStringAsync();
            jsonString.ShouldNotBeNullOrWhiteSpace();
            var arbitraryResourceOwnerResponse =
                JsonConvert.DeserializeObject<ArbitraryResourceOwnerResponse>(jsonString);


            arbitraryResourceOwnerResponse.ShouldNotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(arbitraryResourceOwnerResponse.access_token) as JwtSecurityToken;

            tokenS.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task create_unsigned_jwt()
        {
            var header = new JwtHeader();
            var payload = new JwtPayload
            {
                { "some ", "hello "},
                { "scope", "http://dummy.com/"},
            };
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            // Token to String so you can use it in your client
            var tokenString = handler.WriteToken(secToken);
            // And finally when  you received token from client
            // you can  either validate it or try to  read
            var token = handler.ReadJwtToken(tokenString);

            var d = new JwtSecurityToken(header, token.Payload);
            tokenString = handler.WriteToken(secToken);
        }
    }
}
