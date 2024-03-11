using System.ComponentModel.DataAnnotations;

namespace ComputerHardwareOnlineStore.Models
{
    public class ProductDto
    {
        [Required]
        public string Name { get; set; } = "";
        [Required]
        public string Description { get; set; } = "";
        [Required]
        public int Qty { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public int InStock { get; set; }
    }
}
