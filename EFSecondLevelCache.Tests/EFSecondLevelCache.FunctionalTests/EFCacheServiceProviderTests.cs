using System;
using System.Linq;
using EFSecondLevelCache.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.FunctionalTests
{
    [TestClass]
    public class EFCacheServiceProviderTests
    {
        private readonly IEFCacheServiceProvider _cacheService;
        public EFCacheServiceProviderTests()
        {
            _cacheService = new EFCacheServiceProvider();
        }

        [TestInitialize]
        public void ClearEFGlobalCacheBeforeEachTest()
        {
            _cacheService.ClearAllCachedEntries();
        }

        [TestMethod]
        public void TestCacheInvalidationWithTwoRoots()
        {
            _cacheService.StoreRootCacheKeys(new[] { "entity1.model", "entity2.model" });
            _cacheService.InsertValue("EF_key1", "value1", new[] { "entity1.model", "entity2.model" }, DateTime.Now.AddMinutes(10));

            _cacheService.StoreRootCacheKeys(new[] { "entity1.model", "entity2.model" });
            _cacheService.InsertValue("EF_key2", "value2", new[] { "entity1.model", "entity2.model" }, DateTime.Now.AddMinutes(10));


            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            _cacheService.InvalidateCacheDependencies(new[] { "entity2.model" });

            value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);

            var keys = _cacheService.GetAllEFCachedKeys();
            var key1 = keys.FirstOrDefault(key => key == "EF_key1");
            Assert.IsNull(key1);
            Assert.AreEqual(0, keys.Count);
        }

        [TestMethod]
        public void TestCacheInvalidationWithOneRoot()
        {
            _cacheService.StoreRootCacheKeys(new[] { "entity1" });
            _cacheService.InsertValue("EF_key1", "value1", new[] { "entity1" }, DateTime.Now.AddMinutes(10));

            _cacheService.StoreRootCacheKeys(new[] { "entity1" });
            _cacheService.InsertValue("EF_key2", "value2", new[] { "entity1" }, DateTime.Now.AddMinutes(10));

            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            _cacheService.InvalidateCacheDependencies(new[] { "entity1" });

            value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);

            var keys = _cacheService.GetAllEFCachedKeys();
            var key1 = keys.FirstOrDefault(key => key == "EF_key1");
            Assert.IsNull(key1);
            Assert.AreEqual(0, keys.Count);
        }

        [TestMethod]
        public void TestCacheInvalidationWithSimilarRoots()
        {
            _cacheService.StoreRootCacheKeys(new[] { "entity1", "entity2" });
            _cacheService.InsertValue("EF_key1", "value1", new[] { "entity1", "entity2" }, DateTime.Now.AddMinutes(10));

            _cacheService.StoreRootCacheKeys(new[] { "entity2" });
            _cacheService.InsertValue("EF_key2", "value2", new[] { "entity2" }, DateTime.Now.AddMinutes(10));

            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNotNull(value1);

            var value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNotNull(value2);

            _cacheService.InvalidateCacheDependencies(new[] { "entity2" });

            value1 = _cacheService.GetValue("EF_key1");
            Assert.IsNull(value1);

            value2 = _cacheService.GetValue("EF_key2");
            Assert.IsNull(value2);

            var keys = _cacheService.GetAllEFCachedKeys();
            var key1 = keys.FirstOrDefault(key => key == "EF_key1");
            Assert.IsNull(key1);
            Assert.AreEqual(0, keys.Count);
        }

        [TestMethod]
        public void TestInsertingNullValues()
        {
            _cacheService.StoreRootCacheKeys(new[] { "entity1", "entity2" });
            _cacheService.InsertValue("EF_key1", null, new[] { "entity1", "entity2" }, DateTime.Now.AddMinutes(10));

            var value1 = _cacheService.GetValue("EF_key1");
            Assert.IsTrue(Equals(value1, _cacheService.NullObject));
        }
    }
}