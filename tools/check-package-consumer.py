#!/usr/bin/env python3
"""Install the local package into a separate net8.0 application and exercise only fake HTTP."""
import json,shutil,subprocess,tempfile,xml.etree.ElementTree as E
from pathlib import Path
root=Path(__file__).resolve().parents[1]
config=json.loads((root/'eng/release.json').read_text());package=root/'src/Kirora/bin/Release'/f"{config['packageId']}.{config['version']}.nupkg"
if not package.is_file():raise SystemExit('Pack the SDK before running the independent consumer check')
with tempfile.TemporaryDirectory(prefix='kirora-consumer-') as temporary:
 directory=Path(temporary);feed=directory/'feed';feed.mkdir();shutil.copy2(package,feed/package.name)
 # An isolated package cache ensures the exact newly packed SDK is consumed rather than a stale same-version package.
 settings=E.Element('configuration');sources=E.SubElement(settings,'packageSources');E.SubElement(sources,'clear')
 E.SubElement(sources,'add',key='local',value=str(feed));E.SubElement(sources,'add',key='nuget.org',value='https://api.nuget.org/v3/index.json')
 E.ElementTree(settings).write(directory/'NuGet.Config',encoding='unicode')
 shutil.copy2(root/'global.json',directory/'global.json')
 shutil.copy2(root/'tests/Kirora.Tests/Fixtures/core-examples.json',directory/'core-examples.json')
 project=E.Element('Project',Sdk='Microsoft.NET.Sdk');props=E.SubElement(project,'PropertyGroup')
 for key,value in {'OutputType':'Exe','TargetFramework':'net8.0','ImplicitUsings':'enable','Nullable':'enable','TreatWarningsAsErrors':'true','RestorePackagesPath':str(directory/'packages')}.items():E.SubElement(props,key).text=value
 references=E.SubElement(project,'ItemGroup');E.SubElement(references,'PackageReference',Include=config['packageId'],Version=config['version'])
 E.ElementTree(project).write(directory/'Consumer.csproj',encoding='unicode')
 (directory/'Program.cs').write_text('''using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kirora;
using Kirora.Models;
using Kirora.Serialization;

if (Environment.Version.Major != 8) throw new InvalidOperationException("Expected .NET 8 runtime");
var fixtures = JsonNode.Parse(File.ReadAllText("core-examples.json"))!.AsArray();
using var http = new HttpClient(new OfflineHandler(fixtures));
var client = new KiroraClient(http, new KiroraOptions { ApiKey = "fake-package-consumer-key", MaxRetries = 0 });
var brand = await client.Templates.GetBrandKitAsync();
if (brand.Data.Theme is null) throw new InvalidOperationException("Missing new brand-kit contract");
await client.Journeys.GetActivationCheckAsync("0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d");
var fallbackOrder = new[] { "push", "email" };
SendTransactionalInputEmail email = new SendTransactionalInputEmailStoredTemplate { Template = "order-receipt" };
await client.Transactional.SendAsync(new SendTransactionalInput {
    To = new TransactionalRecipientInputByExternalId { ExternalId = "sdk-offline-consumer" }, Email = email, Push = new TransactionalPushInput { Title = "Offline", Body = "Offline fixture" }, FallbackOrder = fallbackOrder });
var social = JsonSerializer.Deserialize<SocialBlockInput>("{\\"id\\":\\"social\\",\\"type\\":\\"social\\"}")!;
if (social.Links.IsSet) throw new InvalidOperationException("Optional links became required");
Console.WriteLine("Independent .NET 8 NuGet consumer passed: new endpoints, fallback order and optional links. No live HTTP.");

sealed class OfflineHandler(JsonArray fixtures) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var operation = request.RequestUri!.AbsolutePath switch {
            "/v1/brand-kit" => "getBrandKit",
            "/v1/transactional/messages" => "sendTransactionalMessage",
            "/v1/journeys/0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d/activation-check" => "getJourneyActivationCheck",
            _ => throw new InvalidOperationException("Unexpected offline request") };
        if (operation == "sendTransactionalMessage") {
            var node = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            if (!JsonNode.DeepEquals(node["fallbackOrder"], JsonNode.Parse("""["push","email"]"""))) throw new InvalidOperationException("Wrong fallback order JSON");
        }
        var fixture = fixtures.First(f => f!["operationId"]!.GetValue<string>() == operation)!;
        return new HttpResponseMessage((HttpStatusCode)fixture["status"]!.GetValue<int>()) { Content = new StringContent(fixture["response"]!.ToJsonString()) };
    }
}
''')
 subprocess.run(['dotnet','restore','Consumer.csproj'],cwd=directory,check=True)
 subprocess.run(['dotnet','run','--project','Consumer.csproj','--configuration','Release','--no-restore'],cwd=directory,check=True)
 assets=json.loads((directory/'obj/project.assets.json').read_text());assert any(key==config['packageId']+'/'+config['version'] for key in assets['libraries'])
 out=root/'artifacts/package-consumer.json';out.parent.mkdir(exist_ok=True);out.write_text(json.dumps({'package':package.name,'targetFramework':'net8.0','isolatedPackageCache':True,'projectReferences':False,'passed':True,'liveHttp':False},indent=2)+'\n')
