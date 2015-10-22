using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using EFSecondLevelCache.TestDataLayer.DataLayer;

namespace EFSecondLevelCache.PerformanceTests
{
    class Program
    {
        private static void cachedQueries()
        {
            Console.WriteLine("CachedQueries");
            using (var context = new SampleContext())
            {
                var watch = Stopwatch.StartNew();
                for (var i = 0; i <= 10000; i++)
                {
                    var products = context.Products.Include(x => x.Tags).Cacheable().ToList();
                }
                Console.WriteLine("10000 iterations in {0} ms. Average speed: {1} iterations/second.", watch.ElapsedMilliseconds, (int)(10000 / watch.Elapsed.TotalSeconds));
            }
        }

        static void Main(string[] args)
        {
            startDb();
            cachedQueries();
            uncachedQueries();
        }

        private static void startDb()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<SampleContext, Configuration>());
            using (var ctx = new SampleContext())
            {
                ctx.Database.Initialize(force: true);
            }
        }

        private static void uncachedQueries()
        {
            Console.WriteLine("UncachedQueries");
            using (var context = new SampleContext())
            {
                var watch = Stopwatch.StartNew();
                for (var i = 0; i <= 10000; i++)
                {
                    var products = context.Products.Include(x => x.Tags).ToList();
                }
                Console.WriteLine("10000 iterations in {0} ms. Average speed: {1} iterations/second.", watch.ElapsedMilliseconds, (int)(10000 / watch.Elapsed.TotalSeconds));
            }
        }
    }
}