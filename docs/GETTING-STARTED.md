# Getting started

The library targets net8.0. Install a .NET 8 SDK/runtime; global.json selects a compatible 8.x SDK. Examples build with the solution. Read the README first; advanced behavior belongs in SDK-USAGE.md.

## Local package installation

Until an official version is approved and published, use the locally packed archive. From the SDK checkout run `dotnet pack src/Kirora/Kirora.csproj --configuration Release`. In your separate application's NuGet.Config, include the SDK output alongside nuget.org:

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="kirora-local" value="/absolute/path/to/kirora-dotnet/src/Kirora/bin/Release" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="kirora-local"><package pattern="Kirora" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
  <config><add key="globalPackagesFolder" value=".packages" /></config>
</configuration>
```

Replace the absolute path. Then add Kirora 0.1.0-alpha.1 as a PackageReference and restore. Do not commit the project-owned cache. If rebuilding an unpublished package with the same version, remove only the stale project-owned Kirora/version cache entry and run `dotnet restore --force-evaluate`; a global cache may otherwise retain the previous binary. Published versions must be immutable. `tools/check-package-consumer.py` verifies an independent installation from the local archive into a fresh cache, without a project reference or live HTTP.

For development inside this SDK repository, example/test projects legitimately reference the library project. This is different from testing the packed NuGet artifact. No local Playground or developer cache is part of the SDK package.

## Credentials and environments

The SDK does not load environment variables or dotenv files automatically. The README explicitly reads KIRORA_API_KEY and applies optional KIRORA_BASE_URL. Do not put a key in a command argument, source file, launch.json or log. On macOS zsh, use a hidden prompt in the same terminal that runs your app:

```sh
read -r -s 'KIRORA_API_KEY?Kirora API key: '
printf '\n'
export KIRORA_API_KEY
export KIRORA_BASE_URL='https://kirora-staging-backend.brimble.app'
```

The current staging hostname was supplied by Kirora; the SDK's production default remains https://api.kirora.io/. The production hostname is not yet configured according to the team. Set the correct workspace's key, use HTTPS without a path/query/user information, and never disable TLS verification to work around an environment problem. SDK-generated /v1 paths are combined with the configured origin; never append /v1 to BaseAddress.

A shell child (`dotnet run`) inherits exported terminal variables. VS Code's F5 debuggee uses the editor/debug configuration environment; a later export in its integrated terminal does not retrofit the editor's environment. Configure a secure local debugger environment or masked input. Never dump environment variables to diagnose availability; print only presence/absence.

## Examples and test boundaries

- examples/Kirora.Examples: the complete README console; it makes GET /v1/me only when you deliberately run it with credentials. Resource helper methods compile but are not called by default.
- examples/Kirora.AspNetCore: the exact README typed-client registration and local read-only check endpoint.
- examples/Kirora.Worker: background processing example; inspect it before running with credentials.
- examples/Kirora.NativeAotSmoke: fake-HTTP smoke only; see NATIVE-AOT.md for verified platform limits.

Every README C# block is tracked by tools/check-readme.py and compiled with the existing examples. The helper never runs them. Mutation examples are instructions for an approved test workspace, not authorization for an agent or CI to send messages.

## Staging verification checklist

First run restore, Release build/tests, generation/API/README checks, pack and package validation with both live run flags set to 0. Verify current model migration guidance before updating a local consumer. Use a correct staging key and an approved workspace, perform a separately authorized GET /v1/me, then contacts/list reads before any mutation. The opt-in live suite has additional exclusive-workspace, recipient approval and cleanup requirements; see INTEGRATION-TESTS.md. Acceptance of a message does not establish delivery. Report live results separately from offline counts.
