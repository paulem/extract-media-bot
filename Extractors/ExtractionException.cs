using System.Runtime.Serialization;

namespace Telegram.ExtractMediaBot.Extractors;

[Serializable]
public class ExtractionException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public ExtractionException()
    {
    }

    public ExtractionException(string message) : base(message)
    {
    }

    public ExtractionException(string message, Exception inner) : base(message, inner)
    {
    }

    protected ExtractionException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}