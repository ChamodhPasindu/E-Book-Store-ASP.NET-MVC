﻿using EBookStore.Models.Entity;

namespace EBookStore.Models.DTO
{
    public class CartItemViewModel
    {
        public int BookId { get; set; }
        public Book? book { get; set; }
        public int Quantity { get; set; }
    }
}
