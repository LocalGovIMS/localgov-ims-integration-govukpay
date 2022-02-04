namespace Application.Cryptography
{
    public interface ICryptographyService
    {
        string GetHash(string input);
    }
}