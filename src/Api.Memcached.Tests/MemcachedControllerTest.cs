using AarekhLabs.Memcached.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace Api.Tests
{
    /// <summary>
    /// MemcachedController tests.
    /// </summary>
    public class MemcachedControllerTest
    {

        public MemcachedControllerTest()
        {
        }

        [Fact]
        public async Task SetKeyWithStringValueTest()
        {
            var response = await GetResponse("/api/memcached/set/key1?value=valueforkey1", "post", null, "application/json");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetKeyWithStringValueTest()
        {
            var response = await GetResponse("/api/memcached/set/key2?value=StringValueForKey2", "post", null, "application/json");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await GetResponse("/api/memcached/get/key2", "get", null, "application/json");
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"StringValueForKey2\"", responseString);
        }

        [Fact]
        public async Task DeleteKeyTest()
        {
            var response = await GetResponse("/api/memcached/set/key3?value=TestValue3", "post", null, "application/json");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await GetResponse("/api/memcached/delete/key3", "delete", null, "application/json");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            response = await GetResponse("/api/memcached/get/key3", "get", null, "application/json", false);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private async Task<HttpResponseMessage> GetResponse(string uriPath, string method, HttpContent body, string contentType, bool expectSuccess=true)
        {
            HttpClient _client = new HttpClient();
            uriPath = "http://localhost:8263" + uriPath;

            HttpResponseMessage response=null;
            if (string.IsNullOrEmpty(contentType))
                contentType = "application/json";

            if (body!=null)
                body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
          
            
            switch (method)
            {
                case "get":
                    response = await _client.GetAsync(uriPath);
                    break;
                case "post":
                    response = await _client.PostAsync(uriPath, body);
                    break;
                case "put":
                    response = await _client.PutAsync(uriPath, body);
                    break;
                case "delete":
                    response = await _client.DeleteAsync(uriPath);
                    break;
            }
            
            if(expectSuccess)
                response.EnsureSuccessStatusCode();

            return response;
        }
    }
}
