# 6808 Cross Assembler for HERO 1 / HERO Jr - Implementation Plan

## Status
- Phase: Phase 5 in progress (automation added)
- Last updated: 2026-02-09
- Owner: TBD

## Confirmed Requirements
- Target systems: Heathkit HERO 1 and HERO Jr only.
- Build approach: new assembler implementation.
- Implementation platform: .NET 10.
- Output format: fixed Motorola S19.
- Platform support: macOS and Windows.
- Distribution: zip archives.
- Scope model: Option A (assembler-only, absolute output).
- Project license: MIT.
- Test fixture policy: local-first corpus; do not bundle `Hero-ROMS` files in-repo until licensing is explicitly cleared.
- Design goal: useful for HERO programs, without high implementation complexity.
- Scope lock: support common HERO 1/Jr 6800/6808 assembly mnemonics only (no GEHPL/HF language-compiler parity).

## Confirmed Source Inventory (Agent Memory)
- Local source + output pair:
  - `H:\paul\Projects\Heathkit Hero\GotABot\gotabot4-16-0\GotAHero.asm`
  - `H:\paul\Projects\Heathkit Hero\GotABot\gotabot4-16-0\GOTAHERO.S19`
- Historical HERO assembler references:
  - `H:\paul\Projects\Heathkit Hero\Hero1_Assember\smalhero.p`
  - `H:\paul\Projects\Heathkit Hero\Hero1_Assember\smalhero.me`

## Licensing Notes (Evidence)
- New project license is MIT.

## v1 Scope (Minimal but Useful)
- CPU support: Motorola 6800/6808 instruction set used by HERO programs.
- Directives: `EQU`, `ORG`, `FCB`, `FCC`, `FDB`, `RMB`, `END`.
- Labels: global labels + forward references.
- Expressions: hex (`$`), decimal, char literals (including prefix form like `#'G`), label arithmetic (`+`/`-`).
- Comments: `;` line comments.
- Output: absolute S19 only.
- Not in v1 unless required by a confirmed sample: macros, conditionals, `INCLUDE`, relocatable objects, linker.

## HERO Mnemonic Expansion Plan (Targeted)
- Goal: implement only the instruction/directive surface needed for HERO 1 / HERO Jr manuals and ROM-style examples.
- Out of scope: compatibility with `Hero1_C-compiler` GEHPL `.HF` source language; this project remains an assembler.
- Evidence basis:
  - Current local corpus in `H:\paul\Projects\Heathkit Hero\heathkit_hero_1_programs` compiles 10/10.
  - Manual listings in `H:\paul\Projects\Heathkit Hero\HERO 1 Advanced Programming and Interfacting.pdf` include additional mnemonics and listing formats (examples on pages 17-19 and 145).
  - Repro failures observed: `unknown mnemonic 'LDS'` and parsing failure on disassembly-style lines such as `GETC B6,40,00 LDAA E #4000`.
- Priority A (implement first):
  - Mnemonics: `LDS`, `STS`, `BITA`, `BITB`, `RTI`, `DECB`, `INCB`, `PSHB`, `PULB`, `TSX`, `TXS`.
  - Branch set: `BCS`, `BMI`, `BPL`, `BVC`, `BVS`, `BGE`, `BLT`, `BGT`, `BLE`.
  - ALU and compare set: `ADDB`, `SUBB`, `CMPB`, `CPX`, `ANDB`, `ORAB`, `EORA`, `EORB`, `CBA`, `SBA`, `ABA`, `DAA`.
  - Status/control ops: `CLC`, `SEC`, `CLV`, `NOP`, `WAI`, `TAP`, `TPA`.
- Priority B (Hero listing ingestion support):
  - Accept ROM/disassembly listing prefix columns (`ADDR BYTE BYTE ... MNEMONIC OPERAND`).
  - Accept opcode-byte comma notation lines used in manual narrative listings when mnemonic is present.
  - Keep this optional behind a parser mode switch if ambiguity appears.
- Priority C (directive aliases only if confirmed in Hero sources):
  - Aliases to consider: `DB -> FCB`, `DW -> FDB`.
  - Do not add `SEGMENT/ENDS/ASSUME` unless an actual HERO source file requires it.

## .NET Implementation Notes
- Target: `.NET 10` console app and class library.
- Solution format: `Hero6808.slnx`.
- Build artifacts: self-contained zip for Windows and macOS.
- Project layout:
  - `src/Hero6808.Cli`
  - `src/Hero6808.Core`
  - `tests/Hero6808.Tests`

## To-Do Backlog

