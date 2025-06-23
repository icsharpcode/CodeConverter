
internal partial class TestClass
{
    public static string GetTextFeedInput(string pStream, string pTitle, string pText)
    {
        return "{" + AccessKey() + ",\"streamName\": \"" + pStream + "\",\"point\": [" + GetTitleTextPair(pTitle, pText) + "]}";
    }

    public static string AccessKey()
    {
        return "\"accessKey\": \"8iaiHNZpNbBkYHHGbMNiHhAp4uPPyQke\"";
    }

    public static string GetNameValuePair(string pName, int pValue)
    {
        return "{\"name\": \"" + pName + "\", \"value\": \"" + pValue + "\"}";
    }

    public static string GetNameValuePair(string pName, string pValue)
    {
        return "{\"name\": \"" + pName + "\", \"value\": \"" + pValue + "\"}";
    }

    public static string GetTitleTextPair(string pName, string pValue)
    {
        return "{\"title\": \"" + pName + "\", \"msg\": \"" + pValue + "\"}";
    }
    public static string GetDeltaPoint(int pDelta)
    {
        return "{\"delta\": \"" + pDelta + "\"}";
    }
}