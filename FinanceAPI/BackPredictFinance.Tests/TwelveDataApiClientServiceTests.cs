//using BackPredictFinance.Common.enums;
//using BackPredictFinance.Datas.Context;
//using BackPredictFinance.Datas.Models;
//using BackPredictFinance.Services.TwelveDataServices;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Moq;
//using Moq.Protected;
//using System.Net;
//using System.Text;

//namespace TestProject1
//{
//    public class TwelveDataApiClientServiceTests
//    {
//        [Fact]
//        public async Task GetTimeSeriesAsync_ReturnsDeserializedResponse()
//        {
//            // Arrange
//            var handlerMock = new Mock<HttpMessageHandler>();
//            var json = "{\"status\":\"ok\",\"symbol\":\"BTC\",\"values\":[{\"datetime\":\"2025-07-01 00:00:00\",\"open\":\"30000\",\"high\":\"31000\",\"low\":\"29000\",\"close\":\"30500\",\"volume\":\"1234\"}]}";
//            handlerMock
//               .Protected()
//               .Setup<Task<HttpResponseMessage>>("SendAsync",
//                   ItExpr.IsAny<HttpRequestMessage>(),
//                   ItExpr.IsAny<CancellationToken>())
//               .ReturnsAsync(new HttpResponseMessage
//               {
//                   StatusCode = HttpStatusCode.OK,
//                   Content = new StringContent(json, Encoding.UTF8, "application/json")
//               });
//            var client = new HttpClient(handlerMock.Object)
//            {
//                BaseAddress = new Uri("https://api.twelvedata.com/")
//            };
//            var config = new ConfigurationBuilder()
//                .AddInMemoryCollection(new Dictionary<string, string> { { "TwelveData:ApiKey", "TESTKEY" } })
//                .Build();
//            var service = new TwelveDataApiClientService(null, client, config);

//            // Act
//            var result = await service.GetTimeSeriesAsync("BTC");

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("ok", result.Status);
//            Assert.Equal("BTC", result.Symbol);
//            Assert.Single(result.Values);
//            Assert.Equal("30000", result.Values[0].Open);
//        }

//        [Fact]
//        public async Task GetLatestPriceAsync_ReturnsDecimalValue()
//        {
//            // Arrange
//            var handlerMock = new Mock<HttpMessageHandler>();
//            var json = "{\"price\":\"30500\"}";
//            handlerMock
//               .Protected()
//               .Setup<Task<HttpResponseMessage>>("SendAsync",
//                   ItExpr.IsAny<HttpRequestMessage>(),
//                   ItExpr.IsAny<CancellationToken>())
//               .ReturnsAsync(new HttpResponseMessage
//               {
//                   StatusCode = HttpStatusCode.OK,
//                   Content = new StringContent(json, Encoding.UTF8, "application/json")
//               });
//            var client = new HttpClient(handlerMock.Object)
//            {
//                BaseAddress = new Uri("https://api.twelvedata.com/")
//            };
//            var config = new ConfigurationBuilder()
//                .AddInMemoryCollection(new Dictionary<string, string> { { "TwelveData:ApiKey", "TESTKEY" } })
//                .Build();
//            var service = new TwelveDataApiClientService(null, client, config);

//            // Act
//            var price = await service.GetLatestPriceAsync("BTC");

//            // Assert
//            Assert.NotNull(price);
//            Assert.Equal(30500m, price.Value);
//        }
//    }

//    public class TwelveDataAssetServiceTests
//    {
//        [Fact]
//        public async Task GetOrCreateAsync_CreatesAndRetrievesAsset()
//        {
//            // Arrange: In-memory EF Core context
//            var options = new DbContextOptionsBuilder<FinanceDbContext>()
//                .UseInMemoryDatabase("AssetDb_CreateRetrieve")
//                .Options;
//            var context = new FinanceDbContext(options, null);
//            var sp = new ServiceCollection()
//                .AddSingleton(context)
//                .BuildServiceProvider();
//            var service = new TwelveDataAssetService(sp);

//            // Act: Create new
//            var created = await service.GetOrCreateAsync("AAPL", "Apple Inc.", AssetTypeEnum.Stock);
//            // Retrieve existing
//            var retrieved = await service.GetOrCreateAsync("AAPL");

//            // Assert
//            Assert.NotNull(created);
//            Assert.Equal("AAPL", created.Symbol);
//            Assert.Equal("Apple Inc.", created.Name);
//            Assert.Same(created, retrieved);
//        }
//    }

//    public class TwelveDataPriceUpdateServiceTests
//    {
//        [Fact]
//        public async Task UpdateLatestAsync_RecordsPriceHistory()
//        {
//            // Arrange: In-memory DB and mocks
//            var options = new DbContextOptionsBuilder<FinanceDbContext>()
//                .UseInMemoryDatabase("PriceHistory_Test")
//                .Options;
//            var context = new FinanceDbContext(options, null);
//            var sp = new ServiceCollection()
//                .AddSingleton(context)
//                .BuildServiceProvider();

//            var mockAssetService = new Mock<ITwelveDataAssetService>();
//            var asset = new Asset { Symbol = "BTC" };
//            mockAssetService.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), null, It.IsAny<AssetTypeEnum>())).ReturnsAsync(asset);

//            var mockApi = new Mock<ITwelveDataApiClientService>();
//            mockApi.Setup(x => x.GetLatestPriceAsync("BTC")).ReturnsAsync(45000m);

//            var service = new TwelveDataPriceUpdateService(sp, mockApi.Object, mockAssetService.Object);

//            // Act
//            await service.UpdateLatestAsync("BTC");

//            // Assert
//            var histories = await context.PriceHistories.ToListAsync();
//            Assert.Single(histories);
//            Assert.Equal(45000m, histories[0].Price);
//        }
//    }
//}