using BookStore.Business.Components.Interfaces;
using BookStore.Services.Interfaces;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Services
{
    class FundsTransferService : IFundsTransferService
    {
        public IFundsTransferProvider Provider
        {
            get
            {
                return ServiceLocator.Current.GetInstance<IFundsTransferProvider>();
            }
        }

        public void TransferOutcome(bool pOutcome, string Oid)
        {
            Provider.TransferOutcome(pOutcome, Oid);
        }

        public void RefundOutcome(bool pOutcome, string Oid)
        {
            Provider.RefundOutcome(pOutcome, Oid);
        }
    }
}
