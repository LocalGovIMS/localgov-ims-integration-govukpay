namespace Application.Extensions
{
    public static class DecimalExtensions
    {
        public static int ToPence(this decimal source)
        {
            return (int)(source * 100);
        }
    }
}
