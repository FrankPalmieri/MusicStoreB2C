using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MusicStoreB2C.Models
{
    public class OrderHistory
    {
        private readonly MusicStoreContext _dbContext;
        private readonly ClaimsPrincipal _user;
        private OrderHistory(MusicStoreContext dbContext, ClaimsPrincipal user)
        {
            _dbContext = dbContext;
            _user = user;
        }
        public static OrderHistory GetOrderHistory(MusicStoreContext db, ClaimsPrincipal user)
            => new OrderHistory(db, user);

        public Task<int> GetCount(ClaimsPrincipal user)
        {
            // Get the count of each item in the cart and sum them up
            return _dbContext.Orders
                .Where(o => o.Username == user.Identity.Name)
                .CountAsync();
        }

        public Task<bool> HasOrdersAsync()
        {
            var orderCount =
                _dbContext
                .Orders
                .Where(o => o.Username == _user.Identity.Name).
                Count();
            return Task.FromResult((orderCount > 0)? true : false);
        }

        public Task<List<Order>> GetOrders()
        {
            return _dbContext
                .Orders
                .Where(o => o.Username == _user.Identity.Name)
                .ToListAsync();
        }

        public Task<decimal> GetOrdersTotal()
        {
            return _dbContext
                .Orders
                .Where(o => o.Username == _user.Identity.Name)
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