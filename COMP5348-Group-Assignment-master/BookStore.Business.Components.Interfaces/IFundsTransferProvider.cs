using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Business.Components.Interfaces
{
    public interface IFundsTransferProvider
    {
        void TransferOutcome(bool pOutcome, string Oid);
        void RefundOutcome(bool pOutcome, string Oid);
    }
}
