
using System.Text;

namespace Templated;

// adapted from https://github.com/dotnet/aspnetcore/blob/e059629e5e8ce395d24a9a4582772ce88be766dc/src/Shared/RazorViews/BaseView.cs
public abstract class TemplatedExecutionContext
{
    public IDictionary<string, string> Model { get; set; }

    // TODO: perf analysis on StringBuilder vs MemoryStream+StreamWriter
    StringBuilder stringBuilder;

    public string Result => stringBuilder.ToString();

    // For performance don't new up the Model dictionary in the constructor
    // as TemplatedScript will assign an instance before calling ExecuteAsync().
    #pragma warning disable CS8618
    public TemplatedExecutionContext()
    {
        this.stringBuilder = new StringBuilder();
    }

    protected void WriteLiteral(string literal)
    {
        stringBuilder.Append(literal);
        // Console.WriteLine(literal);
    }

    protected void Write(object obj)
    {
        stringBuilder.Append(obj);
        // Console.WriteLine(obj);
    }

    // The compiled razor script is a class that
    // inherits from TemplatedExecutionContext
    // TemplatedScript calls ExecuteAsync on the
    // derived class.
    public abstract Task ExecuteAsync();
}
