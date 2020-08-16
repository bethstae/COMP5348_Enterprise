using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using BookStore.Services.Interfaces;

namespace BookStore.Services
{
    public class DeliveryNotificationService : IDeliveryNotificationService
    {
        IDeliveryNotificationProvider Provider
        {
            get { return ServiceLocator.Current.GetInstance<IDeliveryNotificationProvider>(); }
        }

        public void NotifyDeliveryCompletion(Guid pDeliveryId, DeliveryInfoStatus status)
        {
            Provider.NotifyDeliveryCompletion(pDeliveryId, GetDeliveryStatusFromDeliveryInfoStatus(status));
        }

        private DeliveryStatus GetDeliveryStatusFromDeliveryInfoStatus(DeliveryInfoStatus status)
        {
            if (status == DeliveryInfoStatus.Delivered)
            {
                return DeliveryStatus.Delivered;
            }
            else if (status == DeliveryInfoStatus.Failed)
            {
                return DeliveryStatus.Failed;
            }
            else if (status == DeliveryInfoStatus.Submitted)
            {
                return DeliveryStatus.Submitted;
            }
            else if (status == DeliveryInfoStatus.OrderPicked)
            {
                return DeliveryStatus.OrderPicked;
            }
            else if (status == DeliveryInfoStatus.OrderInTransit)
            {
                return DeliveryStatus.OrderInTransit;
            }
            else
            {
                throw new Exception("Unexpected delivery status received");
            }
        }

    }
}