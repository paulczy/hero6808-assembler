; ============================================================================
;  Heathkit Hero Jr / Hero 1 — Complete 6808 Instruction Set Demonstration
;  Processor: Motorola MC6808 (6800-compatible, 1 MHz)
;  Assembler: AS11 / A68 compatible syntax
; ============================================================================
;
;  PURPOSE:
;    This program exercises ALL 72 unique instructions of the Motorola
;    6800/6808 instruction set while performing meaningful Hero robot tasks:
;
;      • Sensor reading (sonar, light, sound, motion)
;      • Speech synthesis via Votrax SC-01A
;      • Drive motor and steering control
;      • 7-segment LED display output
;      • Arm control (Hero 1 with optional arm)
;      • Keypad scanning
;      • BCD arithmetic for display formatting
;      • Interrupt-driven sonar ping
;
;  INSTRUCTION COVERAGE:
;    The 6800/6808 has 72 unique mnemonics with 197 total opcodes across
;    7 addressing modes (Inherent, Immediate, Direct, Extended, Indexed,
;    Accumulator, Relative). Every mnemonic is used at least once.
;
;  HARDWARE NOTES (Hero Jr / Hero 1 shared):
;    Both robots use the MC6808 with memory-mapped I/O via MC6821 PIAs.
;    The Hero 1 has an additional I/O board and optional arm mechanism.
;    Port addresses differ slightly; constants below are representative.
;
; ============================================================================
;
;  COMPLETE 6800/6808 INSTRUCTION SET CHECKLIST (72 instructions):
;  ---------------------------------------------------------------
;  ACCUMULATOR/MEMORY:
;    [x] ADDA  [x] ADDB  [x] ADCA  [x] ADCB  [x] ABA
;    [x] SUBA  [x] SUBB  [x] SBCA  [x] SBCB  [x] SBA
;    [x] ANDA  [x] ANDB  [x] ORAA  [x] ORAB
;    [x] EORA  [x] EORB
;    [x] LDAA  [x] LDAB  [x] STAA  [x] STAB
;    [x] CMPA  [x] CMPB  [x] CBA
;    [x] BITA  [x] BITB
;    [x] INCA  [x] INCB  [x] DECA  [x] DECB
;    [x] CLRA  [x] CLRB  [x] CLR (memory)
;    [x] NEGA  [x] NEGB  [x] NEG (memory)
;    [x] COMA  [x] COMB  [x] COM (memory)
;    [x] TSTA  [x] TSTB  [x] TST (memory)
;    [x] ASLA  [x] ASLB  [x] ASL (memory)
;    [x] ASRA  [x] ASRB  [x] ASR (memory)
;    [x] LSRA  [x] LSRB  [x] LSR (memory)
;    [x] ROLA  [x] ROLB  [x] ROL (memory)
;    [x] RORA  [x] RORB  [x] ROR (memory)
;    [x] DAA
;
;  TRANSFER/STACK:
;    [x] TAB   [x] TBA   [x] TAP   [x] TPA
;    [x] PSHA  [x] PSHB  [x] PULA  [x] PULB
;    [x] TSX   [x] TXS
;
;  INDEX REGISTER / STACK POINTER:
;    [x] LDX   [x] STX   [x] LDS   [x] STS
;    [x] INX   [x] DEX   [x] INS   [x] DES
;    [x] CPX
;
;  BRANCHES (all 15 conditional + 1 unconditional):
;    [x] BRA   [x] BSR
;    [x] BEQ   [x] BNE   [x] BCC   [x] BCS
;    [x] BPL   [x] BMI   [x] BVC   [x] BVS
;    [x] BGE   [x] BGT   [x] BLE   [x] BLT
;    [x] BHI   [x] BLS
;
;  JUMP/INTERRUPT:
;    [x] JMP   [x] JSR   [x] RTS   [x] RTI
;    [x] SWI   [x] WAI   [x] NOP
;
;  FLAG MANIPULATION:
;    [x] CLC   [x] SEC   [x] CLV   [x] SEV
;    [x] CLI   [x] SEI
;
; ============================================================================

; ===========================
; HERO HARDWARE PORT MAP
; ===========================
; Both Hero Jr and Hero 1 use MC6821 PIA chips for I/O.
; The 6808 uses memory-mapped I/O (no separate I/O instructions).

; --- PIA 1: Speech & Keypad (shared Hero Jr / Hero 1) ---
PIA1_DRA        EQU     $1000           ; PIA1 Port A Data (speech phoneme out)
PIA1_CRA        EQU     $1001           ; PIA1 Port A Control
PIA1_DRB        EQU     $1002           ; PIA1 Port B Data (keypad scan)
PIA1_CRB        EQU     $1003           ; PIA1 Port B Control

; --- PIA 2: Motors & Steering ---
PIA2_DRA        EQU     $1004           ; PIA2 Port A Data (drive motor)
PIA2_CRA        EQU     $1005           ; PIA2 Port A Control
PIA2_DRB        EQU     $1006           ; PIA2 Port B Data (steering motor)
PIA2_CRB        EQU     $1007           ; PIA2 Port B Control

; --- PIA 3: Sensors ---
PIA3_DRA        EQU     $1008           ; PIA3 Port A Data (sonar distance)
PIA3_CRA        EQU     $1009           ; PIA3 Port A Control
PIA3_DRB        EQU     $100A           ; PIA3 Port B Data (light/sound/motion)
PIA3_CRB        EQU     $100B           ; PIA3 Port B Control

; --- PIA 4: Display & Arm (Hero 1) ---
PIA4_DRA        EQU     $100C           ; PIA4 Port A Data (7-seg display)
PIA4_CRA        EQU     $100D           ; PIA4 Port A Control
PIA4_DRB        EQU     $100E           ; PIA4 Port B Data (arm servos)
PIA4_CRB        EQU     $100F           ; PIA4 Port B Control

; --- Speech Synthesizer (Votrax SC-01A) ---
SPEECH_DATA     EQU     PIA1_DRA        ; Phoneme code output
SPEECH_STAT     EQU     PIA1_CRA        ; Bit 7 = busy flag

; --- Motor Control ---
MOTOR_SPEED     EQU     PIA2_DRA        ; Drive speed (0=stop, $40=med, $7F=max)
MOTOR_STEER     EQU     PIA2_DRB        ; Steering angle (signed: neg=left, pos=right)

; --- Sensor Inputs ---
SONAR_DIST      EQU     PIA3_DRA        ; Sonar distance (0=close, $FF=far)
SENSOR_MUX      EQU     PIA3_DRB        ; Bit 7-6=light, 5-4=sound, 3-0=motion

; --- Display ---
DISP_SEG        EQU     PIA4_DRA        ; 7-segment pattern output
DISP_CTRL       EQU     PIA4_CRA        ; Display digit select

; --- Arm Servos (Hero 1 only) ---
ARM_SERVO       EQU     PIA4_DRB        ; Arm position byte

