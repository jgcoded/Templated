namespace Tests;

using Templated;

[TestClass]
public class RazorTests
{
    [TestMethod]
    public async Task CreateTestScriptDLLTests()
    {
        TemplatedScript script = TemplatedScript.Create(@"
        {
            ""hello"": ""@Model[""prop""]""
        }
        ");

        string result = await script.RunAsync(new Dictionary<string, string>{
            { "prop", "world"}
        });

        Assert.AreEqual(@"
        {
            ""hello"": ""world""
        }
        ", result);
    }
}