using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Entities;

namespace BookStore.Business.Components.Interfaces
{
    public interface IDeliveryNotificationProvider
    {
        void NotifyDeliveryCompletion(Guid pDeliveryId, DeliveryStatus status);
    }
}
