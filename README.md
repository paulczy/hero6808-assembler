# Hero6808

A cross assembler for Motorola 6800/6808-style code, focused on Heathkit HERO 1 and HERO Jr workflows.

Current status:
- .NET 10 solution: `Hero6808.slnx`
- Test framework: TUnit
- Output format: Motorola S19
- Corpus-backed golden test: `tests/corpus/local-gotahero`

## Quick Start (Release ZIP)

### 1. Download
- Open the latest release on GitHub.
- Download the ZIP for your platform:
  - `Hero6808-win-x64.zip`
  - `Hero6808-osx-arm64.zip` (Apple Silicon)
  - `Hero6808-osx-x64.zip` (Apple Intel)

### 2. Extract
- Extract the ZIP to a folder you control.

### 3. Run the CLI

Windows (`win-x64`):
```powershell
.\Hero6808.Cli.exe assemble .\input.asm -o .\output.s19
```

macOS Intel (`osx-x64`) and Apple Silicon (`osx-arm64`):
```bash
xattr -dr com.apple.quarantine ./Hero6808.Cli
chmod +x ./Hero6808.Cli
./Hero6808.Cli assemble ./input.asm -o ./output.s19
```

If macOS shows "Apple could not verify it is free of malware", run the `xattr` command above once on the extracted binary, then run it again.

CLI usage:
```text
Hero6808.Cli assemble <input.asm> -o <output.s19>
```

## Quick Start (Build from Source)

### 1. Prerequisites
- .NET SDK 10.x

### 2. Build
```powershell
dotnet build .\Hero6808.slnx -c Debug
```

### 3. Test
Use the .NET 10 Microsoft.Testing.Platform flow:
```powershell
dotnet test --solution .\Hero6808.slnx
```

### 4. Assemble a file
```powershell
dotnet run --project .\src\Hero6808.Cli\Hero6808.Cli.csproj -- assemble .\tests\corpus\local-gotahero\GotAHero.asm -o .\out.s19
```

### 5. Verify output against corpus sample (optional)
```powershell
Compare-Object (Get-Content .\tests\corpus\local-gotahero\GOTAHERO.S19) (Get-Content .\out.s19)
```
If there is no output from `Compare-Object`, the files match.

## Packaging
Create self-contained zip artifacts:
```powershell
.\scripts\package.ps1 -Configuration Release -RuntimeIdentifiers win-x64,osx-x64,osx-arm64
```

Output:
- `artifacts\packages\Hero6808-win-x64.zip`
- `artifacts\packages\Hero6808-osx-x64.zip`
- `artifacts\packages\Hero6808-osx-arm64.zip`

## CI
- Build/test workflow: `.github/workflows/ci.yml`
- Packaging workflow: `.github/workflows/package.yml`

## Test Data Attribution
Assembler validation in this project uses local/external `.asm` and `.s19` test data sourced from:

- `https://github.com/petermcd1010/heathkit_hero_1_programs`
- `https://gotabot.weebly.com/`

These sources are used to validate compatibility and output correctness. Test inputs from those sources are treated as source-attributed data and are not re-licensed by this project.

## Project Layout
- `src/Hero6808.Core` - assembler core, parser, expression evaluator, opcode encoding, S19 writer
- `src/Hero6808.Cli` - command-line interface (`assemble`)
- `tests/Hero6808.Tests` - TUnit test suite
- `tests/corpus/local-gotahero` - source/expected S19 golden corpus
- `tests/corpus/heathkit_hero_1_programs` - paired `.asm/.s19` compatibility corpus

## Troubleshooting

### `dotnet test` reports VSTest is no longer supported on .NET 10
Symptom:
- Error similar to: `Testing with VSTest target is no longer supported ...`

Fix:
- Keep `global.json` in repo root with Microsoft Testing Platform configured.
- Run tests using:
```powershell
dotnet test --solution .\Hero6808.slnx
```

### `dotnet test` says solution must be specified via `--solution`
Symptom:
- Error similar to: `Specifying a solution for 'dotnet test' should be via '--solution'`

Fix:
- Use:
```powershell
dotnet test --solution .\Hero6808.slnx
```

### `dotnet build` fails with `Unknown switch --solution`
Symptom:
- Error similar to: `MSBUILD : error MSB1001: Unknown switch`

Fix:
- For build, pass the solution as a positional argument:
```powershell
dotnet build .\Hero6808.slnx -c Debug
```


