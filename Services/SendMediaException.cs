using System.Runtime.Serialization;

namespace Telegram.ExtractMediaBot.Services;

[Serializable]
public class SendMediaException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public SendMediaException()
    {
    }

    public SendMediaException(string message) : base(message)
    {
    }

    public SendMediaException(string message, Exception inner) : base(message, inner)
    {
    }

    protected SendMediaException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}