# Native AOT verification

## Verified locally

A .NET 8 Native AOT executable was published for **osx-x64** using installed SDK **8.0.404** and Apple's Command Line Tools clang. The binary ran successfully with `--require-native`, confirming dynamic code support was disabled and reflection-based JSON serialization was off.

The offline smoke covers DI, bearer request construction, generated optional fields and explicit nulls, request/response unions, recursive template/campaign/segment content, structured errors, pagination, multipart uploads and CSV downloads. Its HttpMessageHandler is fake; it performs no live network calls to Kirora. It is a smoke test, not exhaustive verification of all 183 endpoints under AOT.

```sh
dotnet publish examples/Kirora.NativeAotSmoke --configuration Release --runtime osx-x64 \
  -p:PublishAot=true -p:TrimmerSingleWarn=false \
  -p:EnableTrimAnalyzer=true -p:EnableAotAnalyzer=true --output artifacts/aot
artifacts/aot/Kirora.NativeAotSmoke --require-native
```

The prepared CI uses linux-x64 with clang/zlib. That GitHub job has not been executed during this session. Windows AOT, Linux AOT and consumer-defined custom models are not claimed as verified.

## Reflection and serialization changes

SDK-generated Optional<T> converters are now statically registered for all SDK field types. Generated union converters and the HTTP transport use JsonTypeInfo overloads rather than reflection-oriented serializer overloads. Request validation parses JsonNode text into JsonDocument instead of invoking an untyped reflection serializer.

The public Optional<T> remains usable for consumer-defined types in normal JIT applications. Its narrowly scoped fallback uses MakeGenericType/Activator only behind RuntimeFeature.IsDynamicCodeSupported. The factory has documented, localized IL2026/IL3050 suppressions for that JIT-only branch. Native AOT rejects unregistered consumer-defined Optional types; a consumer must explicitly register/preserve its own converter and metadata. No blanket trimming/AOT warning suppression was added.

## Warnings and limits

The local native publish emitted:

- **IL3000 in the .NET 8 framework's System.Net.Quic.MsQuicApi**, concerning Assembly.Location in a single-file executable. It was not an SDK serializer/reflection warning. The offline HTTP handler does not test real QUIC/HTTP3 networking.
- Apple's linker warning that **-ld_classic is deprecated**.

No SDK-origin IL2026/IL3050 warnings remained after the static metadata changes and the documented JIT-only guard. Both upstream/toolchain warnings remain disclosed; verification is not described as warning-free.

The installed SDK selects its known .NET 8 runtime/compiler packs (8.0.11). This is an offline compatibility probe, not a recommendation to deploy that older bundled runtime. CI selects an available serviced 8.0.x SDK. Runtime/OS servicing and platform certification remain release responsibilities. The library itself still targets net8.0 and does not declare blanket IsAotCompatible support based on a single smoke.

## New-contract readiness validation

The smoke was repeated after migration to upstream commit 30043317efd68ffceeab882019431e57b7463018, using .NET SDK 8.0.404 and Apple clang 21 on osx-x64. Native publish and --require-native execution passed. The harness now also calls the new brand-kit and journey activation-check endpoints and serializes structured transactional delivery, using fake HTTP only. Reflection JSON is disabled and the native-mode assertion checks dynamic code support is unavailable. The same framework IL3000 and Apple linker warnings remain. No live Native AOT network test or broader-platform verification is claimed. See REPOSITORY-READINESS.md for logs/check commands and other release blockers.
