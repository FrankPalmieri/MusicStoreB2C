using System.Collections.Generic;
using MusicStoreB2C.Models;

namespace MusicStoreB2C.ViewModels
{
    public class ViewOrdersViewModel
    {
        public List<Order> Orders { get; set; }

        public decimal OrderTotal { get; set; }
    }
}
