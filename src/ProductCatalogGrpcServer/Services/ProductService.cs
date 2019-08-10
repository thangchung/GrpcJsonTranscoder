using Grpc.Core;
using GrpcShared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductCatalogGrpcServer.Services
{
    public class ProductService : Product.ProductBase
    {
        private readonly ILogger<ProductService> _logger;
        public ProductService(ILogger<ProductService> logger)
        {
            _logger = logger;
        }

        public override async Task<GetProductsReply> GetProducts(GetProductsRequest request, ServerCallContext context)
        {
            var products = new List<ProductDto>();

            for (int i = 1; i <= 5; i++)
            {
                products.Add(new ProductDto
                {
                    Id = i,
                    Name = $"product {i}",
                    Quantity = new Random().Next(100),
                    Description = $"description of product {i}"
                });
            }

            var reply = new GetProductsReply();
            reply.Products.AddRange(products);
            return await Task.FromResult(reply);
        }

        public override async Task<CreateProductReply> CreateProduct(CreateProductRequest request, ServerCallContext context)
        {
            return await Task.FromResult(new CreateProductReply
            {
                Product = new ProductDto
                {
                    Id = new Random().Next(1000),
                    Name = $"{request.Name} created",
                    Quantity = request.Quantity,
                    Description = $"{request.Description} created"
                }
            });
        }
    }
}
