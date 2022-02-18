namespace Application.Commands
{
    public class CleanupIncompletePaymentsCommandResult
    {
        public int TotalIdentified { get; set; }
        public int TotalClosed { get; set; }
        public int TotalErrors { get; set; }
    }
}
