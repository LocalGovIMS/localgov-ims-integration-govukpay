using LocalGovImsApiClient.Model;
using System;

namespace Application.UnitTests
{
    public partial class TestData
    {
        public static PendingTransactionModel GetPendingTransactionModel()
        {
            return new PendingTransactionModel()
            {
                AccountReference = "AccountReference",
                Amount = 1.00M,
                BatchReference = "BatchReference",
                CancelUrl = "CancelUrl",
                CreatedDate = DateTime.Now,
                ExternalReference = "ExternalReference",
                FailUrl = "FailUrl",
                FundCode = "FF",
                Id = 1,
                InternalReference = "InternalReference",
                MopCode = "MM",
                Narrative = "Narrative",
                OfficeCode = "OO",
                PayeeArea = "PayeeArea",
                PayeeBusinessName = "PayeeBusinessName",
                PayeeCounty = "PayeeCounty",
                PayeeName = "PayeeName",
                PayeePostCode = "PayeePostCode",
                PayeePremiseName = "PayeePremiseName",
                PayeePremiseNumber = "PayeePremiseNumber",
                PayeeStreet = "PayeeStreet",
                PayeeTown = "PayeeTown",
                Reference = "Reference",
                StatusId = 1,
                SuccessUrl = "SuccessUrl",
                TransactionDate = DateTime.Now,
                UserCode = 1,
                VatAmount = 0.20M,
                VatCode = "VV",
                VatRate = 20F
            };
        }
    }
}
