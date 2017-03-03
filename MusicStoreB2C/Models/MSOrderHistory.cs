using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MusicStoreB2C.Models
{
    public class MSOrderHistory
    {
        private readonly MusicStoreContext _dbContext;
        private readonly ApplicationUser _user;
        private MSOrderHistory(MusicStoreContext dbContext, ApplicationUser user)
        {
            _dbContext = dbContext;
            _user = user;
        }
        public static MSOrderHistory GetOrderHistory(MusicStoreContext db, ApplicationUser user)
            => new MSOrderHistory(db, user);

        public Task<int> GetCount(ApplicationUser user)
        {
            // Get the count of each item in the cart and sum them up
            return _dbContext.Orders
                .Where(o => o.Username == user.UserName)
                .CountAsync();
        }

        public Task<bool> HasOrdersAsync()
        {
            var orderCount =
                _dbContext
                .Orders
                .Where(o => o.Username == _user.UserName).
                Count();
            return Task.FromResult((orderCount > 0)? true : false);
        }

        public Task<List<Order>> GetOrders()
        {
            return _dbContext
                .Orders
                .Where(o => o.Username == _user.UserName)
                .ToListAsync();
        }

        public Task<decimal> GetOrdersTotal()
        {
            return _dbContext
                .Orders
                .Where(o => o.Username == _user.UserName)
                .Select(o => o.Total)
                .SumAsync();
        }

        public Task<List<OrderDetail>> GetOrderDetails(int id)
        {
            return _dbContext
                .OrderDetails
                .Where(d => d.OrderId == id)
                .Include(a => a.Album)
                .ThenInclude(g => g.Genre)
                .Include(a => a.Album)
                .ThenInclude(t => t.Artist)
                .ToListAsync();
        }

        public Task<decimal> GetOrderTotal(int id)
        {
            return _dbContext
                .Orders
                .Where(o => o.OrderId == id)
                .Select(o => o.Total)
                .SumAsync();
        }
    }
}