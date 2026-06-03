using System.Collections.Generic;

namespace ClubMembership.Models
{
    public class OmniwarePaymentRedirectViewModel
    {
        public string ActionUrl { get; set; }

        public IDictionary<string, string> Fields { get; set; }
    }

    public class OmniwarePaymentResultViewModel
    {
        public bool IsSuccess { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public string OrderId { get; set; }

        public string TransactionId { get; set; }

        public string ResponseCode { get; set; }

        public string PaymentMode { get; set; }

        public string PaymentChannel { get; set; }

        public string PaymentDatetime { get; set; }
    }
}