### Phase 1 - Requirements Freeze
- [x] Confirm platform targets (macOS + Windows).
- [x] Confirm output format (fixed S19).
- [x] Confirm implementation approach (new assembler).
- [x] Confirm implementation platform (.NET 10).
- [x] Confirm distribution format (zip).
- [x] Confirm target usage (HERO 1 / HERO Jr only).
- [x] Confirm scope option (A: assembler-only).
- [x] Confirm project license (MIT).
- [x] Confirm test fixture policy (local-first; no in-repo `Hero-ROMS` bundle yet).

### Phase 2 - Corpus Curation
- [x] Create `tests/corpus/local-gotahero/` with source and expected output metadata.
- [x] Record provenance and reuse restrictions in `tests/corpus/PROVENANCE.md`.
- [x] Derive opcode/directive coverage checklist from `GotAHero.asm`.

### Phase 3 - Architecture
- [x] Define lexer/parser, expression engine, symbol table, encoder, diagnostics.
- [x] Define two-pass assembly model.
- [ ] Write ADRs in `docs/adr/`.

### Phase 4 - Implementation (v1)
- [x] Implement syntax/directive set in v1 scope (corpus-driven subset).
- [x] Implement 6800/6808 encoding subset required by corpus.
- [x] Implement deterministic S19 emitter + checksum validation.
- [x] Implement CLI UX and diagnostics (file:line:message diagnostic format).
- [x] Add initial CLI command surface: `assemble <input.asm> -o <output.s19>` (wired to assembler core).

### Phase 5 - Validation and Release
- [x] Golden tests: `.asm -> .s19` exact match (local-gotahero).
- [x] Negative tests for diagnostics.
- [x] CI builds for macOS and Windows.
- [x] Zip packaging and release notes (workflow + packaging script).

### Phase 6 - HERO Compatibility Expansion
- [x] Add initial Priority A opcode batch in `src/Hero6808.Core/Assembly/OpcodeTable.cs` (`LDS`, `STS`, `BITA`, `BITB`, `INCB`, `DECB`, `PSHB`, `PULB`, `TSX`, `TXS`, `RTI`).
- [x] Extend parser recognition for the initial Priority A mnemonic batch.
- [x] Add unit tests and fixture coverage for the initial Priority A mnemonic batch in `tests/Hero6808.Tests` and `tests/corpus/hero-common-mnemonics/`.
- [ ] Complete remaining Priority A mnemonic coverage (branch set, additional ALU/compare, and status/control ops).
- [ ] Add manual-derived compatibility fixtures (`tests/corpus/hero-manual-snippets/`) with expected S19.
- [ ] Implement optional listing-mode parser for ROM/disassembly style input.
- [ ] Add regression test for snippet currently failing with `LDS`.
- [ ] Add regression test for manual I/O driver listing normalization path.
- [ ] Re-run full local corpus and publish pass/fail matrix in `tests/corpus/README.md`.

## Immediate Next Actions
1. Complete remaining Priority A mnemonic coverage from the HERO expansion plan.
2. Add `tests/corpus/hero-manual-snippets/` with extracted failing examples and expected outcomes.
3. Decide whether listing-mode parsing is default-on or opt-in.
4. Add ADR docs in `docs/adr/` for parser/addressing and diagnostics decisions.

## Decision Log
- 2026-02-09: User selected .NET implementation platform.
- 2026-02-09: User narrowed target use to HERO 1 and HERO Jr.
- 2026-02-09: User selected Option A (assembler-only).
- 2026-02-09: User selected MIT license.
- 2026-02-09: User approved local-first test fixtures and no immediate in-repo `Hero-ROMS` bundling.
- 2026-02-09: User requested .NET 10 specifically.
- 2026-02-09: Scaffold completed with `Hero6808.slnx`, `src/Hero6808.Core`, `src/Hero6808.Cli`, `tests/Hero6808.Tests`.
- 2026-02-09: Added S19 writer implementation and passing checksum/splitting unit tests.
- 2026-02-09: Two-pass assembler core implemented for corpus scope (directives + opcode encoding + symbol resolution).
- 2026-02-09: Golden corpus test added and verified exact match with `tests/corpus/local-gotahero/GOTAHERO.S19`.
- 2026-02-09: CLI `assemble` command wired to emit S19 files.
- 2026-02-09: Added negative-path diagnostic tests and standardized diagnostics to `source:line:message`.
- 2026-02-09: Added GitHub Actions workflows for CI (`.github/workflows/ci.yml`) and packaging (`.github/workflows/package.yml`), plus `scripts/package.ps1`.
- 2026-02-09: Implemented initial HERO common mnemonic batch (`LDS`, `STS`, `BITA`, `BITB`, `INCB`, `DECB`, `PSHB`, `PULB`, `TSX`, `TXS`, `RTI`) with new fixture-based tests under `tests/corpus/hero-common-mnemonics`.



