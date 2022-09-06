namespace Application.Commands
{
    public class ProcessIncompleteRefundsCommandResult
    {
        public int TotalIdentified { get; set; }
        public int TotalProcessed { get; set; }
        public int TotalErrors { get; set; }
    }
}
