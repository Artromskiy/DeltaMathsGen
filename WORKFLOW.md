# MathsGen workflow

From the workspace root:

```bash
dotnet build MathsGen/Delta.MathsGen.csproj -c Release \
  --disable-build-servers -m:1 /p:UseSharedCompilation=false
dotnet MathsGen/bin/Release/net8.0/Delta.MathsGen.dll Maths/Vectors
dotnet build Maths/Delta.Maths.csproj -c Release -f netstandard2.0
dotnet build Maths/Delta.Maths.csproj -c Release -f netstandard2.1
dotnet run --project Maths/Tests/Delta.Maths.Tests.csproj -c Release
git -C Maths diff --check
```

Run the generator a second time; it must produce no additional diff. Inspect
the generated manifest and files before committing. Cross-project verification
uses [../REVIEW_PLAYBOOK.md](../REVIEW_PLAYBOOK.md).
