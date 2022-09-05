namespace Application.Commands
{
    public class RefundRequestCommandResult
    {
        public string PspReference { get; set; }
        public decimal? Amount { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }

        public static RefundRequestCommandResult Successful(string pspReference, decimal amount)
        {
            return new RefundRequestCommandResult()
            {
                Success = true,
                Amount = amount,
                PspReference = pspReference
            };
        }

        public static RefundRequestCommandResult Failure(string message)
        {
            return new RefundRequestCommandResult()
            {
                Success = false,
                Message = message
            };
        }
    }
}
