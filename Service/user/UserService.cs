using EcomerceBE.Data;
using EcomerceBE.Models;

namespace EcomerceBE.Service.user
{
    public interface IUserService
    {
        User? GetById(int id);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public User? GetById(int id)
        {
            return _db.Users.FirstOrDefault(u => u.Id == id);
        }
    }

}
