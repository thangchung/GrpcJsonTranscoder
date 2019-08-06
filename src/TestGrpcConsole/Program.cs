using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Reflection.V1Alpha;
using GrpcShared;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using static Grpc.Reflection.V1Alpha.ServerReflection;

namespace TestGrpcConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var httpClient = new HttpClient();
            // The port number(5001) must match the port of the gRPC server.
            httpClient.BaseAddress = new Uri("http://localhost:5001");
            var client = GrpcClient.Create<Greeter.GreeterClient>(httpClient);
            var reply = await client.SayHelloAsync(
                              new HelloRequest { Name = "GreeterClient" });
            Console.WriteLine("Greeting: " + reply.Message);

            var channelBuilder = ChannelBuilder.ForHttpClient(httpClient);
            //var channel = new Channel("localhost:5001", ChannelCredentials.Insecure);
            var client1 = new ServerReflectionClient(channel);
            var response = await SingleRequestAsync(client1, new ServerReflectionRequest());

            var services = response.ListServicesResponse;

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task<ServerReflectionResponse> SingleRequestAsync(ServerReflectionClient client, ServerReflectionRequest request)
        {
            var call = client.ServerReflectionInfo();
            await call.RequestStream.WriteAsync(request);
            var response = call.ResponseStream.Current;
            await call.RequestStream.CompleteAsync();
            return response;
        }
    }
}
