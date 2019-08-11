using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace GrpcJsonTranscoder.IntegrationTests
{
    public class GreetServiceTest
    {
        [Fact]
        public async Task CanCallToGreetEndPoint()
        {
            const string webApiBaseUrl = "http://aggregation_service:5001";
            //const string webApiBaseUrl = "http://localhost:5001";
            using (var client = new HttpClient())
            {
                var result = await client.GetStringAsync($"{webApiBaseUrl}/weatherforecast");
                Assert.NotNull(result);
            }
        }
    }
}
