# MathsGen workflow

From the workspace root:

```bash
dotnet build MathsGen/Delta.MathsGen.csproj -c Release \
  --disable-build-servers -m:1 /p:UseSharedCompilation=false
dotnet MathsGen/bin/Release/net8.0/Delta.MathsGen.dll Maths/Vectors
dotnet MathsGen/bin/Release/net8.0/Delta.MathsGen.dll Maths/Vectors
dotnet build Maths/Delta.Maths.csproj -c Release -f netstandard2.0
dotnet build Maths/Delta.Maths.csproj -c Release -f netstandard2.1
dotnet run --project Maths/Tests/Delta.Maths.Tests.csproj -c Release
git -C Maths diff --check
```

The second invocation must produce no additional diff. Inspect
`.delta-generated-files` and validate the generated
`Maths/Vectors/shader-contract.json` schema/layout before committing.
Cross-project verification uses
[../REVIEW_PLAYBOOK.md](../REVIEW_PLAYBOOK.md).

## Code metrics

Run the manual GitHub Actions `Code metrics` workflow before committing a
substantial change, then inspect its SARIF and summary artifacts. The rules
CA1501/CA1502/CA1505/CA1506 are report-only signals; do not refactor a method
for one isolated warning. Refactor when several metrics remain over their
limits, the issue persists across runs, or profiling identifies a hot path.

For local application run `./eng/format.sh`; for a non-mutating check use
`FORMAT_CHECK=1 ./eng/format.sh`. The script uses `dotnet format whitespace
--folder` to avoid the MSBuild/Roslyn workspace load that can hang on macOS
with .NET 10. It checks/applies whitespace only; analyzer/style diagnostics
remain covered by the build and SARIF metrics workflow.
