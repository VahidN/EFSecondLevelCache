using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EFSecondLevelCache.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.FunctionalDbFirstTests
{
    [TestClass]
    public class UnitTests
    {
        [TestInitialize]
        public void ClearEFGlobalCacheBeforeEachTest()
        {
            new EFCacheServiceProvider().ClearAllCachedEntries();
        }

        [TestMethod]
        public void TestIncludeMethodAffectsKeyCache()
        {
            using (var context = new TestDB2015Entities())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("a normal query");
                var product1IncludeTags = context.Products.Include(x => x.Tags).FirstOrDefault();
                Assert.IsNotNull(product1IncludeTags);


                Trace.WriteLine("1st query using Include method.");
                databaseLog.Clear();
                var debugInfo1 = new EFCacheDebugInfo();
                var firstPoductIncludeTags = context.Products.Include(x => x.Tags)
                                                             .Cacheable(debugInfo1)
                                                             .FirstOrDefault();
                Assert.IsNotNull(firstPoductIncludeTags);
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                var hash1 = debugInfo1.EFCacheKey.KeyHash;
                var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;

                Trace.WriteLine(
                    @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var firstPoduct = context.Products.Cacheable(debugInfo2)
                                                  .FirstOrDefault();
                Assert.IsNotNull(firstPoduct);
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                var hash2 = debugInfo2.EFCacheKey.KeyHash;
                var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                Assert.AreNotEqual(hash1, hash2);
                Assert.AreNotEqual(cacheDependencies1, cacheDependencies2);
            }
        }

        [TestMethod]
        public void TestEagerlyLoadingMultipleLevels()
        {
            using (var context = new TestDB2015Entities())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("a normal query");
                var product1IncludeTags = context.Users
                                                 .Include(x => x.Products)
                                                 .Include(x => x.Products.Select(y => y.Tags))
                                                 .FirstOrDefault();
                Assert.IsNotNull(product1IncludeTags);


                Trace.WriteLine("1st query using Include method.");
                databaseLog.Clear();
                var debugInfo1 = new EFCacheDebugInfo();
                var firstPoductIncludeTags = context.Users
                                                    .Include(x => x.Products)
                                                    .Include(x => x.Products.Select(y => y.Tags))
                                                    .Cacheable(debugInfo1)
                                                    .FirstOrDefault();
                Assert.IsNotNull(firstPoductIncludeTags);
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                var hash1 = debugInfo1.EFCacheKey.KeyHash;
                var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;


                Trace.WriteLine("same cached query using Include method.");
                databaseLog.Clear();
                var debugInfo11 = new EFCacheDebugInfo();
                var firstPoductIncludeTags11 = context.Users
                                                    .Include(x => x.Products)
                                                    .Include(x => x.Products.Select(y => y.Tags))
                                                    .Cacheable(debugInfo11)
                                                    .FirstOrDefault();
                Assert.IsNotNull(firstPoductIncludeTags11);
                Assert.AreEqual(true, debugInfo11.IsCacheHit);


                Trace.WriteLine(
                    @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var firstPoduct = context.Users.Cacheable(debugInfo2)
                                               .FirstOrDefault();
                Assert.IsNotNull(firstPoduct);
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                var hash2 = debugInfo2.EFCacheKey.KeyHash;
                var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                Assert.AreNotEqual(hash1, hash2);
                Assert.AreNotEqual(cacheDependencies1, cacheDependencies2);
            }
        }
    }
}