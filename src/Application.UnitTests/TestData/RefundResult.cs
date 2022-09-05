using GovUKPayApiClient.Model;

namespace Application.UnitTests
{
    public partial class TestData
    {
        public static Refund GetSuccessfulRefundResult(string refundId)
        {
            var deserialisedRefundResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Refund>($"{{\"refund_id\":\"{refundId}\", \"status\":2}}");

            return deserialisedRefundResult;
        }
    }
}