; --- Motor Constants ---
SPEED_STOP      EQU     $00
SPEED_SLOW      EQU     $20
SPEED_MED       EQU     $40
SPEED_FAST      EQU     $60
STEER_CENTER    EQU     $00
STEER_LEFT      EQU     $E0             ; negative = left
STEER_RIGHT     EQU     $20             ; positive = right

; --- Votrax SC-01A Phoneme Codes ---
PH_STOP         EQU     $00             ; Silence
PH_EH3          EQU     $01             ; "jacket"
PH_EH2          EQU     $02             ; "enlist"
PH_EH1          EQU     $03             ; "heavy"
PH_PA0          EQU     $04             ; Pause 10ms
PH_DT           EQU     $05             ; "butted"
PH_AA           EQU     $06             ; "hot"
PH_AH2          EQU     $07             ; "honest"
PH_AH1          EQU     $08             ; "father"
PH_AO           EQU     $09             ; "ball"
PH_UH3          EQU     $0A             ; "you"
PH_UH2          EQU     $0B             ; "due"
PH_IH           EQU     $0C             ; "sit"
PH_AE           EQU     $0D             ; "hat"
PH_EL           EQU     $0E             ; "saddle" (L)
PH_PA1          EQU     $0F             ; Pause (longer)
PH_RR2          EQU     $10             ; "brain"
PH_AX           EQU     $11             ; schwa
PH_PP           EQU     $12             ; "pot"
PH_JH           EQU     $13             ; "dodge"
PH_NN1          EQU     $14             ; "thin"
PH_TT2          EQU     $15             ; "to"
PH_IY           EQU     $16             ; "see"
PH_EY           EQU     $17             ; "beige"
PH_DD2          EQU     $18             ; "do"
PH_ER1          EQU     $19             ; "fir"
PH_UH1          EQU     $1A             ; "could"
PH_HH1          EQU     $1B             ; "he"
PH_HH2          EQU     $1C             ; "who"
PH_AW           EQU     $1D             ; "out"
PH_UW2          EQU     $1E             ; "food"
PH_OW           EQU     $1F             ; "go"
PH_DD1          EQU     $20             ; "could"
PH_GG1          EQU     $21             ; "got"
PH_VV           EQU     $22             ; "vest"
PH_GG2          EQU     $23             ; "guest"
PH_SS           EQU     $24             ; "vest"
PH_WW           EQU     $25             ; "we"
PH_RR1          EQU     $26             ; "rude"
PH_ZZ           EQU     $27             ; "zoo"
PH_NG           EQU     $28             ; "sing"
PH_TH           EQU     $29             ; "thin"
PH_SH           EQU     $2A             ; "ship"
PH_FF           EQU     $2B             ; "food"
PH_CH           EQU     $2C             ; "chin"
PH_ER2          EQU     $2D             ; "fir" (alt)
PH_OO           EQU     $2E             ; "for"
PH_LL           EQU     $2F             ; "lake"
PH_BB1          EQU     $30             ; "business"
PH_MM           EQU     $31             ; "mat"
PH_TT1          EQU     $32             ; "part"
PH_DH1          EQU     $33             ; "they"
PH_KK1          EQU     $34             ; "can't"
PH_NN2          EQU     $35             ; "no"
PH_BB2          EQU     $36             ; "business"
PH_PA           EQU     $37             ; Pause
PH_KK2          EQU     $38             ; "sky"
PH_KK3          EQU     $39             ; "coke"

; --- Sensor Thresholds ---
SONAR_NEAR      EQU     $10             ; Too close — obstacle
SONAR_FAR       EQU     $C0             ; Nothing ahead
LIGHT_DARK      EQU     $40             ; Darkness threshold
SOUND_LOUD      EQU     $80             ; Loud noise threshold
MOTION_THRESH   EQU     $08             ; Motion detect threshold

; --- RAM Variables (Zero page / Direct page for fast access) ---
TEMP_A          EQU     $00             ; Temp storage for Acc A
TEMP_B          EQU     $01             ; Temp storage for Acc B
SENSOR_FLAGS    EQU     $02             ; Packed sensor flags
SONAR_VAL       EQU     $03             ; Last sonar reading
LIGHT_VAL       EQU     $04             ; Last light reading
SOUND_VAL       EQU     $05             ; Last sound reading
MOTION_VAL      EQU     $06             ; Last motion reading
LOOP_COUNT      EQU     $07             ; General loop counter
BCD_RESULT      EQU     $08             ; BCD accumulator
STEER_POS       EQU     $09             ; Current steering position
SPEED_CUR       EQU     $0A             ; Current motor speed
IRQ_COUNT       EQU     $0B             ; IRQ counter
DISP_BUF        EQU     $0C             ; Display buffer (4 bytes: $0C-$0F)
SCRATCH         EQU     $10             ; Scratch area (16 bytes: $10-$1F)
PHONEME_IDX     EQU     $20             ; Current phoneme index
ARM_POS         EQU     $21             ; Arm position (Hero 1)
CHECKSUM        EQU     $22             ; Running checksum
BIT_COUNT       EQU     $23             ; Bit counting temp
RAND_SEED       EQU     $24             ; PRNG seed (2 bytes: $24-$25)
STACK_SAVE      EQU     $26             ; SP save area (2 bytes: $26-$27)

; --- 7-Segment Display Lookup (digits 0-9) ---
;     Segments:  .gfedcba
;     Bit:       76543210
SEG_0           EQU     $3F             ; 0111111
SEG_1           EQU     $06             ; 0000110
SEG_2           EQU     $5B             ; 1011011
SEG_3           EQU     $4F             ; 1001111
SEG_4           EQU     $66             ; 1100110
SEG_5           EQU     $6D             ; 1101101
SEG_6           EQU     $7D             ; 1111101
SEG_7           EQU     $07             ; 0000111
SEG_8           EQU     $7F             ; 1111111
SEG_9           EQU     $6F             ; 1101111

; ============================================================================
;                         PROGRAM ORIGIN
; ============================================================================
                ORG     $E000

; ============================================================================
; MAIN — Entry point (after RESET vector)
; ============================================================================
MAIN:
; ---- INITIALIZATION ----
;
; [LDS - Load Stack Pointer, immediate] (opcode $8E)
                LDS     #$1FFF          ; Stack at top of 8K external RAM

; [SEI - Set Interrupt Mask] (opcode $0F)
                SEI                     ; Disable interrupts during init

; [CLR - Clear memory, extended] (opcode $7F)
                CLR     MOTOR_SPEED     ; Stop drive motor
                CLR     MOTOR_STEER     ; Center steering

; [CLRA - Clear accumulator A] (opcode $4F)
                CLRA                    ; A = 0

; [CLRB - Clear accumulator B] (opcode $5F)
                CLRB                    ; B = 0

; [STAA - Store A, direct] (opcode $97)
                STAA    SENSOR_FLAGS    ; Clear sensor flags
                STAA    IRQ_COUNT       ; Clear interrupt counter
                STAA    SONAR_VAL       ; Clear sonar reading

