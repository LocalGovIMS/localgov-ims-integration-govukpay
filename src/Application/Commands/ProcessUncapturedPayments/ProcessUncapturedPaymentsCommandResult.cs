namespace Application.Commands
{
    public class ProcessUncapturedPaymentsCommandResult
    {
        public int TotalIdentified { get; set; }
        public int TotalMarkedAsCaptured { get; set; }
        public int TotalErrors { get; set; }
    }
}
