; ============================================================================
; Heathkit Hero Jr - "Hello World" + 10-foot Circle Demo
; Processor: Motorola 6808 (MC6808)
; Assembler: AS11-compatible syntax
; ============================================================================
;
; This program:
;   1. Speaks "HELLO WORLD" through the Votrax SC-01 speech synthesizer
;   2. Drives the robot in an approximate 10-foot diameter circle
;
; The Hero Jr uses a Votrax SC-01A phoneme-based speech chip and has
; differential drive (single drive wheel + steering via head/body rotation).
;
; HARDWARE NOTES:
;   - Speech synthesizer (Votrax SC-01A) at port SPEECH_PORT
;   - Speech status/ready at SPEECH_STAT
;   - Drive motor control at MOTOR_PORT
;   - Steering motor control at STEER_PORT
;   - Sonar range sensor for obstacle detection
;
; Memory Map (typical Hero Jr):
;   $0000-$007F  RAM (on-chip)
;   $0080-$00FF  I/O Ports
;   $1000-$1FFF  External RAM
;   $E000-$FFFF  ROM
; ============================================================================

; --- I/O Port Definitions ---
SPEECH_PORT     EQU     $0020           ; Votrax SC-01A phoneme data port
SPEECH_STAT     EQU     $0021           ; Speech status register (bit 7 = busy)
MOTOR_PORT      EQU     $0022           ; Drive motor speed/direction
STEER_PORT      EQU     $0023           ; Steering motor control
SONAR_PORT      EQU     $0024           ; Sonar sensor input
TIMER_PORT      EQU     $0025           ; System timer register

; --- Motor Constants ---
MOTOR_FWD       EQU     $40             ; Forward, medium speed
MOTOR_STOP      EQU     $00             ; Motors off
STEER_LEFT      EQU     $20             ; Gentle left turn for circle arc
STEER_CENTER    EQU     $00             ; Wheels straight

; --- Votrax SC-01A Phoneme Codes ---
; The SC-01A uses 6-bit phoneme codes (0-63)
; Reference: Votrax SC-01A data sheet
PH_STOP         EQU     $00             ; Stop / silence
PH_EH3          EQU     $01             ; as in "jacket"
PH_EH2          EQU     $02             ; as in "enlist"
PH_EH1          EQU     $03             ; as in "heavy"
PH_PA0          EQU     $04             ; Pause (10ms)
PH_DT           EQU     $05             ; as in "butted"
PH_AH2          EQU     $07             ; as in "honest"
PH_AH1          EQU     $08             ; as in "father"
PH_IH           EQU     $0C             ; as in "sit"
PH_AE           EQU     $0D             ; as in "hat" (used for schwa in "hello")
PH_HH1          EQU     $1B             ; as in "he"
PH_HH2          EQU     $1C             ; as in "who"
PH_OY           EQU     $05             ; as in "boy"
PH_EL           EQU     $0E             ; as in "saddle" (L sound)
PH_ER1          EQU     $33             ; as in "fir"
PH_LH           EQU     $0E             ; lateral L
PH_OH           EQU     $06             ; as in "store"
PH_OW           EQU     $35             ; as in "beau"
PH_WW           EQU     $2E             ; as in "we"
PH_RR1          EQU     $14             ; as in "rural"
PH_DD1          EQU     $15             ; as in "could"
PH_PA1          EQU     $04             ; Pause (short)

; --- Timing Constants ---
; The Hero Jr drive wheel circumference and speed determine circle params.
; At medium speed (~6 in/sec), a 10-foot circumference circle:
;   Circumference = pi * diameter = pi * 10ft = ~31.4 ft = ~377 inches
;   Time at ~6 in/sec = ~63 seconds
;   We use a loop counter to approximate this duration.
CIRCLE_TIME     EQU     $FF             ; Outer loop count
CIRCLE_INNER    EQU     $FF             ; Inner loop count (~63 sec total)

; ============================================================================
;                         PROGRAM ORIGIN
; ============================================================================
                ORG     $E000

; ============================================================================
; MAIN - Entry point
; ============================================================================
MAIN:
                ; --- Initialize ---
                LDS     #$1FFF          ; Set stack pointer to top of ext RAM
                CLR     MOTOR_PORT      ; Ensure motors are stopped
                CLR     STEER_PORT      ; Center steering

                ; --- Speak "HELLO WORLD" ---
                JSR     SAY_HELLO_WORLD

                ; --- Short pause before moving ---
                JSR     DELAY_1SEC

                ; --- Drive in a 10-foot diameter circle ---
                JSR     DRIVE_CIRCLE

                ; --- Stop and say it again ---
                CLR     MOTOR_PORT
                CLR     STEER_PORT
                JSR     SAY_HELLO_WORLD

                ; --- Halt ---
HALT:
                BRA     HALT            ; Loop forever (or return to monitor)

