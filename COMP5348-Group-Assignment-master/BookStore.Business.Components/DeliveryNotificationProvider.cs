using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using Microsoft.Practices.ServiceLocation;
using System.Transactions;

namespace BookStore.Business.Components
{
    public class DeliveryNotificationProvider : IDeliveryNotificationProvider
    {
        public IEmailProvider EmailProvider
        {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public IUserProvider UserProvider
        {
            get { return ServiceLocator.Current.GetInstance<IUserProvider>(); }
        }

        public void NotifyDeliveryCompletion(Guid pDeliveryId, Entities.DeliveryStatus status)
        {
            Order lAffectedOrder = RetrieveDeliveryOrder(pDeliveryId);
            UpdateDeliveryStatus(pDeliveryId, status);
            if (status == Entities.DeliveryStatus.Submitted)
            {
                Console.WriteLine("Delivery request for Order " + lAffectedOrder.OrderNumber+ " received by DeliveryCo");
                EmailProvider.SendMessage(new EmailMessage()
                {
                    ToAddress = lAffectedOrder.Customer.Email,
                    Message = "Our records show that your order " + lAffectedOrder.OrderNumber + " has been received"
                });
            }
            if (status == Entities.DeliveryStatus.Delivered)
            {
                Console.WriteLine("Order " + lAffectedOrder.OrderNumber + " has been delivered to " + lAffectedOrder.Customer.Address);
                EmailProvider.SendMessage(new EmailMessage()
                {
                    ToAddress = lAffectedOrder.Customer.Email,
                    Message = "Our records show that your order " +lAffectedOrder.OrderNumber + " has been delivered. Thank you for shopping at video store"
                });
            }
            if (status == Entities.DeliveryStatus.Failed)
            {
                //refund customer
                TransferService.TransferServiceClient lClient = new TransferService.TransferServiceClient();
                lClient.Refund(lAffectedOrder.Total ?? 0.0, RetrieveBookStoreAccountNumber(), UserProvider.ReadUserById(lAffectedOrder.Customer.Id).BankAccountNumber, lAffectedOrder.OrderNumber.ToString());
                //rollback stocks
                lAffectedOrder.RollbackStockUpdates();
                Console.WriteLine("Order " + lAffectedOrder.OrderNumber + " has failed!");
                EmailProvider.SendMessage(new EmailMessage()
                {
                    ToAddress = lAffectedOrder.Customer.Email,
                    Message = "Our records show that there was a problem" + lAffectedOrder.OrderNumber + " delivering your order. Please contact Book Store"
                });
            }
            if (status == Entities.DeliveryStatus.OrderPicked)
            {
                Console.WriteLine("Order " + lAffectedOrder.OrderNumber + " has been picked from the required warehouses");
                EmailProvider.SendMessage(new EmailMessage()
                {
                    ToAddress = lAffectedOrder.Customer.Email,
                    Message = "Our records show that your order " + lAffectedOrder.OrderNumber + " has been picked from warehouse/s."
                });
            }
            if (status == Entities.DeliveryStatus.OrderInTransit)
            {
                Console.WriteLine("Delivery request for Order " + lAffectedOrder.OrderNumber + " is in transit to "+ lAffectedOrder.Customer.Address);
                EmailProvider.SendMessage(new EmailMessage()
                {
                    ToAddress = lAffectedOrder.Customer.Email,
                    Message = "Our records show that your order " + lAffectedOrder.OrderNumber + " is transit to " + lAffectedOrder.Delivery.DestinationAddress
                });
            }
            if (status == Entities.DeliveryStatus.OrderCancelled)
            {
                EmailProvider.SendMessage(new EmailMessage()
                {
                    ToAddress = lAffectedOrder.Customer.Email,
                    Message = "Our records show that your order " + lAffectedOrder.OrderNumber + " has been successfully cancelled, your money will be refunded shortly" 
                });
            }

        }

        private void UpdateDeliveryStatus(Guid pDeliveryId, DeliveryStatus status)
        {
            using (TransactionScope lScope = new TransactionScope())
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                Delivery lDelivery = lContainer.Deliveries.Where((pDel) => pDel.ExternalDeliveryIdentifier == pDeliveryId).FirstOrDefault();
                if (lDelivery != null)
                {
                    lDelivery.DeliveryStatus = status;
                    lContainer.SaveChanges();
                }
                lScope.Complete();
            }
        }

        private Order RetrieveDeliveryOrder(Guid pDeliveryId)
        {
 	        using(BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                //Console.WriteLine(pDeliveryId);
                Delivery lDelivery =  lContainer.Deliveries.Include("Order.Customer").Where((pDel) => pDel.ExternalDeliveryIdentifier == pDeliveryId).FirstOrDefault();
                return lDelivery.Order;
            }
        }

        private int RetrieveBookStoreAccountNumber()
        {
            return 123;
        }
    }


}