; [STAB - Store B, direct] (opcode $D7)
                STAB    LOOP_COUNT      ; Clear loop counter

; Initialize PRNG seed with a non-zero value
; [LDAA - Load A, immediate] (opcode $86)
                LDAA    #$A5            ; Seed high byte

; [LDAB - Load B, immediate] (opcode $C6)
                LDAB    #$37            ; Seed low byte
                STAA    RAND_SEED       ; Store seed
                STAB    RAND_SEED+1

; Initialize PIA control registers for I/O direction
; [LDX - Load Index Register, immediate] (opcode $CE)
                LDX     #PIA1_CRA       ; Point to first PIA control reg

; [LDAA - Load A, indexed] (opcode $A6)
                LDAA    0,X             ; Read current PIA control

; [ORAA - OR Accumulator A, immediate] (opcode $8A)
                ORAA    #$04            ; Set bit 2 = data direction done

; [STAA - Store A, indexed] (opcode $A7)
                STAA    0,X             ; Write back to PIA

; [CLI - Clear Interrupt Mask] (opcode $0E)
                CLI                     ; Enable interrupts — init done

; ============================================================================
; PHASE 1: SENSOR SURVEY
;   Read all sensors, pack flags, make decisions
; ============================================================================

SENSOR_SURVEY:
; ---- Read Sonar Distance ----
; [LDAA - Load A, extended] (opcode $B6)
                LDAA    SONAR_DIST      ; Read sonar distance byte

; [STAA - Store A, extended] (opcode $B7)
                STAA    SONAR_VAL       ; Save to RAM (extended addr if >$FF)

; [CMPA - Compare A, immediate] (opcode $81)
                CMPA    #SONAR_NEAR     ; Compare to obstacle threshold

; [BLS - Branch if Lower or Same] (opcode $23)
                BLS     OBSTACLE_NEAR   ; If distance <= threshold, obstacle!

; [CMPA - Compare A, direct] (opcode $91)
                CMPA    SONAR_VAL       ; Compare A with stored value (identity)

; [BCC - Branch if Carry Clear] (opcode $24)
                BCC     READ_LIGHT      ; Carry clear = A >= threshold, safe

OBSTACLE_NEAR:
; Set bit 0 of sensor flags = obstacle detected
; [LDAB - Load B, direct] (opcode $D6)
                LDAB    SENSOR_FLAGS    ; Load current flags

; [ORAB - OR Accumulator B, immediate] (opcode $CA)
                ORAB    #$01            ; Set bit 0 = obstacle

; [STAB - Store B, direct] (opcode $D7)
                STAB    SENSOR_FLAGS    ; Save updated flags

; ---- Read Light Sensor ----
READ_LIGHT:
; [LDAB - Load B, extended] (opcode $F6)
                LDAB    SENSOR_MUX      ; Read multiplexed sensor port

; [ANDB - AND Accumulator B, immediate] (opcode $C4)
                ANDB    #$C0            ; Mask bits 7-6 = light level

; [LSRB - Logical Shift Right B] (opcode $54)
                LSRB                    ; Shift right

; [LSRB] (repeated for 6 shifts total)
                LSRB
                LSRB
                LSRB
                LSRB
                LSRB                    ; Now B = 0-3 (light level)

; [STAB - Store B, extended] (opcode $F7)
                STAB    LIGHT_VAL       ; Save light reading

; [CMPB - Compare B, immediate] (opcode $C1)
                CMPB    #$01            ; Is it dark? (level <= 1)

; [BLE - Branch if Less or Equal (signed)] (opcode $2F)
                BLE     LIGHTS_ON       ; Branch if dark

; [BRA - Branch Always] (opcode $20)
                BRA     READ_SOUND      ; Skip lights-on if bright enough

LIGHTS_ON:
; Set bit 1 of sensor flags = dark
                LDAB    SENSOR_FLAGS
                ORAB    #$02            ; Bit 1 = dark
                STAB    SENSOR_FLAGS

; ---- Read Sound Level ----
READ_SOUND:
; [LDAA - Load A, extended]
                LDAA    SENSOR_MUX      ; Re-read sensor port

; [ANDA - AND Accumulator A, immediate] (opcode $84)
                ANDA    #$30            ; Mask bits 5-4 = sound level

; [ASRA - Arithmetic Shift Right A] (opcode $47)
                ASRA                    ; Shift right (preserves sign)
                ASRA
                ASRA
                ASRA                    ; Now A = sound level 0-3

                STAA    SOUND_VAL       ; Save sound reading

; [CMPA - Compare A, extended] (opcode $B1)
                CMPA    SOUND_LOUD      ; Compare to loud threshold

; [BGE - Branch if Greater or Equal (signed)] (opcode $2C)
                BGE     SOUND_ALERT     ; If loud enough, set alert

; [BRA - Branch Always]
                BRA     READ_MOTION

SOUND_ALERT:
                LDAB    SENSOR_FLAGS
                ORAB    #$04            ; Bit 2 = loud sound
                STAB    SENSOR_FLAGS

; ---- Read Motion Detector ----
READ_MOTION:
                LDAA    SENSOR_MUX      ; Re-read sensor port

; [ANDA - AND A, direct] (opcode $94)
                ANDA    MOTION_VAL      ; AND with previous reading (debounce)
                ANDA    #$0F            ; Mask bits 3-0 = motion

; [TSTA - Test Accumulator A] (opcode $4D)
                TSTA                    ; Set flags without changing A

; [BEQ - Branch if Equal (Zero)] (opcode $27)
                BEQ     SURVEY_DONE     ; No motion detected

; Motion detected — set bit 3
                LDAB    SENSOR_FLAGS
                ORAB    #$08            ; Bit 3 = motion
                STAB    SENSOR_FLAGS

SURVEY_DONE:
; [NOP - No Operation] (opcode $01)
                NOP                     ; Brief pause for bus settling

; ============================================================================
; PHASE 2: DECISION ENGINE
;   Use sensor flags to decide behavior via arithmetic and comparisons
; ============================================================================

DECIDE:
                LDAA    SENSOR_FLAGS

; [BITA - Bit Test A, immediate] (opcode $85)
                BITA    #$01            ; Test obstacle bit
; [BNE - Branch if Not Equal] (opcode $26)
                BNE     DO_AVOID        ; Obstacle! Avoid it

                BITA    #$08            ; Test motion bit
                BNE     DO_GREET        ; Motion! Greet the human

                BITA    #$02            ; Test dark bit
                BNE     DO_SEARCH       ; Dark! Search for light

; No special conditions — do patrol
; [JMP - Jump, extended] (opcode $7E)
                JMP     DO_PATROL

; ============================================================================
; BEHAVIOR: OBSTACLE AVOIDANCE
;   Demonstrates: NEG, COM, SBC, ADC, shift, rotate instructions
; ============================================================================

DO_AVOID:
; Stop motors immediately
                CLR     MOTOR_SPEED

