using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EFSecondLevelCache.TestDataLayer.DataLayer;
using EFSecondLevelCache.TestDataLayer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure;

namespace EFSecondLevelCache.MockingTests
{
    [TestClass]
    public class MockingTests
    {
        // more info: http://msdn.microsoft.com/en-us/data/dn314429

        [TestMethod]
        public void TestCacheableGetAllProductsSync()
        {
            var data = new List<Product>
            {
                new Product { ProductName = "BBB"},
                new Product { ProductName = "ZZZ" },
                new Product { ProductName = "AAA" }
            }.AsQueryable();

            var mockSet = new Mock<DbSet<Product>>();
            mockSet.As<IQueryable<Product>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<Product>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<Product>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<Product>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var mockContext = new Mock<SampleContext>();
            mockContext.Setup(c => c.Products).Returns(mockSet.Object);

            // public List<Product> GetAllProductsOrderedByName()
            var products = mockContext.Object.Products
                                             .OrderBy(product => product.ProductName)
                                             .Cacheable()
                                             .ToList();

            Assert.AreEqual(3, products.Count);
            Assert.AreEqual("AAA", products[0].ProductName);
            Assert.AreEqual("BBB", products[1].ProductName);
            Assert.AreEqual("ZZZ", products[2].ProductName);
        }


        [TestMethod]
        public async Task TestCacheableGetAllProductsAsync()
        {
            var data = new List<Product>
            {
                new Product { ProductName = "BBB"},
                new Product { ProductName = "ZZZ" },
                new Product { ProductName = "AAA" }
            }.AsQueryable();

            var mockSet = new Mock<DbSet<Product>>();
            mockSet.As<IDbAsyncEnumerable<Product>>()
                .Setup(m => m.GetAsyncEnumerator())
                .Returns(new EFAsyncEnumerator<Product>(data.GetEnumerator()));

            mockSet.As<IQueryable<Product>>()
                .Setup(m => m.Provider)
                .Returns(new TestDbAsyncQueryProvider<Product>(data.Provider));

            mockSet.As<IQueryable<Product>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<Product>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<Product>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var mockContext = new Mock<SampleContext>();
            mockContext.Setup(c => c.Products).Returns(mockSet.Object);

            // public Task<List<Product>> GetAllProductsOrderedByNameAsync()
            var products = await mockContext.Object.Products
                                             .OrderBy(product => product.ProductName)
                                             .Cacheable()
                                             .ToListAsync();

            Assert.AreEqual(3, products.Count);
            Assert.AreEqual("AAA", products[0].ProductName);
            Assert.AreEqual("BBB", products[1].ProductName);
            Assert.AreEqual("ZZZ", products[2].ProductName);
        }
    }
}