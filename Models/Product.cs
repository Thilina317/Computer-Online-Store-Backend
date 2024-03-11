namespace ComputerHardwareOnlineStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Qty { get; set; }
        public decimal Amount { get; set; }
        public int InStock { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
