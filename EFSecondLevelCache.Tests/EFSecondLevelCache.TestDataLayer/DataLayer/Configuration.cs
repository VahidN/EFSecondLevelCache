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
            User user1;

            const string user1Name = "User1";
            if (!context.Users.Any(user => user.Name == user1Name))
            {
                user1 = new User { Name = user1Name };
                user1 = context.Users.Add(user1);
            }
            else
            {
                user1 = context.Users.First(user => user.Name == user1Name);
            }

            const string product1Name = "Product1";
            if (!context.Products.Any(product => product.ProductName == product1Name))
            {
                var product1 = new Product
                {
                    ProductName = product1Name,
                    IsActive = true,
                    Notes = "Notes ...",
                    ProductNumber = "001",
                    User = user1
                };
                product1 = context.Products.Add(product1);
                var tag1 = new Tag
                {
                    Name = "Tag1"
                };
                context.Tags.Add(tag1);
                product1.Tags = new List<Tag> {tag1};
            }


            const string product2Name = "Product2";
            if (!context.Products.Any(product => product.ProductName == product2Name))
            {
                var product2 = new Product
                {
                    ProductName = product2Name,
                    IsActive = true,
                    Notes = "Notes ...",
                    ProductNumber = "002",
                    User = user1
                };
                context.Products.Add(product2);
            }

            const string product3Name = "Product3";
            if (!context.Products.Any(product => product.ProductName == product3Name))
            {
                var product3 = new Product
                {
                    ProductName = product3Name,
                    IsActive = true,
                    Notes = "Notes ...",
                    ProductNumber = "003",
                    User = user1
                };
                context.Products.Add(product3);
            }

            const string post1Title = "Post1";
            if (!context.Posts.Any(post => post.Title == post1Title))
            {
                var page1 = new Page
                {
                    Title = post1Title,
                    User = user1
                };
                context.Posts.Add(page1);
            }

            const string post2Title = "Post2";
            if (!context.Posts.Any(post => post.Title == post2Title))
            {
                var page2 = new Page
                {
                    Title = post2Title,
                    User = user1
                };
                context.Posts.Add(page2);
            }

            base.Seed(context);
        }
    }
}