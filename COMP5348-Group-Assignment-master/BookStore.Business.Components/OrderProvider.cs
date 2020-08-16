using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using System.Transactions;
using Microsoft.Practices.ServiceLocation;
using DeliveryCo.MessageTypes;
using System.Data.Entity;
using System.Data.SqlClient;

namespace BookStore.Business.Components
{
    public class OrderProvider : IOrderProvider
    {
        public IEmailProvider EmailProvider
        {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public IUserProvider UserProvider
        {
            get { return ServiceLocator.Current.GetInstance<IUserProvider>(); }
        }

        public void SubmitOrder(Entities.Order pOrder)
        {      
            using (TransactionScope lScope = new TransactionScope())
            {
                //LoadBookStocks(pOrder);
                //MarkAppropriateUnchangedAssociations(pOrder);

                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                    try
                    {
                        pOrder.OrderNumber = Guid.NewGuid();
                        pOrder.Store = "OnLine";

                        // Book objects in pOrder are missing the link to their Stock tuple (and the Stock GUID field)
                        // so fix up the 'books' in the order with well-formed 'books' with 1:1 links to Stock tuples
                        foreach (OrderItem lOrderItem in pOrder.OrderItems)
                        {
                            int bookId = lOrderItem.Book.Id;
                            lOrderItem.Book = lContainer.Books.Where(book => bookId == book.Id).First();
                            var stockList = from stock in lContainer.Stocks
                                            where stock.Book.Id == bookId
                                            select stock;
                            foreach(var stock in stockList)
                            {
                                Stock temp = lOrderItem.Book.Stock.Where(s => stock.Id == s.Id).First();
                                System.Guid stockId = temp.Id;
                                temp = lContainer.Stocks.Where(stocks => stockId == stocks.Id).First();
                            }
                        }
                        // and update the stock levels

                        // add the modified Order tree to the Container (in Changed state)
                        lContainer.Orders.Add(pOrder);
                        Console.WriteLine("Order Received: " + pOrder.OrderNumber);

                        // ask the Bank service to transfer fundss
                        TransferFundsFromCustomer(UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.Total ?? 0.0, pOrder.OrderNumber.ToString());
                        Console.WriteLine("Funds Transfer Processing..." );

                        // ask the delivery service to organise delivery
                        //PlaceDeliveryForOrder(pOrder);

                        // and save the order
                        lContainer.SaveChanges();
                        lScope.Complete();                    
                    }
                    catch (Exception lException)
                    {
                        SendOrderErrorMessage(pOrder, lException);
                        IEnumerable<System.Data.Entity.Infrastructure.DbEntityEntry> entries =  lContainer.ChangeTracker.Entries();
                        throw;
                    }
                }
            }
            //SendOrderPlacedConfirmation(pOrder);
        }

        public List<Entities.Order> GetOrders(User pUser)
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                List<User> userList = lContainer.Database.SqlQuery<User>("SELECT * FROM Users WHERE Email = @email", new SqlParameter("@email", pUser.Email)).ToList();
                List<Order> orderList = new List<Order>();

                foreach (User user in userList)
                {
                    List<Order> temp = lContainer.Database.SqlQuery<Order>("SELECT * FROM Orders WHERE Customer_Id = @Customer_Id", new SqlParameter("@Customer_Id", user.Id)).ToList();
                    foreach (Order order in temp)
                    {
                        orderList.Add(order);
                    }

                }
                //return lContainer.Database.SqlQuery<Order>("SELECT * FROM Orders WHERE Customer_Id = @Customer_Id", new SqlParameter("@Customer_Id", pUser.Id)).ToList();
                //return (from orderItem in lContainer.Orders.Include("Customer.LoginCredential")
                //        orderby orderItem.Id
                //        select orderItem).ToList();
                return orderList;
            }
        }

        public Boolean CancelOrder(Order pOrder)
        {
            if (pOrder.Delivery == null)
            {
                Console.WriteLine("Order cannot be cancelled. Delivery request has been submitted");
                return false;
            }
            TransferService.TransferServiceClient lClient = new TransferService.TransferServiceClient();
            lClient.Refund(pOrder.Total ?? 0.0, RetrieveBookStoreAccountNumber(), UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.OrderNumber.ToString());
            return true;
        }

        //private void MarkAppropriateUnchangedAssociations(Order pOrder)
        //{
        //    pOrder.Customer.MarkAsUnchanged();
        //    pOrder.Customer.LoginCredential.MarkAsUnchanged();
        //    foreach (OrderItem lOrder in pOrder.OrderItems)
        //    {
        //        lOrder.Book.Stock.MarkAsUnchanged();
        //        lOrder.Book.MarkAsUnchanged();
        //    }
        //}

        /* private void LoadBookStocks(Order pOrder)
         {
             using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
             {
                 foreach (OrderItem lOrderItem in pOrder.OrderItems)
                 {
                     lOrderItem.Book.Stock = lContainer.Stocks.Where((pStock) => pStock.Book.Id == lOrderItem.Book.Id).FirstOrDefault();    
                 }
             }
         }*/

        private void SendOrderErrorMessage(Order pOrder, Exception pException)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "There was an error in processsing your order " + pOrder.OrderNumber + ": "+ pException.Message + ". Please contact Book Store"
            });
        }

        private void SendOrderPlacedConfirmation(Order pOrder)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "Your order " + pOrder.OrderNumber + " has been placed"
            });
        }

        /*private void PlaceDeliveryForOrder(Order pOrder)
        {
            Delivery lDelivery = new Delivery() { DeliveryStatus = DeliveryStatus.Submitted, SourceAddress = "Book Store Address", DestinationAddress = pOrder.Customer.Address, Order = pOrder };

            Guid lDeliveryIdentifier = ExternalServiceFactory.Instance.DeliveryService.SubmitDelivery(new DeliveryInfo()
            { 
                OrderNumber = lDelivery.Order.OrderNumber.ToString(),  
                SourceAddress = lDelivery.SourceAddress,
                DestinationAddress = lDelivery.DestinationAddress,
                DeliveryNotificationAddress = "net.tcp://localhost:9010/DeliveryNotificationService"
            });

            lDelivery.ExternalDeliveryIdentifier = lDeliveryIdentifier;
            pOrder.Delivery = lDelivery;   
        }*/

        private void TransferFundsFromCustomer(int pCustomerAccountNumber, double pTotal, string Oid)
        {
            try
            {
                /* REMOVED/CHANGED
                 * The line below retreives the instance of Bank and directly calls the TransferService class in Bank.
                 * We do not want this as, if the Bank application is not running, the Bookstore application
                 * will crash when submitting an order. 
                 * Instead, we use TransferService from Bank (first run Bank.Process in one instance and then
                 * "Add service reference" in Bank.Components ->net.tcp://localhost:9042/TransferService/mex)
                 * This way, the message is sent through BankTranferQueueTransacted and the applications can run
                 * independently.
                 */
                //ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, pCustomerAccountNumber, RetrieveBookStoreAccountNumber());
                TransferService.TransferServiceClient lClient = new TransferService.TransferServiceClient();
                lClient.Transfer(pTotal, pCustomerAccountNumber, RetrieveBookStoreAccountNumber(), Oid);
            }
            catch
            {
                throw new Exception("Error when transferring funds for order.");
                //TODO Send email to customer - ERROR
            }
        }


        private int RetrieveBookStoreAccountNumber()
        {
            return 123;
        }


    }
}
