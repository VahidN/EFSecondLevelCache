using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using EFSecondLevelCache.TestDataLayer.Models;

namespace EFSecondLevelCache.TestDataLayer.DataLayer
{
    public class Configuration : DbMigrationsConfiguration<SampleContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(SampleContext context)
        {
            if(context.Products.Any())
                return;

            var user = new User {Name = "User1"};
            user = context.Users.Add(user);

            var product1 = new Product
            {
                ProductName = "Product1",
                IsActive = true,
                Notes = "Notes ...",
                ProductNumber = "001",
                User = user
            };
            product1 = context.Products.Add(product1);
            var tag1 = new Tag
            {
                Name = "Tag1"
            };
            context.Tags.Add(tag1);
            product1.Tags  = new List<Tag> { tag1 };

            var product2 = new Product
            {
                ProductName = "Product2",
                IsActive = true,
                Notes = "Notes ...",
                ProductNumber = "002",
                User = user
            };
            context.Products.Add(product2);

            var product3 = new Product
            {
                ProductName = "Product3",
                IsActive = true,
                Notes = "Notes ...",
                ProductNumber = "003",
                User = user
            };
            context.Products.Add(product3);

            base.Seed(context);
        }
    }
}