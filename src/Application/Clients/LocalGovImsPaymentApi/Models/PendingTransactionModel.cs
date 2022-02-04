using System;
using System.Diagnostics.CodeAnalysis;

namespace Application.Clients.LocalGovImsPaymentApi
{
    [ExcludeFromCodeCoverage]
    public class PendingTransactionModel
    {
        public int Id { get; set; }
        public string Reference { get; set; }
        public string InternalReference { get; set; }
        public string OfficeCode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string AccountReference { get; set; }
        public int UserCode { get; set; }
        public string FundCode { get; set; }
        public string MopCode { get; set; }
        public decimal? Amount { get; set; }
        public string VatCode { get; set; }
        public float VatRate { get; set; }
        public decimal? VatAmount { get; set; }
        public string Narrative { get; set; }
        public string BatchReference { get; set; }
        public string ExternalReference { get; set; }
        public string PayeeName { get; set; }
        public string PayeeBusinessName { get; set; }
        public string PayeePremiseNumber { get; set; }
        public string PayeePremiseName { get; set; }
        public string PayeeStreet { get; set; }
        public string PayeeArea { get; set; }
        public string PayeeTown { get; set; }
        public string PayeeCounty { get; set; }
        public string PayeePostCode { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        public string FailUrl { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int StatusId { get; set; }

        public PendingTransactionModel() { }
    }
}
