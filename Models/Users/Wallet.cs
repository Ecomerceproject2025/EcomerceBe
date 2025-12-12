namespace EcomerceBE.Models
{
    public class Wallet
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public decimal Balance { get; set; }

        public User User { get; set; }
        public ICollection<WalletItem> WalletItems { get; set; }
    }
}
