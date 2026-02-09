# Local GotAHero Coverage Checklist

- Source: `tests/corpus/local-gotahero/GotAHero.asm`
- Total parsed code lines: 285
- Unique directives: 7
- Unique opcodes: 36

## Directives Required For v1
- [ ] END (1)
- [ ] EQU (14)
- [ ] FCB (37)
- [ ] FCC (7)
- [ ] FDB (1)
- [ ] ORG (1)
- [ ] RMB (4)

## Opcodes Observed In Corpus
- [ ] ABA (1)
- [ ] ADDA (4)
- [ ] ANDA (3)
- [ ] ASLA (4)
- [ ] BCC (1)
- [ ] BEQ (13)
- [ ] BLS (1)
- [ ] BNE (9)
- [ ] BRA (2)
- [ ] BSR (4)
- [ ] CLI (7)
- [ ] CLR (2)
- [ ] CLRA (2)
- [ ] CLRB (3)
- [ ] CMPA (22)
- [ ] INX (12)
- [ ] JMP (18)
- [ ] JSR (35)
- [ ] LDAA (9)
- [ ] LDAB (3)
- [ ] LDX (16)
- [ ] LSRA (4)
- [ ] ORAA (1)
- [ ] PSHA (2)
- [ ] PULA (2)
- [ ] RTS (8)
- [ ] SEI (2)
- [ ] STAA (16)
- [ ] STAB (3)
- [ ] STX (4)
- [ ] SUBA (1)
- [ ] SWI (1)
- [ ] TAB (2)
- [ ] TBA (1)
- [ ] TSTA (1)
- [ ] TSTB (1)

## Addressing/Operand Patterns To Support
- [ ] direct8 (4)
- [ ] extended16 (3)
- [ ] immediate (49)
- [ ] indexed (15)
- [ ] inherent (53)
- [ ] other (8)
- [ ] symbolic (88)
- [ ] symbol-plus-offset operands (examples: `DispChar+1`, `MoveMotor1+1`, `ScratchPad+1`)
- [ ] immediate char literal prefix form used in corpus (example: `#'G`)

## Artifact
- Machine-readable details: `tests/corpus/local-gotahero/coverage.json`

## Notes
- This checklist is corpus-driven and intentionally excludes unsupported features not present in this sample.