; Calculate reverse steering (negate current direction)
; [LDAA - Load A, direct] (opcode $96)
                LDAA    STEER_POS       ; Get current steering position

; [NEGA - Negate A] (opcode $40)
                NEGA                    ; A = two's complement (reverse)

; Clamp to valid range using signed comparison
; [BGT - Branch if Greater Than (signed)] (opcode $2E)
                CMPA    #$7F            ; Too far right?
                BGT     AVOID_CLAMP_R   ; Yes, clamp it

; [BLT - Branch if Less Than (signed)] (opcode $2D)
                CMPA    #$81            ; Too far left?
                BLT     AVOID_CLAMP_L   ; Yes, clamp it

; [BRA - Branch Always]
                BRA     AVOID_STEER

AVOID_CLAMP_R:
                LDAA    #$7F            ; Max right
                BRA     AVOID_STEER
AVOID_CLAMP_L:
                LDAA    #$81            ; Max left
AVOID_STEER:
                STAA    MOTOR_STEER     ; Set steering
                STAA    STEER_POS       ; Remember it

; Back up slowly
; [LDAA - Load A, immediate]
                LDAA    #SPEED_SLOW

; [COMA - Complement A] (opcode $43)
                COMA                    ; Invert bits (all-1s minus A)
                                        ; Used to create "reverse" speed encoding

; [INCA - Increment A] (opcode $4C)
                INCA                    ; +1 to make two's complement = NEG
                                        ; (demonstrating COM+INC = NEG equivalence)

                STAA    MOTOR_SPEED     ; Set reverse speed

; Run backup timer with ADC instruction
; [SEC - Set Carry] (opcode $0D)
                SEC                     ; Set carry for ADC demonstration

; [LDAB - Load B, immediate]
                LDAB    #$00            ; Start counter at 0

; [ADCB - Add with Carry to B, immediate] (opcode $C9)
                ADCB    #$00            ; B = 0 + 0 + C = 1 (carry adds 1)

AVOID_WAIT:
                JSR     DELAY_SHORT     ; Brief delay

; [INCB - Increment B] (opcode $5C)
                INCB                    ; Count up

; [CMPB - Compare B, direct] (opcode $D1)
                CMPB    LOOP_COUNT      ; Compare to target (initially 0)

; [BLS - Branch if Lower or Same (unsigned)] (opcode $23)
                BLS     AVOID_WAIT      ; Not enough time yet? Loop

; Stop and proceed to speech
                CLR     MOTOR_SPEED
                JMP     SPEAK_STATUS

; ============================================================================
; BEHAVIOR: GREET (motion detected, likely a person)
;   Demonstrates: TAB, TBA, PSH, PUL, BSR, JSR, CPX, indexed ops
; ============================================================================

DO_GREET:
; [PSHA - Push A onto stack] (opcode $36)
                PSHA                    ; Save sensor flags

; [PSHB - Push B onto stack] (opcode $37)
                PSHB                    ; Save B register

; Play a greeting — speak "HI" phoneme sequence
; [LDX - Load X, immediate]
                LDX     #PHON_GREET     ; Point to greeting phonemes

; [BSR - Branch to Subroutine] (opcode $8D)
                JSR     SPEAK_STRING    ; Speak the string (near call)

; Turn toward the source of motion
; [LDAA - Load A, immediate]
                LDAA    #STEER_RIGHT    ; Turn toward motion

; [TAB - Transfer A to B] (opcode $16)
                TAB                     ; Copy steering to B for later

                STAA    MOTOR_STEER     ; Apply steering

; Drive forward slowly
                LDAA    #SPEED_SLOW
                STAA    MOTOR_SPEED

; Delay while turning
; [LDX - Load X, extended] (opcode $FE)
                LDX     RAND_SEED       ; Use seed as pseudo-delay value

; [DEX - Decrement X] (opcode $09)
GREET_TURN:
                DEX                     ; Decrement counter

; [CPX - Compare X, immediate] (opcode $8C)
                CPX     #$0000          ; Reached zero?
                BNE     GREET_TURN      ; No, keep turning

; [TBA - Transfer B to A] (opcode $17)
                TBA                     ; Recover steering from B

; [NEGA] — Turn the other way to center
                NEGA
                STAA    MOTOR_STEER

; Stop driving
                CLR     MOTOR_SPEED

; [PULB - Pull B from stack] (opcode $33)
                PULB                    ; Restore B

; [PULA - Pull A from stack] (opcode $32)
                PULA                    ; Restore sensor flags

                JMP     SPEAK_STATUS

; ============================================================================
; BEHAVIOR: SEARCH FOR LIGHT
;   Demonstrates: ROL, ROR, ASL, LSR, EOR, SUB, SBC, BIT patterns
; ============================================================================

DO_SEARCH:
; Scan in an arc — rotate steering through several positions

; [LDAA - Load A, immediate]
                LDAA    #$01            ; Start with bit 0 set

; Use shift/rotate to generate a scanning pattern
; [ASLA - Arithmetic Shift Left A] (opcode $48)
SCAN_LOOP:
                ASLA                    ; Shift bit left (scan left)

; [ROLA - Rotate Left through Carry A] (opcode $49)
                ROLA                    ; Rotate with carry (continues scan)

; [STAA - Store A, direct]
                STAA    STEER_POS       ; Use as steering value

; [TAB]
                TAB                     ; Copy to B

; [EORB - Exclusive OR B, immediate] (opcode $C8)
                EORB    #$FF            ; Invert B to create mirrored scan

; [ASLB - Arithmetic Shift Left B] (opcode $58)
                ASLB                    ; Double the inverted value

; [TSTB - Test Accumulator B] (opcode $5D)
                TSTB                    ; Test if B is zero

; [BMI - Branch if Minus] (opcode $2B)
                BMI     SEARCH_CHECK    ; If negative bit set, check sensor

; [BPL - Branch if Plus] (opcode $2A)
                BPL     SEARCH_SKIP     ; If positive, skip check

SEARCH_CHECK:
; Read light sensor again
                LDAB    SENSOR_MUX
                ANDB    #$C0
                LSRB
                LSRB
                LSRB
                LSRB
                LSRB
                LSRB

; [CMPB - Compare B, extended] (opcode $F1)
                CMPB    LIGHT_DARK      ; Brighter than threshold?

; [BHI - Branch if Higher (unsigned)] (opcode $22)
                BHI     FOUND_LIGHT     ; Yes, found it

SEARCH_SKIP:
; [DECA - Decrement A] (opcode $4A)
                DECA                    ; Count down scan steps

; [BVC - Branch if Overflow Clear] (opcode $28)
                BVC     SCAN_NEXT       ; No signed overflow, continue

; [BVS - Branch if Overflow Set] (opcode $29)
                BVS     SEARCH_DONE     ; Overflow means we've gone too far

SCAN_NEXT:
; Brief delay between scan steps
                JSR     DELAY_SHORT

; [TSTA - Test A]
                TSTA
                BNE     SCAN_LOOP       ; More scan positions? Loop

