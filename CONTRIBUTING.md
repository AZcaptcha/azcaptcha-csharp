# Contributing

1. Read the contract in [AZcaptcha/sdk-spec](https://github.com/AZcaptcha/sdk-spec).
2. Fixtures in `tests/AZcaptcha.Sdk.Tests/fixtures/` are copied from the spec repo;
   a fixture change is a spec change — open the issue there first.
3. Run the suite:

```bash
dotnet test
dotnet pack src/AZcaptcha.Sdk/AZcaptcha.Sdk.csproj -c Release
```

4. Keep changes scoped; add a test for every behaviour change.
5. Releases are cut from tags `vX.Y.Z`; CI pushes the NuGet package.
