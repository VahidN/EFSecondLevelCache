using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFSecondLevelCache.Contracts;
using EFSecondLevelCache.TestDataLayer.DataLayer;
using EFSecondLevelCache.TestDataLayer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.FunctionalTests
{
    [TestClass]
    public class EFCachedQueryProviderTests
    {
        [TestInitialize]
        public void ClearEFGlobalCacheBeforeEachTest()
        {
            new EFCacheServiceProvider().ClearAllCachedEntries();
        }

        [TestMethod]
        public void TestIncludeMethodAffectsKeyCache()
        {
            using (var context = new SampleContext())
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
                var firstProductIncludeTags = context.Products.Include(x => x.Tags)
                                                             .Cacheable(debugInfo1)
                                                             .FirstOrDefault();
                Assert.IsNotNull(firstProductIncludeTags);
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                var hash1 = debugInfo1.EFCacheKey.KeyHash;
                var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;

                Trace.WriteLine(
                    @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var firstProduct = context.Products.Cacheable(debugInfo2)
                                                  .FirstOrDefault();
                Assert.IsNotNull(firstProduct);
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                var hash2 = debugInfo2.EFCacheKey.KeyHash;
                var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                Assert.AreNotEqual(hash1, hash2);
                Assert.AreNotEqual(cacheDependencies1, cacheDependencies2);
            }
        }

        [TestMethod]
        public void TestInsertingDataIntoTheSameTableShouldInvalidateTheCacheAutomatically()
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

                Trace.WriteLine("1st query, reading from db");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo1)
                    .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());


                Trace.WriteLine("same query, reading from 2nd level cache");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo2)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());


                Trace.WriteLine("inserting data, invalidates the cache on SaveChanges");
                var rnd = new Random();
                var newProduct = new Product
                {
                    IsActive = false,
                    ProductName = "Product" + rnd.Next(),
                    ProductNumber = rnd.Next().ToString(),
                    Notes = "Notes ...",
                    UserId = 1
                };
                context.Products.Add(newProduct);
                context.SaveChanges();


                Trace.WriteLine("same query after insert, reading from database.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo3)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
            }
        }

        [TestMethod]
        public void TestInsertingDataToOtherTablesShouldNotInvalidateTheCacheDependencyAutomatically()
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

                Trace.WriteLine("1st query, reading from db (it dependes on/includes the Tags table)");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo1)
                    .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());


                Trace.WriteLine("same query, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo2)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());


                Trace.WriteLine(
                    "inserting data into a *non-related* table, shouldn't invalidate the cache on SaveChanges.");
                var rnd = new Random();
                var user = new User
                {
                    Name = "User " + rnd.Next()
                };
                context.Users.Add(user);
                context.SaveChanges();


                Trace.WriteLine("same query after insert, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo3)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
            }
        }

        [TestMethod]
        public void TestInsertingDataToRelatedTablesShouldInvalidateTheCacheDependencyAutomatically()
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

                Trace.WriteLine("1st query, reading from db (it dependes on/includes the Tags table).");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo1)
                    .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());


                Trace.WriteLine("same query, reading from 2nd level cache");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo2)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());


                Trace.WriteLine("inserting data into a *related* table, invalidates the cache on SaveChanges.");
                var rnd = new Random();
                var tag = new Tag
                {
                    Name = "Tag " + rnd.Next()
                };
                context.Tags.Add(tag);
                context.SaveChanges();


                Trace.WriteLine(
                    "same query after insert, reading from database (it dependes on/includes the Tags table)");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo3)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
            }
        }

        [TestMethod]
        public void TestQueriesUsingDifferentParameterValuesWillNotUseTheCache()
        {
            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("1st query, reading from db.");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive && product.ProductName == "Product1")
                    .Cacheable(debugInfo1)
                    .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsNotNull(list1);

                Trace.WriteLine("2nd query, reading from db.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == false && product.ProductName == "Product1")
                    .Cacheable(debugInfo2)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsNotNull(list2);

                Trace.WriteLine("third query, reading from db.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == false && product.ProductName == "Product2")
                    .Cacheable(debugInfo3)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo3.IsCacheHit);
                Assert.IsNotNull(list3);

                Trace.WriteLine("4th query, same as third one, reading from cache.");
                databaseLog.Clear();
                var debugInfo4 = new EFCacheDebugInfo();
                var list4 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == false && product.ProductName == "Product2")
                    .Cacheable(debugInfo4)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo4.IsCacheHit);
                Assert.IsNotNull(list4);
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheCreatesTheCommandTreeAfterCallingTheSameNormalQuery()
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

                Trace.WriteLine("1st normal query, reading from db.");
                var list1 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.IsTrue(list1.Any());


                Trace.WriteLine("same query as Cacheable, reading from db.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo2)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());
                var hash2 = debugInfo2.EFCacheKey.KeyHash;


                Trace.WriteLine("same query, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
                var hash3 = debugInfo3.EFCacheKey.KeyHash;

                Assert.AreEqual(hash2, hash3);
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheDoesNotHitTheDatabase()
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

                Trace.WriteLine("1st query, reading from db.");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo1)
                                   .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());
                var hash1 = debugInfo1.EFCacheKey.KeyHash;


                Trace.WriteLine("same query, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo2)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());
                var hash2 = debugInfo2.EFCacheKey.KeyHash;


                Trace.WriteLine("same query, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
                var hash3 = debugInfo3.EFCacheKey.KeyHash;

                Assert.AreEqual(hash1, hash2);
                Assert.AreEqual(hash2, hash3);

                Trace.WriteLine("different query, reading from db.");
                databaseLog.Clear();
                var debugInfo4 = new EFCacheDebugInfo();
                var list4 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                   .Cacheable(debugInfo4)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo4.IsCacheHit);
                Assert.IsTrue(list4.Any());

                var hash4 = debugInfo4.EFCacheKey.KeyHash;
                Assert.AreNotSame(hash3, hash4);
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheInTwoDifferentContextsDoesNotHitTheDatabase()
        {
            var isActive = true;
            var name = "Product1";
            string hash2;
            string hash3;

            Trace.WriteLine("context 1.");
            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("1st query as Cacheable, reading from db.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo2)
                    .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());
                hash2 = debugInfo2.EFCacheKey.KeyHash;
            }

            Trace.WriteLine("context 2");
            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("same query, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
                hash3 = debugInfo3.EFCacheKey.KeyHash;
            }

            Assert.AreEqual(hash2, hash3);
        }


        [TestMethod]
        public void TestSecondLevelCacheInTwoDifferentParallelContexts()
        {
            var isActive = true;
            var name = "Product1";
            var debugInfo2 = new EFCacheDebugInfo();
            var debugInfo3 = new EFCacheDebugInfo();

            var task1 = Task.Factory.StartNew(() =>
            {
                Trace.WriteLine("context 1.");
                using (var context = new SampleContext())
                {
                    Trace.WriteLine("1st query as Cacheable.");
                    var list2 = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo2)
                        .ToList();
                    Assert.IsTrue(list2.Any());
                }
            });

            var task2 = Task.Factory.StartNew(() =>
            {
                Trace.WriteLine("context 2");
                using (var context = new SampleContext())
                {
                    Trace.WriteLine("same query");
                    var list3 = context.Products
                        .OrderBy(product => product.ProductNumber)
                        .Where(product => product.IsActive == isActive && product.ProductName == name)
                        .Cacheable(debugInfo3)
                        .ToList();
                    Assert.IsTrue(list3.Any());
                }
            });

            Task.WaitAll(task1, task2);

            Assert.AreEqual(debugInfo2.EFCacheKey.KeyHash, debugInfo3.EFCacheKey.KeyHash);
        }

        [TestMethod]
        public async Task TestSecondLevelCacheUsingAsyncMethodsDoesNotHitTheDatabase()
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

                Trace.WriteLine("1st async query, reading from db");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo1)
                                   .ToListAsync();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());
                var hash1 = debugInfo1.EFCacheKey.KeyHash;


                Trace.WriteLine("same async query, reading from 2nd level cache");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo2)
                                   .ToListAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());
                var hash2 = debugInfo2.EFCacheKey.KeyHash;


                Trace.WriteLine("same async query, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .ToListAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
                var hash3 = debugInfo3.EFCacheKey.KeyHash;

                Assert.AreEqual(hash1, hash2);
                Assert.AreEqual(hash2, hash3);

                Trace.WriteLine("different async query, reading from db.");
                databaseLog.Clear();
                var debugInfo4 = new EFCacheDebugInfo();
                var list4 = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                   .Cacheable(debugInfo4)
                                   .ToListAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo4.IsCacheHit);
                Assert.IsTrue(list4.Any());

                var hash4 = debugInfo4.EFCacheKey.KeyHash;
                Assert.AreNotSame(hash3, hash4);

                Trace.WriteLine("different async query, reading from db.");
                databaseLog.Clear();
                var debugInfo5 = new EFCacheDebugInfo();
                var product1 = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                   .Cacheable(debugInfo5)
                                   .FirstOrDefaultAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo5.IsCacheHit);
                Assert.IsNotNull(product1);

                var hash5 = debugInfo5.EFCacheKey.KeyHash;
                Assert.AreNotSame(hash4, hash5);
            }
        }

        [TestMethod]
        public void TestTransactionRollbackShouldNotInvalidateTheCacheDependencyAutomatically()
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

                Trace.WriteLine("1st query, reading from db.");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo1)
                    .ToList();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());


                Trace.WriteLine("same query, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var list2 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo2)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo2.IsCacheHit);
                Assert.IsTrue(list2.Any());

                Trace.WriteLine(
                    "inserting data with transaction.Rollback, shouldn't invalidate the cache on SaveChanges.");
                try
                {
                    var rnd = new Random();
                    var newProduct = new Product
                    {
                        IsActive = false,
                        ProductName = "Product1", // It has an `IsUnique` constraint.
                        ProductNumber = rnd.Next().ToString(),
                        Notes = "Notes ...",
                        UserId = 1
                    };
                    context.Products.Add(newProduct);
                    context.SaveChanges(); // it uses a transaction behind the scene.
                }
                catch (Exception ex)
                {
                    // ProductName is duplicate here and should throw an exception on save changes
                    // and rollback the transaction automatically.
                    Trace.WriteLine(ex.ToString());
                }

                Trace.WriteLine("same query after insert, reading from 2nd level cache.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var list3 = context.Products.Include(x => x.Tags)
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == name)
                    .Cacheable(debugInfo3)
                    .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list3.Any());
            }
        }

        [TestMethod]
        public async Task TestSecondLevelCacheUsingDifferentAsyncMethods()
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

                Trace.WriteLine("ToListAsync");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo1)
                                   .ToListAsync();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());


                Trace.WriteLine("CountAsync");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var count = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo2)
                                   .CountAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(count > 0);


                Trace.WriteLine("FirstOrDefaultAsync");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var product1 = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .FirstOrDefaultAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo3.IsCacheHit);
                Assert.IsTrue(product1 != null);


                Trace.WriteLine("AnyAsync");
                databaseLog.Clear();
                var debugInfo4 = new EFCacheDebugInfo();
                var any = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                   .Cacheable(debugInfo4)
                                   .AnyAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo4.IsCacheHit);
                Assert.IsTrue(any);


                Trace.WriteLine("SumAsync");
                databaseLog.Clear();
                var debugInfo5 = new EFCacheDebugInfo();
                var sum = await context.Products
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                    .Cacheable(debugInfo5)
                    .SumAsync(x => x.ProductId);

                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo5.IsCacheHit);
                Assert.IsTrue(sum > 0);
            }
        }


        [TestMethod]
        public void TestSecondLevelCacheUsingDifferentSyncMethods()
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


                Trace.WriteLine("Count");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var count = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo2)
                                   .Count();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(count > 0);


                Trace.WriteLine("ToList");
                var debugInfo1 = new EFCacheDebugInfo();
                var list1 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo1)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                Assert.IsTrue(list1.Any());


                Trace.WriteLine("FirstOrDefault");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var product1 = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .FirstOrDefault();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo3.IsCacheHit);
                Assert.IsTrue(product1 != null);


                Trace.WriteLine("Any");
                databaseLog.Clear();
                var debugInfo4 = new EFCacheDebugInfo();
                var any = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                                   .Cacheable(debugInfo4)
                                   .Any();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo4.IsCacheHit);
                Assert.IsTrue(any);


                Trace.WriteLine("Sum");
                databaseLog.Clear();
                var debugInfo5 = new EFCacheDebugInfo();
                var sum = context.Products
                    .OrderBy(product => product.ProductNumber)
                    .Where(product => product.IsActive == isActive && product.ProductName == "Product2")
                    .Cacheable(debugInfo5)
                    .Sum(x => x.ProductId);

                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo5.IsCacheHit);
                Assert.IsTrue(sum > 0);
            }
        }


        [TestMethod]
        public void TestSecondLevelCacheUsingTwoCountMethods()
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


                Trace.WriteLine("Count 1");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var count = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo2)
                                   .Count();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(count > 0);

                Trace.WriteLine("Count 2");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                count = context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .Count();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(count > 0);
            }
        }


        [TestMethod]
        public async Task TestSecondLevelCacheUsingTwoCountAsyncMethods()
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


                Trace.WriteLine("Count 1, From DB");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var count = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo2)
                                   .CountAsync();
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsTrue(count > 0);

                Trace.WriteLine("Count 2, Reading from cache");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                count = await context.Products
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Cacheable(debugInfo3)
                                   .CountAsync();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(count > 0);
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheUsingProjections()
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
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Select(x => x.ProductId)
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
                                   .OrderBy(product => product.ProductNumber)
                                   .Where(product => product.IsActive == isActive && product.ProductName == name)
                                   .Select(x => x.ProductId)
                                   .Cacheable(debugInfo3)
                                   .ToList();
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsTrue(list2.Any());
            }
        }


        [TestMethod]
        public void TestSecondLevelCacheUsingFiltersAfterCacheableMethod()
        {
            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };


                Trace.WriteLine("Filters After Cacheable Method 1.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var product1 = context.Products
                                   .Cacheable(debugInfo2)
                                   .FirstOrDefault(product => product.IsActive);
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                Assert.IsNotNull(product1);


                Trace.WriteLine("Filters After Cacheable Method 2, Same query.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                product1 = context.Products
                                   .Cacheable(debugInfo3)
                                   .FirstOrDefault(product => product.IsActive);
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo3.IsCacheHit);
                Assert.IsNotNull(product1);


                Trace.WriteLine("Filters After Cacheable Method 3, Different query.");
                databaseLog.Clear();
                var debugInfo4 = new EFCacheDebugInfo();
                product1 = context.Products
                                   .Cacheable(debugInfo4)
                                   .FirstOrDefault(product => !product.IsActive);
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo4.IsCacheHit);
                Assert.IsNotNull(product1);


                Trace.WriteLine("Filters After Cacheable Method 4, Different query.");
                databaseLog.Clear();
                var debugInfo5 = new EFCacheDebugInfo();
                product1 = context.Products
                                   .Cacheable(debugInfo5)
                                   .FirstOrDefault(product => product.ProductName == "Product1");
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo5.IsCacheHit);
                Assert.IsNotNull(product1);


                Trace.WriteLine("Filters After Cacheable Method 5, Different query.");
                databaseLog.Clear();
                var debugInfo6 = new EFCacheDebugInfo();
                product1 = context.Products
                                   .Cacheable(debugInfo6)
                                   .FirstOrDefault(product => product.Tags.Any(tag => tag.Id == 1));
                sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(false, debugInfo6.IsCacheHit);
                Assert.IsNotNull(product1);
            }
        }

        [TestMethod]
        public void TestEagerlyLoadingMultipleLevels()
        {
            using (var context = new SampleContext())
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
                var firstProductIncludeTags = context.Users
                                                    .Include(x => x.Products)
                                                    .Include(x => x.Products.Select(y => y.Tags))
                                                    .Cacheable(debugInfo1)
                                                    .FirstOrDefault();
                Assert.IsNotNull(firstProductIncludeTags);
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                var hash1 = debugInfo1.EFCacheKey.KeyHash;
                var cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;


                Trace.WriteLine("same cached query using Include method.");
                databaseLog.Clear();
                var debugInfo11 = new EFCacheDebugInfo();
                var firstProductIncludeTags11 = context.Users
                                                    .Include(x => x.Products)
                                                    .Include(x => x.Products.Select(y => y.Tags))
                                                    .Cacheable(debugInfo11)
                                                    .FirstOrDefault();
                Assert.IsNotNull(firstProductIncludeTags11);
                Assert.AreEqual(true, debugInfo11.IsCacheHit);


                Trace.WriteLine(
                    @"2nd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var firstProduct = context.Users.Cacheable(debugInfo2)
                                               .FirstOrDefault();
                Assert.IsNotNull(firstProduct);
                Assert.AreEqual(false, debugInfo2.IsCacheHit);
                var hash2 = debugInfo2.EFCacheKey.KeyHash;
                var cacheDependencies2 = debugInfo2.EFCacheKey.CacheDependencies;

                Assert.AreNotEqual(hash1, hash2);
                Assert.AreNotEqual(cacheDependencies1, cacheDependencies2);
            }
        }

        [TestMethod]
        public void TestIncludeMethodAndProjectionAffectsKeyCache()
        {
            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("a normal query");
                var product1IncludeTags = context.Products
                    .Include(x => x.Tags)
                    .Select(x => new { Name = x.ProductName, x.Tags }).OrderBy(x => x.Name)
                    .FirstOrDefault();
                Assert.IsNotNull(product1IncludeTags);
            }

            string hash1;
            string[] cacheDependencies1;
            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("1st Cacheable query using Include method, reading from db");
                databaseLog.Clear();
                var debugInfo1 = new EFCacheDebugInfo();
                var firstProductIncludeTags = context.Products
                    .Include(x => x.Tags)
                    .Select(x => new { Name = x.ProductName, x.Tags }).OrderBy(x => x.Name)
                    .Cacheable(debugInfo1)
                    .FirstOrDefault();
                Assert.IsNotNull(firstProductIncludeTags);
                Assert.AreEqual(false, debugInfo1.IsCacheHit);
                hash1 = debugInfo1.EFCacheKey.KeyHash;
                cacheDependencies1 = debugInfo1.EFCacheKey.CacheDependencies;
            }

            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine("same Cacheable query, reading from 2nd level cache");
                databaseLog.Clear();
                var debugInfo2 = new EFCacheDebugInfo();
                var firstProductIncludeTags2 = context.Products
                    .Include(x => x.Tags)
                    .Select(x => new { Name = x.ProductName, x.Tags }).OrderBy(x => x.Name)
                    .Cacheable(debugInfo2)
                    .FirstOrDefault();
                Assert.IsNotNull(firstProductIncludeTags2);
                var sqlCommands = databaseLog.ToString().Trim();
                Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                Assert.AreEqual(true, debugInfo2.IsCacheHit);
            }

            using (var context = new SampleContext())
            {
                var databaseLog = new StringBuilder();
                context.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                Trace.WriteLine(
                    @"3rd query looks the same, but it doesn't have the Include method, so it shouldn't produce the same queryKeyHash.
                 This was the problem with just parsing the LINQ expression, without considering the produced SQL.");
                databaseLog.Clear();
                var debugInfo3 = new EFCacheDebugInfo();
                var firstProduct = context.Products
                    .Select(x => new { Name = x.ProductName, x.Tags }).OrderBy(x => x.Name)
                    .Cacheable(debugInfo3)
                    .FirstOrDefault();
                Assert.IsNotNull(firstProduct);
                Assert.AreEqual(false, debugInfo3.IsCacheHit);
                var hash3 = debugInfo3.EFCacheKey.KeyHash;
                var cacheDependencies3 = debugInfo3.EFCacheKey.CacheDependencies;

                Assert.AreNotEqual(hash1, hash3);
                Assert.AreNotEqual(cacheDependencies1, cacheDependencies3);
            }
        }

        protected static void ExecuteInParallel(Action test, int count = 40)
        {
            var tests = new Action[count];
            for (var i = 0; i < count; i++)
            {
                tests[i] = test;
            }
            Parallel.Invoke(tests);
        }

        [TestMethod]
        public void TestParallelQueriesShouldCacheData()
        {
            var debugInfo1 = new EFCacheDebugInfo();
            ExecuteInParallel(() =>
            {
                using (var context = new SampleContext())
                {
                    var firstProductIncludeTags = context.Products
                        .Include(x => x.Tags)
                        .Select(x => new {Name = x.ProductName, x.Tags}).OrderBy(x => x.Name)
                        .Cacheable(debugInfo1)
                        .FirstOrDefault();
                    Assert.IsNotNull(firstProductIncludeTags);
                }
            });
            Assert.AreEqual(true, debugInfo1.IsCacheHit);
        }
    }
}