using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic; // https://github.com/NArnott/System.Linq.Dynamic
using System.Text;
using System.Threading.Tasks;
using EFSecondLevelCache.Contracts;
using EFSecondLevelCache.TestDataLayer.DataLayer;
using EFSecondLevelCache.TestDataLayer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.FunctionalTests
{
    [DynamicLinqType]
    public class ProductInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
    }

    [TestClass]
    public class DynamicLINQTests
    {
        [TestInitialize]
        public void ClearEFGlobalCacheBeforeEachTest()
        {
            new EFCacheServiceProvider().ClearAllCachedEntries();
        }

        [TestMethod]
        public void TestDynamicLINQWorksUsingProjections()
        {
            using (var context = new SampleContext())
            {
                var isActive = true;
                var name = "Product1";

                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };


                Trace.WriteLine("Projection 1");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   .Select("ProductId")
                                   .Cast<int>()
                                   .Cacheable(debugInfo2)
                                   .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());

                Trace.WriteLine("Projection 2");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                list2 = context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   .Select("ProductId")
                                   .Cast<int>()
                                   .Cacheable(debugInfo3)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list2.Any());
            }
        }

        [TestMethod]
        public void TestDynamicLINQWorksUsingFirstOrDefault()
        {
            using (var context = new SampleContext())
            {
                var isActive = true;
                var name = "Product1";

                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };


                Trace.WriteLine("Projection 1");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var id1 = context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   .Cast<Product>()
                                   .Cacheable(debugInfo2)
                                   .FirstOrDefault();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsNotNull(id1);

                Trace.WriteLine("Projection 2");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                id1 = context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   .Cast<Product>()
                                   .Cacheable(debugInfo3)
                                   .FirstOrDefault();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsNotNull(id1);
            }
        }

        [TestMethod]
        public async Task TestDynamicLINQWorksUsingAsyncProjections()
        {
            using (var context = new SampleContext())
            {
                var isActive = true;
                var name = "Product1";

                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };


                Trace.WriteLine("Projection 1");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = await context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   .Select("ProductId")
                                   .Cast<int>()
                                   .Cacheable(debugInfo2)
                                   .ToListAsync();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());

                Trace.WriteLine("Projection 2");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                list2 = await context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   .Select("ProductId")
                                   .Cast<int>()
                                   .Cacheable(debugInfo3)
                                   .ToListAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list2.Any());
            }
        }


        [TestMethod]
        public async Task TestDynamicLINQWorksUsingAsyncAnonymousProjections()
        {
            using (var context = new SampleContext())
            {
                var isActive = true;
                var name = "Product1";

                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };


                Trace.WriteLine("Projection 1");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = await context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   //.Select("new (ProductId, ProductName) as ProductInfo")
                                   .Select("new (ProductInfo.ProductId, ProductInfo.ProductName)")
                                   .Cast<ProductInfo>()//todo: use dynamic type here .....!!
                                   .Cacheable(debugInfo2)
                                   .ToListAsync();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());

                Trace.WriteLine("Projection 2");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                list2 = await context.Products
                                   .OrderBy("ProductNumber")
                                   .Where("IsActive = @0 and ProductName = @1", isActive, name)
                                   //.Select("new (ProductId, ProductName) as ProductInfo")
                                   .Select("new (ProductInfo.ProductId, ProductInfo.ProductName)")
                                   .Cast<ProductInfo>()
                                   .Cacheable(debugInfo3)
                                   .ToListAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list2.Any());
            }
        }

    }
}