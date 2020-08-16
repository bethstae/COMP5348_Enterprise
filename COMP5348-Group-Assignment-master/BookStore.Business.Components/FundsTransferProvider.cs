using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using DeliveryCo.MessageTypes;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BookStore.Business.Components
{
    class FundsTransferProvider : IFundsTransferProvider
    {
        public IEmailProvider EmailProvider
        {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public void TransferOutcome(bool pOutcome, string Oid)
        {
            Order pOrder = RetrieveOrder(Oid);
            pOrder.CheckStockLevels();
            pOrder = RetrieveOrder(Oid);
            using (TransactionScope lScope = new TransactionScope())
            {
                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                    if (pOutcome)
                    {
                        Console.WriteLine("Funds transferred successfully!");
                        System.Threading.Thread.Sleep(10000);
                        if (pOrder.OrderItems == null)
                        {
                            return;
                        }
                        Console.WriteLine("Sending delivery request...");
                        //Place delivery for order
                        pOrder.UpdateStockLevels();
                        SendDeliveryRequest(pOrder);
                    }
                    else
                    {
                        Console.WriteLine("Failure in transferring funds!");
                        //Send email to customer- INSUFFICIENT FUNDS
                        SendInsufficientFundsEmail(pOrder);
                    }
                    lContainer.SaveChanges();
                    lScope.Complete();
                }
            }
        }

        public void SendDeliveryRequest(Order pOrder)
        {
            try
            {
                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                    DeliveryService.DeliveryServiceClient lClient = new DeliveryService.DeliveryServiceClient();
                    Guid id = Guid.NewGuid();
                    Delivery lDelivery = new Delivery()
                    {
                        ExternalDeliveryIdentifier = id,
                        DeliveryStatus = DeliveryStatus.Submitted,
                        SourceAddress = "Book Store Address",
                        DestinationAddress = pOrder.Customer.Address,
                        Order = pOrder
                    };
                    lDelivery.ExternalDeliveryIdentifier = id;
                    pOrder.Delivery = lDelivery;
                    lContainer.Deliveries.Add(lDelivery);

                    //get list of warehouses needed to fulfill order
                    Dictionary<int, string> wareHouseList = new Dictionary<int, string>();
                    List<int> warehouseIdList = lContainer.Database.SqlQuery<int>("SELECT Warehouses_Id FROM OrderWarehouse WHERE Orders_Id = @Id", new SqlParameter("@Id", pOrder.Id)).ToList();
                    foreach (var w in warehouseIdList)
                    {
                        Warehouse temp = lContainer.Warehouses.Where(s => w == s.Id).First();
                        wareHouseList.Add(temp.Id, temp.Name);
                        //Console.WriteLine(temp.Name);
                    }

                    lClient.SubmitDelivery(new DeliveryInfo()
                    {
                        OrderNumber = lDelivery.Order.OrderNumber.ToString(),
                        SourceAddress = lDelivery.SourceAddress,
                        DestinationAddress = lDelivery.DestinationAddress,
                        DeliveryNotificationAddress = "net.tcp://localhost:9010/DeliveryNotificationService",
                        DeliveryIdentifier = id
                    }, wareHouseList);
                    lContainer.SaveChanges();
                }

            }
            catch
            {
                String message = "Our records show that the delivery request for order " + pOrder.Id + " was unsuccessful. Please contact BookStore.";
                SendEmail(pOrder, message);
                throw new Exception("Error sending delivery request");
                //TODO Send email to customer - ERROR
            }
        }

        public void RefundOutcome(bool pOutcome, string Oid)
        {
            Order pOrder = RetrieveOrder(Oid);
            if (pOutcome){
                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                    pOrder.OrderItems = null;
                    lContainer.SaveChanges();
                }
                    String message = "Our records show that the refund for order " + pOrder.Id + " was successful";
                SendEmail(pOrder, message);
                Console.WriteLine("Refund for Order: " + pOrder.Id + " was successful!");
            }
            else{
                String message = "Our records show that the refund for order " + pOrder.Id + " has failed. Please contact BookStore.";
                SendEmail(pOrder, message);
                Console.WriteLine("Refund for Order: " + pOrder.Id + " failed!");
            }
        }

        private Order RetrieveOrder(string Oid)
        {
            Guid gOid = Guid.Parse(Oid);
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                Order order = lContainer.Orders.Include("Customer.LoginCredential").Include("OrderItems.Book").Where((pOrder) => (pOrder.OrderNumber == gOid)).FirstOrDefault();
                return order;
            }
        }

        public void SendEmail(Order pOrder, String message)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = message
            });
        }

        public void SendInsufficientFundsEmail(Order pOrder)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "Your order " + pOrder.OrderNumber + " has been rolled back due to insufficient funds"
            });
        }
    }
}