; ============================================================================
; SAY_HELLO_WORLD - Speaks "Hello World" using the Votrax SC-01A
; ============================================================================
SAY_HELLO_WORLD:
                PSHA
                PSHX

                LDX     #PHONEMES_HELLO ; Point to phoneme string
SAY_LOOP:
                LDAA    0,X             ; Load next phoneme code
                CMPA    #$FF            ; End-of-string marker?
                BEQ     SAY_DONE        ; Yes, we're done

                JSR     SPEAK_PHONEME   ; Send phoneme to synthesizer
                INX                     ; Next phoneme
                BRA     SAY_LOOP

SAY_DONE:
                PULX
                PULA
                RTS

; ============================================================================
; SPEAK_PHONEME - Send one phoneme to the Votrax SC-01A
;   Input: A = phoneme code (6-bit)
; ============================================================================
SPEAK_PHONEME:
                PSHB
SP_WAIT:
                LDAB    SPEECH_STAT     ; Read speech status
                BITB    #$80            ; Test busy flag (bit 7)
                BNE     SP_WAIT         ; Loop while busy

                STAA    SPEECH_PORT     ; Write phoneme code
                JSR     DELAY_SHORT     ; Brief settling delay

                PULB
                RTS

; ============================================================================
; DRIVE_CIRCLE - Drive in approximately a 10-foot diameter circle
;
; Strategy: Set forward speed to medium and steering to a constant
; gentle left turn. The turning radius depends on the steer angle.
; With STEER_LEFT = $20, the Hero Jr turns in roughly a 5-foot radius
; (10-foot diameter) circle at medium forward speed.
;
; Duration is calibrated so the robot completes one full revolution.
; ============================================================================
DRIVE_CIRCLE:
                PSHA
                PSHB

                ; Set steering to gentle left arc
                LDAA    #STEER_LEFT
                STAA    STEER_PORT

                ; Set drive motor to forward, medium speed
                LDAA    #MOTOR_FWD
                STAA    MOTOR_PORT

                ; Run for the time it takes to complete the circle
                ; Outer loop * inner loop ≈ 63 seconds at clock rate
                LDAA    #CIRCLE_TIME
DC_OUTER:
                LDAB    #CIRCLE_INNER
DC_INNER:
                JSR     DELAY_SHORT     ; Small delay per iteration
                ; Optional: check sonar for obstacles
                ; LDAB    SONAR_PORT
                ; CMPB   #$10
                ; BLT    DC_OBSTACLE
                DECB
                BNE     DC_INNER
                DECA
                BNE     DC_OUTER

                ; Stop motors
                CLR     MOTOR_PORT
                CLR     STEER_PORT

                PULB
                PULA
                RTS

; ============================================================================
; DELAY_1SEC - Approximately 1 second delay
; ============================================================================
DELAY_1SEC:
                PSHA
                PSHB
                LDAA    #$05
D1_OUTER:
                LDAB    #$FF
D1_INNER:
                NOP
                NOP
                NOP
                NOP
                DECB
                BNE     D1_INNER
                DECA
                BNE     D1_OUTER
                PULB
                PULA
                RTS

; ============================================================================
; DELAY_SHORT - Short delay (~1-2ms)
; ============================================================================
DELAY_SHORT:
                PSHB
                LDAB    #$60
DS_LOOP:
                NOP
                DECB
                BNE     DS_LOOP
                PULB
                RTS

; ============================================================================
; PHONEME DATA - "HELLO WORLD"
;
; Phoneme sequence approximation:
;   H  -  EH  -  L  -  L  -  OW    (HELLO)
;   [pause]
;   W  -  ER  -  L  -  D            (WORLD)
;
; Each byte is a Votrax SC-01A phoneme code.
; $FF = end of string marker
; ============================================================================
PHONEMES_HELLO:
                ; "HELLO"
                FCB     PH_HH1         ; H
                FCB     PH_EH1         ; EH
                FCB     PH_EL          ; L
                FCB     PH_EL          ; L
                FCB     PH_OW          ; OW
                ; pause between words
                FCB     PH_PA1         ; (pause)
                FCB     PH_PA1         ; (pause)
                ; "WORLD"
                FCB     PH_WW          ; W
                FCB     PH_ER1         ; ER
                FCB     PH_EL          ; L
                FCB     PH_DD1         ; D
                ; end
                FCB     PH_PA0         ; trailing silence
                FCB     $FF             ; end-of-string marker

; ============================================================================
; INTERRUPT VECTORS (6808)
; ============================================================================
                ORG     $FFF8
                FDB     MAIN            ; IRQ vector (unused, point to MAIN)
                FDB     MAIN            ; SWI vector
                FDB     MAIN            ; NMI vector
                FDB     MAIN            ; RESET vector

; ============================================================================
                END     MAIN
