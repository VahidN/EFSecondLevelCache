EFSecondLevelCache
=======
Entity Framework 6.x Second Level Caching Library.

Second level caching is a query cache. The results of EF commands will be stored in the cache, so that the same EF commands will retrieve their data from the cache rather than executing them against the database again.

Install via NuGet
-----------------
To install EFSecondLevelCache, run the following command in the Package Manager Console:

```
PM> Install-Package EFSecondLevelCache
```

You can also view the [package page](http://www.nuget.org/packages/EFSecondLevelCache/) on NuGet.



Usage:
-----------------
1- Setting up the cache invalidation by overriding the SaveChanges method to prevent stale reads:

```csharp
namespace EFSecondLevelCache.TestDataLayer.DataLayer
{
    public class SampleContext : DbContext
    {
        // public DbSet<Product> Products { get; set; }

        public SampleContext()
            : base("connectionString1")
        {
        }

        public override int SaveChanges()
        {
            return SaveAllChanges(invalidateCacheDependencies: true);
        }

        public int SaveAllChanges(bool invalidateCacheDependencies = true)
        {
            var changedEntityNames = getChangedEntityNames();
            var result = base.SaveChanges();
            if (invalidateCacheDependencies)
            {
               new EFCacheServiceProvider().InvalidateCacheDependencies(changedEntityNames);
            }
            return result;
        }

        private string[] getChangedEntityNames()
        {
            return this.ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Added ||
                            x.State == EntityState.Modified ||
                            x.State == EntityState.Deleted)
                .Select(x => ObjectContext.GetObjectType(x.Entity.GetType()).FullName)
                .Distinct()
                .ToArray();
        }
    }
}
```

Sometimes you don't want to invalidate the cache when non important properties such as NumberOfViews are updated.
In these cases, try `SaveAllChanges(invalidateCacheDependencies: false)`, before updating the data.


2- Then to cache the results of the normal queries like:
```csharp
var products = context.Products.Include(x => x.Tags).FirstOrDefault();
```
We can use the new `Cacheable()` extension method:
```csharp
var products = context.Products.Include(x => x.Tags).Cacheable().FirstOrDefault(); // Async methods are supported too.
```


Notes:
-----------------
Good candidates for query caching are global site's settings,
list of `public` articles or comments and not frequently changed,
private or specific data to each user.
If a page requires authentication, its data shouldn't be cached.
