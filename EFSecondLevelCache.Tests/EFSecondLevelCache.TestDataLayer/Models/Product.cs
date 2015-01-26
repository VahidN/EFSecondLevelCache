using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFSecondLevelCache.TestDataLayer.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [StringLength(30)]
        [Required]
        public string ProductNumber { get; set; }

        [StringLength(50)]
        [Required]
        [Index(IsUnique = true)]
        public string ProductName { get; set; }

        [StringLength(int.MaxValue)]
        public string Notes { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Tag> Tags { set; get; }

        [ForeignKey("UserId")]
        public virtual User User { set; get; }
        public int UserId { set; get; }
    }
}