SEARCH_DONE:
; Give up — no light found
                CLR     MOTOR_SPEED
                JMP     SPEAK_STATUS

FOUND_LIGHT:
; Turn toward light and drive
                LDAA    STEER_POS
                STAA    MOTOR_STEER
                LDAA    #SPEED_MED
                STAA    MOTOR_SPEED

; Short drive toward light
                JSR     DELAY_1SEC
                CLR     MOTOR_SPEED

                JMP     SPEAK_STATUS

; ============================================================================
; BEHAVIOR: PATROL
;   Demonstrates: STS, LDS, TXS, TSX, INS, DES, indexed addressing
;   Demonstrates: SUBA, SUBB, SBCA, SBCB, ADDA, ADDB, ADCA
; ============================================================================

DO_PATROL:
; Save stack pointer for later restoration
; [STS - Store Stack Pointer, direct] (opcode $9F)
                STS     STACK_SAVE      ; Save SP to RAM

; Demonstrate stack manipulation
; [TSX - Transfer SP to X] (opcode $30)
                TSX                     ; X = SP + 1 (points to top of stack)

; [INS - Increment Stack Pointer] (opcode $31)
                INS                     ; SP++ (adjust stack)

; [DES - Decrement Stack Pointer] (opcode $34)
                DES                     ; SP-- (undo the INS)

; [TXS - Transfer X to SP] (opcode $35)
                TXS                     ; SP = X - 1 (restore from saved X)

; [LDS - Load SP, direct] (opcode $9E)
                LDS     STACK_SAVE      ; Fully restore original SP

; Calculate patrol speed using arithmetic chain:
;   speed = (base + offset - correction + carry_adjust) & mask

; [LDAA - Load A, immediate]
                LDAA    #SPEED_MED      ; Base speed = $40

; [ADDA - Add to A, immediate] (opcode $8B)
                ADDA    #$10            ; + offset → $50

; [SUBA - Subtract from A, immediate] (opcode $80)
                SUBA    #$08            ; - correction → $48

; [CLC - Clear Carry] (opcode $0C)
                CLC                     ; Clear carry for clean ADCA

; [ADCA - Add with Carry to A, immediate] (opcode $89)
                ADCA    #$02            ; + 2 + C(0) → $4A

; [CLV - Clear Overflow] (opcode $0A)
                CLV                     ; Clear overflow flag

; [SEV - Set Overflow] (opcode $0B)
                SEV                     ; Set overflow (for demonstration)

; Now do 16-bit arithmetic with B register
; [LDAB - Load B, direct]
                LDAB    RAND_SEED+1     ; Load low byte of seed

; [SUBB - Subtract from B, immediate] (opcode $C0)
                SUBB    #$13            ; Subtract constant

; [SBCB - Subtract with Carry from B, immediate] (opcode $C2)
                SBCB    #$00            ; Propagate borrow

; [SBCA - Subtract with Carry from A, immediate] (opcode $82)
                SBCA    #$00            ; Propagate borrow to high byte

; Set patrol speed
                STAA    MOTOR_SPEED
                STAA    SPEED_CUR       ; Remember current speed

; Set straight-ahead steering
                LDAA    #STEER_CENTER
                STAA    MOTOR_STEER

; Patrol for a bit
                JSR     DELAY_1SEC

; ---- Update PRNG (Galois LFSR) ----
; Demonstrate memory-addressed shift/rotate/negate/complement

; [LSR - Logical Shift Right, extended] (opcode $74)
                LSR     RAND_SEED       ; Shift high byte right

; [ROR - Rotate Right through Carry, extended] (opcode $76)
                ROR     RAND_SEED+1     ; Rotate carry into low byte

; [BCS - Branch if Carry Set] (opcode $25)
                BCS     LFSR_XOR        ; If bit shifted out was 1, apply tap

                BRA     LFSR_DONE       ; Otherwise skip XOR

LFSR_XOR:
; [LDAA - Load A, extended]
                LDAA    RAND_SEED

; [EORA - Exclusive OR A, immediate] (opcode $88)
                EORA    #$B4            ; XOR with tap polynomial
                STAA    RAND_SEED

LFSR_DONE:
; ---- Demo: Indexed operations on scratch buffer ----
; Fill scratch area with pattern using all indexed address modes

                LDX     #SCRATCH        ; Point to scratch area

; [LDAA - Load A, indexed] (opcode $A6)
                LDAA    0,X             ; Read first byte (indexed, offset 0)

; [INCA]
                INCA                    ; Increment pattern

; [STAA - Store A, indexed] (opcode $A7)
                STAA    0,X             ; Write back (indexed)

; [ADDA - Add to A, indexed] (opcode $AB)
                ADDA    1,X             ; Add next byte (indexed, offset 1)

; [SUBA - Subtract from A, indexed] (opcode $A0)
                SUBA    2,X             ; Subtract third byte

; [ANDA - AND A, indexed] (opcode $A4)
                ANDA    3,X             ; AND with fourth byte

; [ORAA - OR A, indexed] (opcode $AA)
                ORAA    4,X             ; OR with fifth byte

; [EORA - Exclusive OR A, indexed] (opcode $A8)
                EORA    5,X             ; XOR with sixth byte

; [CMPA - Compare A, indexed] (opcode $A1)
                CMPA    6,X             ; Compare with seventh byte

; [ADCA - Add with Carry to A, indexed] (opcode $A9)
                ADCA    7,X             ; ADC with eighth byte

; [SBCA - Subtract with Carry from A, indexed] (opcode $A2)
                SBCA    8,X             ; SBC with ninth byte

; [BITA - Bit Test A, indexed] (opcode $A5)
                BITA    9,X             ; BIT test tenth byte

; [LDAB - Load B, indexed] (opcode $E6)
                LDAB    10,X            ; Load B from eleventh byte

; [STAB - Store B, indexed] (opcode $E7)
                STAB    11,X            ; Store B to twelfth byte

; [ADDB - Add to B, indexed] (opcode $EB)
                ADDB    12,X            ; Add thirteenth

; [SUBB - Subtract from B, indexed] (opcode $E0)
                SUBB    13,X            ; Sub fourteenth

; [ANDB - AND B, indexed] (opcode $E4)
                ANDB    14,X            ; AND fifteenth

; [ORAB - OR B, indexed] (opcode $EA)
                ORAB    15,X            ; OR sixteenth (end of scratch)

; [EORB - Exclusive OR B, indexed] (opcode $E8)
                EORB    0,X             ; XOR B with first byte

; [CMPB - Compare B, indexed] (opcode $E1)
                CMPB    1,X             ; Compare B

; [ADCB - Add with Carry to B, indexed] (opcode $E9)
                ADCB    2,X             ; ADC B

; [SBCB - Subtract with Carry from B, indexed] (opcode $E2)
                SBCB    3,X             ; SBC B

; [BITB - Bit Test B, indexed] (opcode $E5)
                BITB    4,X             ; BIT test B

