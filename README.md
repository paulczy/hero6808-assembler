# Asm6808

A cross assembler for Motorola 6800/6808-style code, focused on Heathkit HERO 1 and HERO Jr workflows.

Current status:
- .NET 10 solution: `6808Asm.slnx`
- Test framework: TUnit
- Output format: Motorola S19
- Corpus-backed golden test: `tests/corpus/local-gotahero`

## Quick Start

### 1. Prerequisites
- .NET SDK 10.x

### 2. Build
```powershell
dotnet build .\6808Asm.slnx -c Debug
```

### 3. Test
Use the .NET 10 Microsoft.Testing.Platform flow:
```powershell
dotnet test --solution .\6808Asm.slnx
```

### 4. Assemble a file
```powershell
dotnet run --project .\src\Asm6808.Cli\Asm6808.Cli.csproj -- assemble .\tests\corpus\local-gotahero\GotAHero.asm -o .\out.s19
```

### 5. Verify output against corpus sample (optional)
```powershell
Compare-Object (Get-Content .\tests\corpus\local-gotahero\GOTAHERO.S19) (Get-Content .\out.s19)
```
If there is no output from `Compare-Object`, the files match.

## Packaging
Create self-contained zip artifacts:
```powershell
.\scripts\package.ps1 -Configuration Release -RuntimeIdentifiers win-x64,osx-x64
```

Output:
- `artifacts\packages\asm6808-win-x64.zip`
- `artifacts\packages\asm6808-osx-x64.zip`

## CI
- Build/test workflow: `.github/workflows/ci.yml`
- Packaging workflow: `.github/workflows/package.yml`

## Test Data Attribution
Assembler validation in this project uses local/external `.asm` and `.s19` test data sourced from:

- `https://github.com/petermcd1010/heathkit_hero_1_programs`
- `https://gotabot.weebly.com/`

These sources are used to validate compatibility and output correctness. Test inputs from those sources are treated as source-attributed data and are not re-licensed by this project.

## Project Layout
- `src/Asm6808.Core` - assembler core, parser, expression evaluator, opcode encoding, S19 writer
- `src/Asm6808.Cli` - command-line interface (`assemble`)
- `tests/Asm6808.Tests` - TUnit test suite
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
dotnet test --solution .\6808Asm.slnx
```

### `dotnet test` says solution must be specified via `--solution`
Symptom:
- Error similar to: `Specifying a solution for 'dotnet test' should be via '--solution'`

Fix:
- Use:
```powershell
dotnet test --solution .\6808Asm.slnx
```

### `dotnet build` fails with `Unknown switch --solution`
Symptom:
- Error similar to: `MSBUILD : error MSB1001: Unknown switch`

Fix:
- For build, pass the solution as a positional argument:
```powershell
dotnet build .\6808Asm.slnx -c Debug
```
