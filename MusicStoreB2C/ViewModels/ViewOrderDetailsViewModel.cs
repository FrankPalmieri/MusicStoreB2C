using System.Collections.Generic;
using MusicStoreB2C.Models;

namespace MusicStoreB2C.ViewModels
{
    public class ViewOrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        public decimal OrderTotal { get; set; }
    }
}
