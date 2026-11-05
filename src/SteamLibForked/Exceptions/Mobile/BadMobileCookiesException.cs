namespace SteamLib.Exceptions.Mobile;

[Serializable]
public class BadMobileCookiesException : Exception
{

    public BadMobileCookiesException() : base(
        "You are using default HttpClient without mobile specific cookies. Login can't be proceeded with these cookies")
    {
    }

    public BadMobileCookiesException(string message) : base(message)
    {
    }

    public BadMobileCookiesException(string message, Exception inner) : base(message, inner)
    {
    }
}