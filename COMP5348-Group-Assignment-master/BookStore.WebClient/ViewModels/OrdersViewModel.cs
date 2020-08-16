using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookStore.Services.MessageTypes;
using BookStore.Services.Interfaces;

namespace BookStore.WebClient.ViewModels
{
    public class OrdersViewModel
    {
        public OrdersViewModel(User p)
        {
            pUser = p;
        }

        public User pUser
        {
            get;set;
        }

        private IOrderService OrderService
        {
            get
            {
                return ServiceFactory.Instance.OrderService;
            }
        }

        public List<Order> Orders
        {
            get
            {
                return OrderService.GetOrders(pUser);
            }
        }
    }
}