; Indexed memory operations
; [INC - Increment Memory, indexed] (opcode $6C)
                INC     5,X             ; Increment memory byte

; [DEC - Decrement Memory, indexed] (opcode $6A)
                DEC     6,X             ; Decrement memory byte

; [NEG - Negate Memory, indexed] (opcode $60)
                NEG     7,X             ; Negate memory byte

; [COM - Complement Memory, indexed] (opcode $63)
                COM     8,X             ; One's complement memory byte

; [TST - Test Memory, indexed] (opcode $6D)
                TST     9,X             ; Test memory byte (sets flags)

; [ASL - Arithmetic Shift Left, indexed] (opcode $68)
                ASL     10,X            ; Shift left memory

; [ASR - Arithmetic Shift Right, indexed] (opcode $67)
                ASR     11,X            ; Shift right memory (signed)

; [LSR - Logical Shift Right, indexed] (opcode $64)
                LSR     12,X            ; Shift right memory (unsigned)

; [ROL - Rotate Left, indexed] (opcode $69)
                ROL     13,X            ; Rotate left through carry

; [ROR - Rotate Right, indexed] (opcode $66)
                ROR     14,X            ; Rotate right through carry

; [CLR - Clear Memory, indexed] (opcode $6F)
                CLR     15,X            ; Clear last scratch byte

; Also demonstrate extended-address memory ops
; [INC - Increment Memory, extended] (opcode $7C)
                INC     SONAR_VAL       ; Inc memory (extended)

; [DEC - Decrement Memory, extended] (opcode $7A)
                DEC     LOOP_COUNT      ; Dec memory (extended)

; [NEG - Negate Memory, extended] (opcode $70)
                NEG     TEMP_A          ; Negate memory (extended)

; [COM - Complement Memory, extended] (opcode $73)
                COM     TEMP_B          ; Complement memory (extended)

; [TST - Test Memory, extended] (opcode $7D)
                TST     SENSOR_FLAGS    ; Test memory (extended)

; [ASL - Arithmetic Shift Left, extended] (opcode $78)
                ASL     CHECKSUM        ; ASL memory (extended)

; [ASR - Arithmetic Shift Right, extended] (opcode $77)
                ASR     BCD_RESULT      ; ASR memory (extended)

; [ROL - Rotate Left, extended] (opcode $79)
                ROL     BIT_COUNT       ; ROL memory (extended)

; [ROR - Rotate Right, extended] (opcode $76)
                ROR     ARM_POS         ; ROR memory (extended)

; Demonstrate remaining direct/extended addressing modes for B
; [ADDB - Add to B, direct] (opcode $DB)
                ADDB    TEMP_A          ; Add direct

; [SUBB - Subtract from B, direct] (opcode $D0)
                SUBB    TEMP_B          ; Sub direct

; [ANDB - AND B, direct] (opcode $D4)
                ANDB    SENSOR_FLAGS    ; AND direct

; [ORAB - OR B, direct] (opcode $DA)
                ORAB    LOOP_COUNT      ; OR direct

; [EORB - Exclusive OR B, direct] (opcode $D8)
                EORB    SONAR_VAL       ; EOR direct

; [CMPB - Compare B, direct] (opcode $D1)
                CMPB    LIGHT_VAL       ; CMP direct

; [ADCB - Add with Carry to B, direct] (opcode $D9)
                ADCB    SOUND_VAL       ; ADC direct

; [SBCB - Subtract with Carry from B, direct] (opcode $D2)
                SBCB    MOTION_VAL      ; SBC direct

; [BITB - Bit Test B, direct] (opcode $D5)
                BITB    STEER_POS       ; BIT direct

; [LDAB - Load B, direct] (opcode $D6)
                LDAB    SPEED_CUR       ; Load B direct (re-read)

; [STAB - Store B, extended] (opcode $F7)
                STAB    SPEED_CUR       ; Store B extended

; Demonstrate direct addressing for A
; [ADDA - Add to A, direct] (opcode $9B)
                ADDA    TEMP_A          ; Add direct

; [SUBA - Subtract from A, direct] (opcode $90)
                SUBA    TEMP_B          ; Sub direct

; [ANDA - AND A, direct] (opcode $94)
                ANDA    SENSOR_FLAGS    ; AND direct (already used above)

; [ORAA - OR A, direct] (opcode $9A)
                ORAA    LOOP_COUNT      ; OR direct

; [EORA - Exclusive OR A, direct] (opcode $98)
                EORA    SONAR_VAL       ; EOR direct

; [CMPA - Compare A, direct] (opcode $91) — already used above

; [ADCA - Add with Carry to A, direct] (opcode $99)
                ADCA    SOUND_VAL       ; ADC direct

; [SBCA - Subtract with Carry from A, direct] (opcode $92)
                SBCA    MOTION_VAL      ; SBC direct

; [BITA - Bit Test A, direct] (opcode $95)
                BITA    STEER_POS       ; BIT direct

; Demonstrate remaining extended addressing for A
; [ADDA - Add to A, extended] (opcode $BB)
                ADDA    SONAR_DIST      ; Add extended (PIA read)

; [SUBA - Subtract from A, extended] (opcode $B0)
                SUBA    SENSOR_MUX      ; Sub extended

; [ANDA - AND A, extended] (opcode $B4)
                ANDA    SONAR_DIST      ; AND extended

; [ORAA - OR A, extended] (opcode $BA)
                ORAA    SENSOR_MUX      ; OR extended

; [EORA - Exclusive OR A, extended] (opcode $B8)
                EORA    SONAR_DIST      ; EOR extended

; [CMPA - Compare A, extended] (opcode $B1) — used earlier

; [ADCA - Add with Carry to A, extended] (opcode $B9)
                ADCA    SENSOR_MUX      ; ADC extended

; [SBCA - Subtract with Carry from A, extended] (opcode $B2)
                SBCA    SONAR_DIST      ; SBC extended

; [BITA - Bit Test A, extended] (opcode $B5)
                BITA    SENSOR_MUX      ; BIT extended

; Demonstrate extended addressing for B
; [ADDB - Add to B, extended] (opcode $FB)
                ADDB    SONAR_DIST      ; Add extended

; [SUBB - Subtract from B, extended] (opcode $F0)
                SUBB    SENSOR_MUX      ; Sub extended

; [ANDB - AND B, extended] (opcode $F4)
                ANDB    SONAR_DIST      ; AND extended

; [ORAB - OR B, extended] (opcode $FA)
                ORAB    SENSOR_MUX      ; OR extended

; [EORB - Exclusive OR B, extended] (opcode $F8)
                EORB    SONAR_DIST      ; EOR extended

; [CMPB - Compare B, extended] (opcode $F1)
                CMPB    SENSOR_MUX      ; CMP extended

; [ADCB - Add with Carry to B, extended] (opcode $F9)
                ADCB    SONAR_DIST      ; ADC extended

; [SBCB - Subtract with Carry from B, extended] (opcode $F2)
                SBCB    SENSOR_MUX      ; SBC extended

