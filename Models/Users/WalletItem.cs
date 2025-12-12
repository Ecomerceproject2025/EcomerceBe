namespace EcomerceBE.Models
{
    public class WalletItem
    {
        public int WalletItemId { get; set; }
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Wallet Wallet { get; set; }
    }
}
