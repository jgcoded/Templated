
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;

namespace Templated;

public class TemplatedScript
{
    private readonly static byte[] ScriptHashKey = new byte[] { 0xab, 0xcd, 0xef, 0xff };

    private readonly Type type;

    internal TemplatedScript(Type type)
    {
        this.type = type;
    }

    public static TemplatedScript Create(string razorScript)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("@inherits Templated.TemplatedExecutionContext");
        builder.Append(razorScript);
        razorScript = builder.ToString();

        // var hash = new HMACSHA256(ScriptHashKey);
        // string scriptName = Encoding.UTF8.GetString(hash.ComputeHash(Encoding.UTF8.GetBytes(razorScript)));
        string scriptName = Path.GetRandomFileName();

        // Adapted from https://stackoverflow.com/questions/38247080/using-razor-outside-of-mvc-in-net-core?answertab=trending#tab-top
        var fs = RazorProjectFileSystem.Create(".");
        var engine = RazorProjectEngine.Create(RazorConfiguration.Default, fs,
            builder =>
            {
                builder.SetNamespace("TemplatedScript");
            });

        // Can instead obtain the script from the filesystem
        // RazorProjectItem item = fs.GetItem("TestScript.cshtml");

        var source = RazorSourceDocument.Create(razorScript, Path.GetRandomFileName());
        RazorCodeDocument document = engine.Process(
            source,
            FileKinds.Legacy /* mvc */,
            new List<RazorSourceDocument>(),
            new List<TagHelperDescriptor>());
        RazorCSharpDocument csDocument = document.GetCSharpDocument();
        // Console.WriteLine(csDocument.GeneratedCode);

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csDocument.GeneratedCode);
        CSharpCompilation compilation = CSharpCompilation.Create(scriptName,
        new[] { syntaxTree },
        // Adapted from https://github.com/adoconnection/RazorEngineCore/blob/master/RazorEngineCore/RazorEngine.cs
        new List<MetadataReference> {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RazorCompiledItemAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "", "netstandard.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "", "System.Runtime.dll"))
        },
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var memoryStream = new MemoryStream())
        {
            EmitResult result = compilation.Emit(memoryStream);

            // Could write out the DLL to a file
            // EmitResult result = compilation.Emit(scriptName + ".dll");

            // Console.WriteLine(typeof(object).Assembly.Location);
            // Console.WriteLine(Assembly.GetExecutingAssembly().Location);
            if (!result.Success)
            {
                // Todo use a package like ErrorOr or OneOf
                // For Rust-style Result error handling instead of exceptions
                throw new Exception(string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString())));
            }

            Assembly assembly = Assembly.Load(memoryStream.ToArray());

            Type? type = assembly.GetType("TemplatedScript.Template");
            if (type == null)
            {
                throw new Exception("Could not get type");
            }

            return new TemplatedScript(type);
        }

        throw new Exception("Failed creating TemplatedExecutionContext");
    }

    public async Task<string> RunAsync(Dictionary<string, string> model)
    {
        object? instance = Activator.CreateInstance(type);
        if (instance == null)
        {
            throw new Exception("Could not create instance of TemplatedExecutionContext");
        }

        var context = instance as TemplatedExecutionContext;
        if (context == null)
        {
            throw new Exception("Could not cast to TemplatedExecutionContext");
        }

        context.Model = model;
        await context.ExecuteAsync();

        return context.Result;
    }
}
