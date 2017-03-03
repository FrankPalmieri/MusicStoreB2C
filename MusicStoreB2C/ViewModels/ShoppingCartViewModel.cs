using System.Collections.Generic;
using MusicStoreB2C.Models;

namespace MusicStoreB2C.ViewModels
{
    public class ShoppingCartViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public decimal CartTotal { get; set; }
    }
}
