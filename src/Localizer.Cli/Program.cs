using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Localizer.Cli;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
            name: "--file",
            description: "The file to read and display on the console.");

        var azureKey = new Option<string?>(name: "--apikey", description: "The api key for Azure Translator");
        var region = new Option<string?>(name: "--region", description: "The api rgion for Azure Translator");
        var fromLang = new Option<string?>(name: "--from", description: "From language code");
        var toLang = new Option<string?>(name: "--to", description: "To language code");

        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(azureKey);
        rootCommand.AddOption(region);
        rootCommand.AddOption(fromLang);
        rootCommand.AddOption(toLang);
        rootCommand.SetHandler(ReadFile, fileOption, azureKey, region, fromLang, toLang);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task ReadFile(
        FileInfo? file,
        string? apiKey,
        string? apiRegion,
        string? fromLang,
        string? toLang)
    {
        if (file is null ||
            apiKey is null ||
            apiRegion is null ||
            fromLang is null ||
            toLang is null)
        {
            return;
        }

        var doc = new XmlDocument();
        doc.Load(file.FullName);
        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("x", "urn:oasis:names:tc:xliff:document:1.2");

        XmlNodeList? untranslatedNodes = doc.SelectNodes("x:xliff/x:file/x:body/x:group/x:trans-unit/x:target[@state='new']", nsmgr);
        if (untranslatedNodes is XmlNodeList nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is XmlNode node)
                {
                    string old = node.InnerText;
                    var translation = await TranslateAsync(
                        old,
                        fromLang,
                        toLang,
                        apiKey,
                        apiRegion);

                    if (old != translation)
                    {
                        var item = node.Attributes?.GetNamedItem("state");
                        if (item is not null) item.Value = "translated";
                        node.InnerText = translation;
                    }
                }
            }

            doc.Save(file.FullName);
        }
    }

    private static readonly string translateTemplate = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={0}&to={1}";

    private static async Task<string> TranslateAsync(string text, string fromLang, string toLang, string apiKey, string apiRegion)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string url = string.Format(translateTemplate, fromLang, toLang);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Headers.Add("Ocp-Apim-Subscription-Region", apiRegion);

        var payload = new List<PostBody>()
        {
            new PostBody()
            {
                Text = text
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var rawResponse = await client.SendAsync(request);
            var content = await rawResponse.Content.ReadAsStringAsync();
            if (rawResponse.IsSuccessStatusCode)
            {
                var response = JsonSerializer.Deserialize<Response[]>(content);
                return response?.FirstOrDefault()?.translations.FirstOrDefault()?.text ?? text;
            }
        }
        catch (Exception)
        {

        }


        return text;
    }
}

internal class PostBody
{
    public string Text { get; set; } = string.Empty;
}

internal class Response
{
    public Translated[] translations { get; set; } = Array.Empty<Translated>();
}

internal class Translated
{
    public string text { get; set; } = string.Empty;
    public string to { get; set; } = string.Empty;
}