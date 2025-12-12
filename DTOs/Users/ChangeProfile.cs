using EcomerceBE.Models;
using System.ComponentModel.DataAnnotations;

namespace EcomerceBE.DTOs
{
    public class ChangeProfile
    {

        public int Id { get; set; }

        public string ? Lname { get; set; }
        public string  ? Fname { get; set; }

        public string? CurrentPassWord { get; set; }

        public string? NewPassWord { get; set; }

        public string ? Avatar { get; set; }

        public Address? Address { get; set; }

    }
}
