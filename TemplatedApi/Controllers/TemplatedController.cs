using Microsoft.AspNetCore.Mvc;
using Templated;

namespace TemplatedApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TemplatedController : ControllerBase
{
    private readonly ILogger<TemplatedController> logger;

    public TemplatedController(ILogger<TemplatedController> logger)
    {
        this.logger = logger;
    }

    // Outputs query params in the URL as JSON key/value pairs
    // Sample: /templated?hello=world&foo=bar
    [HttpGet(Name = "OutputQueryParams")]
    public async Task<ContentResult> Get()
    {
        this.logger.Log(LogLevel.Debug, "Executing Script");

        var model = new Dictionary<string, string>();
        foreach (var query in this.Request.Query)
        {
            model.Add(query.Key.ToString(), query.Value.ToString());
        }

        // https://docs.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-6.0
        TemplatedScript script = TemplatedScript.Create(@"{
            @{
                bool first = true;
                foreach(var item in @Model)
                {
                    if (!first)
                    {
                        <text>,</text>
                    }
                    <text>""@item.Key"": ""@item.Value""</text>
                    first = false;
                }
            }
        }
        ");

        string result = await script.RunAsync(model);

        return Content(result, "application/json");
    }
}