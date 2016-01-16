using System.ComponentModel.DataAnnotations.Schema;

namespace EFSecondLevelCache.TestDataLayer.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { set; get; }
        public int UserId { set; get; }
    }

    [Table("Pages")]
    public class Page : Post
    {
    }
}
