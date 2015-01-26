using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using EFSecondLevelCache.TestDataLayer.DataLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.FunctionalTests
{
    [TestClass]
    public class PerformanceTests
    {
        [TestInitialize]
        public void ClearEFGlobalCacheBeforeEachTest()
        {
            new EFCacheServiceProvider().ClearAllCachedEntries();
        }

        [TestMethod]
        public void UncachedQueries()
        {
            using (var context = new SampleContext())
            {
                var watch = Stopwatch.StartNew();
                for (var i = 0; i <= 10000; i++)
                {
                    var products = context.Products.Include(x => x.Tags).ToList();
                }
                Trace.WriteLine(string.Format("10000 iterations in {0} ms. Average speed: {1} iterations/second.", watch.ElapsedMilliseconds, (int)(10000 / watch.Elapsed.TotalSeconds)));
            }
        }

        [TestMethod]
        public void CachedQueries()
        {
            using (var context = new SampleContext())
            {
                var watch = Stopwatch.StartNew();
                for (var i = 0; i <= 10000; i++)
                {
                    var products = context.Products.Include(x => x.Tags).Cacheable().ToList();
                }
                Trace.WriteLine(string.Format("10000 iterations in {0} ms. Average speed: {1} iterations/second.", watch.ElapsedMilliseconds, (int)(10000 / watch.Elapsed.TotalSeconds)));
            }
        }
    }
}