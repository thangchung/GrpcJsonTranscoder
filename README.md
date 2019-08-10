# Grpc-Json Transcoder project

[![Price](https://img.shields.io/badge/price-FREE-0098f7.svg)](https://github.com/thangchung/GrpcJsonTranscoder/blob/master/LICENSE)

This is a filter that allows a RESTful JSON API client (Ocelot Gateway) to send requests to .NET Web API (Aggregation Service) over HTTP and get proxied to a gRPC service (on behind).

This project is inspired by [grpc-gateway](https://github.com/grpc-ecosystem/grpc-gateway) which is totally for golang, [grpc-dynamic-gateway](https://github.com/konsumer/grpc-dynamic-gateway) is for nodejs. And especially, [Envoy gRPC-JSON transcoder](https://www.envoyproxy.io/docs/envoy/latest/configuration/http_filters/grpc_json_transcoder_filter) is the best of transcode in this area, but it is only on the infrastructure level. You also can use it just like my project used at [https://github.com/vietnam-devs/coolstore-microservices/tree/master](https://github.com/vietnam-devs/coolstore-microservices/blob/master/deploys/dockers/envoy-proxy/envoy.yaml).

## Give a Star!

If you liked [`GrpcJsonTranscoder`](https://github.com/thangchung/GrpcJsonTranscoder) project or if it helped you, please give a star :star: for this repository. That will not only help strengthen our .NET community but also improve cloud-native apps development skills for .NET developers in around the world. Thank you very much :+1:

Check out my [blog](https://medium.com/@thangchung) or say hi on [Twitter](https://twitter.com/thangchung)!

## How to run it!


```bash
$ docker-compose up # I haven't done it yet :p
```

or 

```bash
$ bash
$ start.sh # I haven't done it yet :p
```

## How to understand it!

The project aim is for .NET community and its ecosystem which leverage the power of [Ocelot Gateway](https://github.com/ThreeMammals/Ocelot) which is very power in the gateway components were used by varous of companies and sample source code when we try to adopt the microservices architecture project.

![](assets/overview.png)

- OcelotGateway (.NET Core 2.2): http://localhost:5000
- AggregationRestApi (.NET Core 3.0): http://localhost:5001
- ProductCatalogGrpcServer (.NET Core 3.0): http://localhost:5002
- GreatGrpcServer (.NET Core 3.0): http://localhost:5003

We will normally use Ocelot configuration for the transcode process, the main parser and transformation processes are only happen at aggregation service level so that you will easy to upgrade Ocelot in case we need, but not effect to the grpc-json transcode seats in the aggregation service. 

```json
// ocelot.json
{
  "ReRoutes": [
    {
      "UpstreamPathTemplate": "/say/{name}",
      "UpstreamHttpMethod": [ "Get" ],
      "DownstreamPathTemplate": "/Greet.Greeter/SayHello",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5001
        }
      ]
    },
    {
      "UpstreamPathTemplate": "/products",
      "UpstreamHttpMethod": [ "Get" ],
      "DownstreamPathTemplate": "/ProductCatalog.Product/GetProducts",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5001
        }
      ]
    },
    {
      "UpstreamPathTemplate": "/products",
      "UpstreamHttpMethod": [ "Post" ],
      "DownstreamPathTemplate": "/ProductCatalog.Product/CreateProduct",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5001
        }
      ]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

and,

```csharp
// Program.cs
var configuration = new OcelotPipelineConfiguration
{
    PreQueryStringBuilderMiddleware = async (ctx, next) =>
    {
        var routes = ctx.TemplatePlaceholderNameAndValues;
        ctx.DownstreamRequest.Headers.Add(
            "x-grpc-routes",
            JsonConvert.SerializeObject(routes.Select(x => new NameAndValue { Name = x.Name, Value = x.Value })));
        await next.Invoke();
    }
};

app.UseOcelot(configuration).Wait();
```

More at https://github.com/thangchung/GrpcJsonTranscoder/tree/master/src/OcelotGateway

Then we only put some of json configuration into appsettings.json inside aggregation service to point it to other gRPC services we need.

```json
// appsettings.json
"GrpcJsonTranscoder": {
  "GrpcMappers": [
    {
      "GrpcMethod": "/Greet.Greeter/SayHello",
      "GrpcHost": "127.0.0.1:5003"
    },
    {
      "GrpcMethod": "/ProductCatalog.Product/GetProducts",
      "GrpcHost": "127.0.0.1:5002"
    },
    {
      "GrpcMethod": "/ProductCatalog.Product/CreateProduct",
      "GrpcHost": "127.0.0.1:5002"
    }
  ]
}
```

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    ...

    services.AddGrpcJsonTranscoder(() => new GrpcAssemblyResolver().ConfigGrpcAssembly(typeof(Greeter.GreeterClient).Assembly));
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...

    app.UseGrpcJsonTranscoder();
    
    ...
}
```

More at https://github.com/thangchung/GrpcJsonTranscoder/tree/master/src/AggregationRestApi

### **Don't believe what I said. Try it!**

> We haven't tested it with stream and duplex transport protocols yet. So we feel free to contribute by community.

## Contributing

1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :p