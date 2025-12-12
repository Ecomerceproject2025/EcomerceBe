namespace EcomerceBE.DTOs
{


    public class UserFilter
    {
        public string? EmailOrName { get; set; }
        public string? Status { get; set; }
        public string? Role { get; set; }
        public DateTime? DateCreate { get; set; }
        public string ? Phone { get; set; }
        public bool? HasRecentOrders { get; set; } // Filter users who have recent orders
        public int? RecentOrdersDays { get; set; } // Number of days to consider "recent" (default 30)
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
    }

}