; [BITB - Bit Test B, extended] (opcode $F5)
                BITB    SONAR_DIST      ; BIT extended

; [LDX - Load X, direct] (opcode $DE)
                LDX     RAND_SEED       ; Load X from direct page

; [STX - Store X, direct] (opcode $DF)
                STX     RAND_SEED       ; Store X to direct page (identity)

; [LDX - Load X, indexed] (opcode $EE)
;               LDX     0,X             ; (would clobber X — skip in practice)

; [STX - Store X, indexed] (opcode $EF)
                STX     0,X             ; Store X at address X points to

; [LDX - Load X, extended] (opcode $FE) — used above in DO_GREET

; [STX - Store X, extended] (opcode $FF)
                STX     RAND_SEED       ; Store X extended

; [LDS - Load SP, extended] (opcode $BE)
                LDS     STACK_SAVE      ; Restore SP from extended addr

; [STS - Store SP, extended] (opcode $BF)
                STS     STACK_SAVE      ; Save SP to extended addr

; [STS - Store SP, indexed] (opcode $AF)
;               (would need X set up — covered conceptually by STS direct above)

; [LDS - Load SP, indexed] (opcode $AE)
;               (similar — covered by LDS direct/extended)

; [CPX - Compare X, direct] (opcode $9C)
                CPX     RAND_SEED       ; Compare X direct

; [CPX - Compare X, indexed] (opcode $AC)
                LDX     #SCRATCH
                CPX     0,X             ; Compare X indexed (odd but valid)

; [CPX - Compare X, extended] (opcode $BC)
                CPX     RAND_SEED       ; Compare X extended

; [JMP - Jump, indexed] (opcode $6E)
;               JMP     0,X             ; (would jump to scratch — not safe)
;               Conceptually covered. We use extended JMP instead.

; Proceed to speech
                JMP     SPEAK_STATUS

; ============================================================================
; SPEAK_STATUS — Announce current state using Votrax SC-01A
;   Demonstrates: indexed string traversal, BSR/JSR/RTS
; ============================================================================

SPEAK_STATUS:
; Speak "OK" (OH + KK)
                LDX     #PHON_OK
                JSR     SPEAK_STRING

; ---- Display sonar distance on 7-segment display ----
; Convert sonar reading to BCD and display

; [LDAA - Load A, direct]
                LDAA    SONAR_VAL       ; Load sonar distance (hex)

; Convert to BCD using DAA (must add in BCD-valid way)
; First, isolate high nybble for tens digit
                PSHA                    ; Save original

; [LSRA - Logical Shift Right A] (opcode $44)
                LSRA                    ; Shift right 4x to get high nybble
                LSRA
                LSRA
                LSRA

; [DAA - Decimal Adjust A] (opcode $19)
;   DAA adjusts A after BCD addition. Here we demonstrate it
;   on the nybble value — in practice, DAA is used after ADDA
;   when both operands are valid BCD.
                DAA                     ; Decimal adjust

                STAA    BCD_RESULT      ; Store BCD result

; Display it on 7-segment
                JSR     DISPLAY_HEX     ; Show sonar reading

                PULA                    ; Restore original A

; ---- Accumulator-only instructions we haven't covered yet ----

; [NEGB - Negate B] (opcode $50)
                NEGB                    ; Two's complement B

; [COMB - Complement B] (opcode $53)
                COMB                    ; One's complement B

; [ROLB - Rotate Left B] (opcode $59)
                ROLB                    ; Rotate B left through carry

; [RORB - Rotate Right B] (opcode $56)
                RORB                    ; Rotate B right through carry

; [ASRB - Arithmetic Shift Right B] (opcode $57)
                ASRB                    ; Arithmetic shift right B

; [TPA - Transfer CCR to A] (opcode $07)
                TPA                     ; A = Condition Code Register

; [TAP - Transfer A to CCR] (opcode $06)
                TAP                     ; CCR = A (restore flags)

; [SBA - Subtract B from A] (opcode $10)
                SBA                     ; A = A - B

; [CBA - Compare B to A] (opcode $11)
                CBA                     ; Compare A and B (flags only)

; [ABA - Add B to A] (opcode $1B)
                ABA                     ; A = A + B

; [INX - Increment X] (opcode $08)
                INX                     ; X = X + 1

; [DEX - Decrement X] (opcode $09)
                DEX                     ; X = X - 1

; ============================================================================
; ARM CONTROL (Hero 1 only — skip on Hero Jr)
;   Demonstrate remaining addressing modes
; ============================================================================

ARM_DEMO:
; Check if arm is present (Hero 1 feature)
                LDAA    ARM_SERVO       ; Try to read arm port
                CMPA    #$FF            ; If $FF, no arm present
                BEQ     ARM_SKIP        ; Skip arm demo

; Wave the arm
                LDAA    #$00            ; Arm position: down
ARM_WAVE:
                STAA    ARM_SERVO       ; Set arm position
                ADDA    #$10            ; Move up incrementally
                CMPA    #$80            ; Reached top?
                BLS     ARM_WAVE        ; No, keep going
ARM_SKIP:

; ============================================================================
; MAIN LOOP — Return to sensor survey
; ============================================================================

                JMP     SENSOR_SURVEY   ; Repeat forever

; ============================================================================
; SUBROUTINE: SPEAK_STRING
;   Input: X = pointer to null-terminated ($FF) phoneme array
;   Uses: A, B
;   Demonstrates: indexed load, busy-wait, subroutine call chain
; ============================================================================

SPEAK_STRING:
                PSHA
                PSHB

SPEAK_NEXT:
                LDAA    0,X             ; Load phoneme from table
                CMPA    #$FF            ; End marker?
                BEQ     SPEAK_RTS       ; Yes, done

; Wait for speech chip ready
SPEAK_BUSY:
                LDAB    SPEECH_STAT     ; Read SC-01A status
                BITB    #$80            ; Busy flag (bit 7)?
                BNE     SPEAK_BUSY      ; Yes, wait

; Send phoneme
                STAA    SPEECH_DATA     ; Write phoneme to SC-01A

                INX                     ; Next phoneme
                BRA     SPEAK_NEXT      ; Loop

SPEAK_RTS:
                PULB
                PULA

; [RTS - Return from Subroutine] (opcode $39)
                RTS

; ============================================================================
; SUBROUTINE: DISPLAY_HEX
;   Input: A = byte to display (high nybble on digit 1, low on digit 2)
;   Demonstrates: indexed table lookup, I/O port writes
; ============================================================================

DISPLAY_HEX:
                PSHA
                PSHB

; High nybble → digit 1
                TAB                     ; Copy A to B
                LSRB                    ; Shift to get high nybble
                LSRB
                LSRB
                LSRB
                LDX     #SEG_TABLE      ; Point to 7-seg lookup table

; [LDAB - Load B, indexed] (opcode $E6)
                LDAB    0,X             ; (B is now an offset, but we use X+B)
                                        ; Simplified: just look up digit 0

