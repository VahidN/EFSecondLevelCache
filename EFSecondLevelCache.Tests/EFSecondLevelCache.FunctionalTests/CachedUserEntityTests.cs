using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EFSecondLevelCache.Contracts;
using EFSecondLevelCache.TestDataLayer.DataLayer;
using EFSecondLevelCache.TestDataLayer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFSecondLevelCache.FunctionalTests
{
    [TestClass]
    public class CachedUserEntityTests
    {
        readonly Random _rnd = new Random();

        [TestInitialize]
        public void ClearEFGlobalCacheBeforeEachTest()
        {
            new EFCacheServiceProvider().ClearAllCachedEntries();


            using (var context = new SampleContext())
            {
                var user = new User
                {
                    Name = string.Format("User {0}", _rnd.Next())
                };
                context.Users.Add(user);
                context.SaveChanges();
            }
        }

        [TestMethod]
        public void TestSecondLevelCacheDoesNotHitTheDatabase()
        {
            int id = 20000;

            var databaseLog = new StringBuilder();

            var uow = new SampleContext();
            uow.Database.Log = commandLine =>
            {
                databaseLog.AppendLine(commandLine);
                Trace.Write(commandLine);
            };


            Trace.WriteLine("1st query, reading from db.");
            databaseLog.Clear();
            var debugInfo1 = new EFCacheDebugInfo();
            var list1 = uow.Set<User>()
                .OrderBy(x => x.Name)
                .Where(x => x.Id > id)
                .Cacheable(debugInfo1)
                .ToList();
            var sqlCommands = databaseLog.ToString().Trim();
            Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
            Assert.AreEqual(false, debugInfo1.IsCacheHit);
            Assert.IsNotNull(list1);
            var hash1 = debugInfo1.EFCacheKey.KeyHash;

            Trace.WriteLine("same query, reading from 2nd level cache.");
            databaseLog.Clear();
            var debugInfo2 = new EFCacheDebugInfo();
            var list2 = uow.Set<User>()
                .OrderBy(x => x.Name)
                .Where(x => x.Id > id)
                .Cacheable(debugInfo2)
                .ToList();
            sqlCommands = databaseLog.ToString().Trim();
            Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
            Assert.AreEqual(true, debugInfo2.IsCacheHit);
            Assert.IsNotNull(list2);
            var hash2 = debugInfo2.EFCacheKey.KeyHash;


            Trace.WriteLine("same query, reading from 2nd level cache.");
            databaseLog.Clear();
            var debugInfo3 = new EFCacheDebugInfo();
            var list3 = uow.Set<User>()
                .OrderBy(x => x.Name)
                .Where(x => x.Id > id)
                .Cacheable(debugInfo3)
                .ToList();
            sqlCommands = databaseLog.ToString().Trim();
            Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
            Assert.AreEqual(true, debugInfo3.IsCacheHit);
            Assert.IsNotNull(list3);
            var hash3 = debugInfo3.EFCacheKey.KeyHash;

            Assert.AreEqual(hash1, hash2);
            Assert.AreEqual(hash2, hash3);

            Trace.WriteLine("different query, reading from db.");
            databaseLog.Clear();
            var debugInfo4 = new EFCacheDebugInfo();
            var list4 = uow.Set<User>()
                .OrderBy(x => x.Name)
                .Where(x => x.Id > 20001)
                .Cacheable(debugInfo4)
                .ToList();
            sqlCommands = databaseLog.ToString().Trim();
            Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
            Assert.AreEqual(false, debugInfo4.IsCacheHit);
            Assert.IsNotNull(list4);

            var hash4 = debugInfo4.EFCacheKey.KeyHash;
            Assert.AreNotSame(hash3, hash4);

            uow.Dispose();
        }

        [TestMethod]
        public void TestSecondLevelCacheDoesNotHitTheDatabase2()
        {
            int id = 20000;
            var databaseLog = new StringBuilder();

            using (var uow = new SampleContext())
            {
                uow.Database.Log = commandLine =>
                {
                    databaseLog.AppendLine(commandLine);
                    Trace.Write(commandLine);
                };

                using (var ctx = new SampleContext())
                {
                    ctx.Database.Log = commandLine =>
                    {
                        databaseLog.AppendLine(commandLine);
                        Trace.Write(commandLine);
                    };


                    Trace.WriteLine("1st query, reading from db.");
                    databaseLog.Clear();
                    var debugInfo1 = new EFCacheDebugInfo();
                    var list1 = ctx.Users
                        .OrderBy(x => x.Name)
                        .Where(x => x.Id > id)
                        .Cacheable(debugInfo1)
                        .ToList();
                    var sqlCommands = databaseLog.ToString().Trim();
                    Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                    Assert.AreEqual(false, debugInfo1.IsCacheHit);
                    Assert.IsNotNull(list1);
                    var hash1 = debugInfo1.EFCacheKey.KeyHash;

                    Trace.WriteLine("same query, reading from 2nd level cache.");
                    databaseLog.Clear();
                    var debugInfo2 = new EFCacheDebugInfo();
                    var list2 = ctx.Users
                        .OrderBy(x => x.Name)
                        .Where(x => x.Id > id)
                        .Cacheable(debugInfo2)
                        .ToList();
                    sqlCommands = databaseLog.ToString().Trim();
                    Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                    Assert.AreEqual(true, debugInfo2.IsCacheHit);
                    Assert.IsNotNull(list2);
                    var hash2 = debugInfo2.EFCacheKey.KeyHash;


                    Trace.WriteLine("same query, reading from 2nd level cache.");
                    databaseLog.Clear();
                    var debugInfo3 = new EFCacheDebugInfo();
                    var list3 = uow.Set<User>()
                        .OrderBy(x => x.Name)
                        .Where(x => x.Id > id)
                        .Cacheable(debugInfo3)
                        .ToList();
                    sqlCommands = databaseLog.ToString().Trim();
                    Assert.AreEqual(true, string.IsNullOrWhiteSpace(sqlCommands));
                    Assert.AreEqual(true, debugInfo3.IsCacheHit);
                    Assert.IsNotNull(list3);
                    var hash3 = debugInfo3.EFCacheKey.KeyHash;

                    Assert.AreEqual(hash1, hash2);
                    Assert.AreEqual(hash2, hash3);

                    Trace.WriteLine("different query, reading from db.");
                    databaseLog.Clear();
                    var debugInfo4 = new EFCacheDebugInfo();
                    var list4 = uow.Set<User>()
                        .OrderBy(x => x.Name)
                        .Where(x => x.Id > 20001)
                        .Cacheable(debugInfo4)
                        .ToList();
                    sqlCommands = databaseLog.ToString().Trim();
                    Assert.AreEqual(false, string.IsNullOrWhiteSpace(sqlCommands));
                    Assert.AreEqual(false, debugInfo4.IsCacheHit);
                    Assert.IsNotNull(list4);

                    var hash4 = debugInfo4.EFCacheKey.KeyHash;
                    Assert.AreNotSame(hash3, hash4);
                }
            }
        }
    }
}