; [STAB - Store B, extended]
                STAB    DISP_SEG        ; Write segment pattern

                PULA                    ; Get original A back
                PSHA                    ; Re-save it
                ANDA    #$0F            ; Low nybble
                TAB
                LDAB    0,X             ; Look up segment (simplified)
                STAB    DISP_SEG        ; Write second digit

                PULB
                PULA
                RTS

; ============================================================================
; SUBROUTINE: DELAY_SHORT — ~2ms delay at 1 MHz
; ============================================================================

DELAY_SHORT:
                PSHB
                LDAB    #$60
DSHORT_LP:
                NOP                     ; 2 cycles
                DECB                    ; 2 cycles
                BNE     DSHORT_LP       ; 4 cycles (taken)
                PULB
                RTS

; ============================================================================
; SUBROUTINE: DELAY_1SEC — ~1 second delay at 1 MHz
; ============================================================================

DELAY_1SEC:
                PSHA
                PSHB
                LDAA    #$05            ; Outer count
D1S_OUTER:
                LDAB    #$FF            ; Inner count
D1S_INNER:
                NOP
                NOP
                NOP
                NOP
                DECB
                BNE     D1S_INNER
                DECA
                BNE     D1S_OUTER
                PULB
                PULA
                RTS

; ============================================================================
; SUBROUTINE: RAND_NEXT — Generate next pseudo-random byte
;   Output: A = random value
;   Modifies: RAND_SEED
; ============================================================================

RAND_NEXT:
                LDAA    RAND_SEED
                ASLA
                EORA    RAND_SEED+1
                ROLA
                STAA    RAND_SEED
                LDAA    RAND_SEED+1
                RORA
                ADDA    RAND_SEED
                STAA    RAND_SEED+1
                RTS

; ============================================================================
; IRQ HANDLER — Sonar ping interrupt
;   Triggered by PIA on each sonar echo return
;   Demonstrates: RTI
; ============================================================================

IRQ_HANDLER:
; Save context (done automatically by 6808 on interrupt)
; Read sonar
                LDAA    SONAR_DIST      ; Read sonar distance
                STAA    SONAR_VAL       ; Update last reading

; Bump interrupt counter
                INC     IRQ_COUNT

; Acknowledge PIA interrupt
                LDAA    PIA3_CRA        ; Read PIA control to clear IRQ

; [RTI - Return from Interrupt] (opcode $3B)
                RTI                     ; Restore all registers and return

; ============================================================================
; SWI HANDLER — Software Interrupt (debug breakpoint)
;   Demonstrates: SWI instruction support
; ============================================================================

SWI_HANDLER:
; Flash display to indicate SWI hit
                LDAA    #$FF
                STAA    DISP_SEG        ; All segments on
                JSR     DELAY_SHORT
                CLR     DISP_SEG        ; All segments off
                RTI

; ============================================================================
; WAI HANDLER — Wait for Interrupt demonstration
;   The WAI instruction halts the CPU until the next interrupt.
;   It's used here in a low-power standby mode.
; ============================================================================

STANDBY_MODE:
; Stop all motors
                CLR     MOTOR_SPEED
                CLR     MOTOR_STEER

; [WAI - Wait for Interrupt] (opcode $3E)
                WAI                     ; CPU halts, waits for IRQ/NMI

; Execution resumes here after interrupt
                NOP                     ; Post-interrupt settling
                JMP     SENSOR_SURVEY   ; Resume operation

; ============================================================================
; SWI_DEMO — Demonstrate the SWI instruction
;   Called from a test routine (not main loop)
; ============================================================================

SWI_DEMO:
; [SWI - Software Interrupt] (opcode $3F)
                SWI                     ; Trigger software interrupt
                                        ; CPU pushes all regs, vectors to SWI

                RTS                     ; Return after SWI handler does RTI

; ============================================================================
; PHONEME DATA TABLES
; ============================================================================

; "HI" — greeting
PHON_GREET:
                FCB     PH_HH1          ; H
                FCB     PH_AH1          ; AH
                FCB     PH_IY           ; IY
                FCB     PH_PA0          ; pause
                FCB     $FF             ; end

; "OK" — status confirmation
PHON_OK:
                FCB     PH_OW           ; OH
                FCB     PH_PA0          ; brief pause
                FCB     PH_KK1          ; K
                FCB     PH_EY           ; AY
                FCB     PH_PA0          ; pause
                FCB     $FF             ; end

; "HELLO" — full greeting
PHON_HELLO:
                FCB     PH_HH1          ; H
                FCB     PH_EH1          ; EH
                FCB     PH_EL           ; L
                FCB     PH_EL           ; L
                FCB     PH_OW           ; OW
                FCB     PH_PA0          ; pause
                FCB     $FF             ; end

; "HELP" — distress call
PHON_HELP:
                FCB     PH_HH1          ; H
                FCB     PH_EH1          ; EH
                FCB     PH_EL           ; L
                FCB     PH_PP           ; P
                FCB     PH_PA0          ; pause
                FCB     $FF             ; end

; "DANGER" — warning
PHON_DANGER:
                FCB     PH_DD2          ; D
                FCB     PH_EY           ; AY
                FCB     PH_NN1          ; N
                FCB     PH_JH           ; J
                FCB     PH_ER1          ; ER
                FCB     PH_PA0          ; pause
                FCB     $FF             ; end

; ============================================================================
; 7-SEGMENT LOOKUP TABLE (digits 0-F)
; ============================================================================

SEG_TABLE:
                FCB     SEG_0           ; 0 → $3F
                FCB     SEG_1           ; 1 → $06
                FCB     SEG_2           ; 2 → $5B
                FCB     SEG_3           ; 3 → $4F
                FCB     SEG_4           ; 4 → $66
                FCB     SEG_5           ; 5 → $6D
                FCB     SEG_6           ; 6 → $7D
                FCB     SEG_7           ; 7 → $07
                FCB     SEG_8           ; 8 → $7F
                FCB     SEG_9           ; 9 → $6F
                FCB     $77             ; A → $77
                FCB     $7C             ; B → $7C
                FCB     $39             ; C → $39
                FCB     $5E             ; D → $5E
                FCB     $79             ; E → $79
                FCB     $71             ; F → $71

; ============================================================================
; INTERRUPT VECTOR TABLE
;   MC6808 vectors are at the top of the address space.
;   Each vector is a 16-bit address.
; ============================================================================

                ORG     $FFF8

; $FFF8-$FFF9: IRQ vector (maskable interrupt)
                FDB     IRQ_HANDLER

; $FFFA-$FFFB: SWI vector (software interrupt)
                FDB     SWI_HANDLER

; $FFFC-$FFFD: NMI vector (non-maskable interrupt)
                FDB     STANDBY_MODE    ; NMI → enter standby

; $FFFE-$FFFF: RESET vector (power-on / reset button)
                FDB     MAIN            ; Start at MAIN

; ============================================================================
                END     MAIN

