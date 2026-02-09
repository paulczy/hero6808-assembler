;  Hero 1 serial IF for GotABot
;    Commands:
;              GB     - Get battery status (reserved - not implemented)
;              GDxxxx..xx3fff - Display 6 characters where xx=Ascii Hex representation of Display segment code
;                         ex GD08374F051D88 = "-HERO-" 
;              GH     - Halt
;              GMxxxx - Move Motor, where xxxxxx = Motor Control Opcodes,  
;                         ex GMD30820 = Wait, Relative, Drive Motor, forward $20 units, slow
;                         ex GMD30C20 = Wait, Relative, Drive Motor, reverse $20 units, slow 
;                         ex GMC3A830 = Wait, Abs, Gripper Motor, $30, close slow
;                         ex GMC3AC50 = Wait, Abs, Gripper Motor, $50, open slow
;              GPn    - Get Motor n Position.  0=Exten, 1=Shoulder, 2=Rotate, 3=Pivot, 4=Gripper, 5=Head, 6=Steering, 7=Drive, 9=Odometer
;              GS     - Get Sonar Range  (0.408" per tick")
;              GTxxxx - Talk phonems  where xx=Ascii Hex representation of Phoneme Code
;                         ex GT1B3B1835373FFF  = "HELLO"
;              GUx    - Ultrasonic Enable where (x=1) = Enable and (x=0) = Disable   (reserved - not tested)
;              GX     - eXit
;              GV     - Get Version Number
;     Each command is terminated with a Carriage Return
;  Source code formateed for Motorolla Freeware 8 bit cross assembler as0
;
;  Prepare Hero for download by typing '3A' on Hero
;  Execute by typing 'AD0200' on Hero 

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;ROM subroutines
CLRDIS   EQU $f65b  ; Clear the display
OUTSTR   EQU $f7e5  ; Display embedded character string 
GETC     EQU $e7d0  ; Get a char from the RS-232 in A
PUTC     EQU $e75d  ; Output character in A to RS-232
execmode EQU $f423  ; Main Executive Loop address
NMODE    EQU $fed3  ; change to Native mode / machine language
RMODE    EQU $feda  ; change to Robot (interpretive)  mode
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Variables
irqport EQU $c200
irqpcpy	EQU $0f04
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Opcodes
machine EQU $83    ; machine language code 
robot   EQU $3f    ; change  to robot language mode / AKA Repeat Mode 
exec    EQU $3a    ; EMODE / return to executive mode 
pause   EQU $8f    ; Pause
speak   EQU $72
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;


	ORG $0200 


Main:
	    fcb		machine  
	    jsr		NMODE
	    cli						; enable interrupts
	    ldx		#Trml_Banner
	    jsr		WriteSerial
 	    jsr		CLRDIS          ; Clear the display
   	    jsr		OUTSTR
	    fcb		$5E,$37,$01,$30,$7E,$FE     ; Display "GH-100."
MainLoop:
	    fcb		machine  
	    jsr		NMODE
	    cli
	    ldx		#Trml_Prompt; Show the prompt on the terminal
	    jsr		WriteSerial

WaitForHeader:				; Wait for Command Header "GH"
	    jsr		GetSerial	; Get a char from the RS-232 in A
	    cmpa	#'G 		; Is this the header?
	    bne		#MainLoop	; Try again


GetCmd: 
	    ldx		#CmdBuffer	; Store the command here
GetByte: 
	    jsr		GetSerial	; Get a char from the RS-232 in A
	    staa	0,x
	    cmpa	#13		; 13= Carriage Return = end of the command
	    beq		#Execute	; if so, execute it
	    inx				; Set pointer to the next byte
	    bra		GetByte

Execute:
	    ldx		#CmdBuffer	; The command is stored here
	    ldaa	0,x		; Load the command type
	    cmpa	#'D
	    beq		Disp
	    cmpa	#'M
	    beq		MoveMotor
	    cmpa	#'H
	    bne		Execute1
	    jmp		Halt
Execute1:
	    cmpa	#'S
	    bne		Execute2
	    jmp		SonarPing
Execute2:
	    cmpa	#'P
	    bne		Execute3
	    jmp		GetPosition
Execute3:
	    cmpa	#'T
	    bne		Execute4
	    jmp		Talk
Execute4:
	    cmpa	#'X
	    bne		Execute5
	    jmp		Exit
Execute5:
	    cmpa	#'U
	    bne		Execute6
	    jmp		SonarEn
Execute6:
	    cmpa	#'V
	    ldx		#Trml_Banner	; Show Banner message on the terminal
	    jsr		WriteSerial
	    jmp		MainLoop	 
Execute7:
	    ldx		#Trml_NoMatch   ; Show No Match message on the terminal
	    jsr		WriteSerial
	    jmp		MainLoop		; Done
	




****************     Commands   ***********************

Disp:   ; 
;  Input:  X = point to command type in Command Buffer
;  Output: 
;  Used:   a, x 
    	inx                     ; point to next byte in Command Buffer
        jsr 	AsciiToBin8     ; Load byte 1
    	staa    DispChar
        jsr 	AsciiToBin8     ; Load byte 2
    	staa    DispChar+1
        jsr 	AsciiToBin8     ; Load byte 3
    	staa    DispChar+2
        jsr 	AsciiToBin8     ; Load byte 4
    	staa    DispChar+3
        jsr 	AsciiToBin8     ; Load byte 5
    	staa    DispChar+4
        jsr 	AsciiToBin8     ; Load byte 6
    	oraa    #$80
    	staa    DispChar+5
        jsr     CLRDIS          ; Clear the display
        jsr     OUTSTR
DispChar:	
        fcb     $01,$01,$01,$01,$01,$81    
        jmp     MainLoop        ; Done

MoveMotor:
;  Input:  X = point to command type in Command Buffer
;  Output: 
;  Used:   a, x 
 	inx						; point to next byte in Command Buffer
 	jsr 	AsciiToBin8     ; Get first byte
	jmp		CkOpCode
ValidOp: 
   	staa	MoveMotor1
	jsr 	AsciiToBin8     ; Get second byte
	staa	MoveMotor1+1
	jsr 	AsciiToBin8     ; Get third byte
	staa	MoveMotor1+2
	fcb		robot  
MoveMotor1:
	fcb     $D3,$08,$20     ; motor opcode
	fcb     pause,0,$0A		; PAUSE 5/8 SECOND  
 	jmp     MainLoop  
	
;	fcb     machine			; Enter machine language mode
;	bra		Halt
	       

Halt:
	jsr     $e390       ; abort drive motor
	jsr     $e399       ; abort steering motor
	jsr     $e39d       ; abort arm motors
	jmp     MainLoop    ; 


CkOpCode:		    ; Check that move motor opcode is valid
;  Input:  a = motor opcode
;  Output: aborts motor movement if invalid opcode
;  Used:   
	cmpa	#$D3
	beq		ValidOp
	cmpa	#$DC
	beq		ValidOp
	cmpa	#$C3
	beq		ValidOp
	cmpa	#$CC
	beq		ValidOp
	cmpa	#$E3
	beq		ValidOp
	cmpa	#$EC
	beq		ValidOp
	cmpa	#$F3
	beq		ValidOp
	cmpa	#$FC
	beq		ValidOp
	ldx		#Trml_Invalid   ; Show No Match message on the terminal
	jsr		WriteSerial
	jmp		Halt

SonarEn:
;  Input:  X = point to command type in Command Buffer
;  Output: 
;  Used:   a,  x 
        ldaa    1,x         ; Load Enable (1) / Disable (0) byte
        cmpa    #0
        beq     SonarDis
        fcb     robot       ; ENTER Robot MODE  
        fcb     $45         ; Enable sonar                           
        jmp     MainLoop    ; 
SonarDis:
        fcb     robot       ; ENTER Robot MODE  
        fcb     $55         ; Disable sonar                           
        jmp     MainLoop    ; 



GetPosition:
;  Input:  X = point to command type in Command Buffer
;  Output: 
;  Used:   a, b, x 
        ldaa    1,x             ; Load motor number
        jsr     AsciiToBin4
        staa    ScratchPad+1
        clrb
        stab    ScratchPad
        ldx     ScratchPad		; point index at motor position
        fcb     robot           ; ENTER Robot MODE  
        ldaa    0,x             ; load position
        psha
        fcb     machine         ; Enter machine language mode
        ldx     #Trml_Custom    ; prepare data for write to serial
        pula
        jsr     Bin8ToAsciiWr
        clra
        staa    0,x
        ldx     #Trml_Custom    ; write to serial
        jsr     WriteSerial
        jmp     MainLoop        ; 


SonarPing:
;  Output: a = range
;          b = hits
;  Used:   a, b, x
        cli
        clr     $11         ; CLEAR RANGE VALUE                             
        clr     $10         ; CLEAR HITS VALUE                              
        fcb     robot       ; ENTER Robot MODE  
        fcb     $45         ; Enable sonar                           
        fcb     pause,0,$0A ; PAUSE 5/8 SECOND  
        ldab    $10         ; LOAD HITS IN B                         
        ldaa    $11         ; LOAD RANGE IN A                               
        fcb     machine     ; Enter machine language mode
        tstb                ; DID WE GET ANY HITS?                          
        bne     SonarPing1  ; IF NOT THEN...                                
        ldaa    #$FF        ; ...SET RANGE TO MAXIMUM IF NO HITS                       
SonarPing1:
        ldx     #Trml_Custom; prepare data for write to serial
        jsr     Bin8ToAsciiWr
        clra
        staa    0,x
        ldx     #Trml_Custom; write to serial
        jsr     WriteSerial
        jmp     MainLoop     

Talk:
        inx                     ; point to next byte in Command Buffer
        stx     ScratchPad      ; 
        ldx     #CmdBuffer
        stx     ScratchPad2     ; setup pointer to phonemes in ScratchPad2
GetPhoneme:
        ldx     ScratchPad
        jsr 	AsciiToBin8     ; Get phoneme
        stx     ScratchPad      ; save CmdBuffer pointer
        ldx     ScratchPad2
        staa    0,x
        inx
        stx     ScratchPad2     ; save phonemes pointer in ScratchPad2
        cmpa    #$ff
        bne     GetPhoneme
        cli
        fcb     robot       ; ENTER Robot (interpretive) MODE  
        fcb     speak
        fdb     CmdBuffer
        fcb     machine     ; Enter machine language mode
        jmp     MainLoop     


Exit:
        cli
        ldx     #Trml_Exit
        jsr     WriteSerial
        swi                 ; Software interrupt
        fcb     exec		; Done.  Jump back to Robot Executive


****************   ASCII / Binary Conversions    ***********************
AsciiToBin8:                ; Get 2 Ascii values and convert to  8 bit binary value
;  Input:  X = address of upper nibble
;  Output: a = Binary value
;          X = X + 2
;  Used:   b 
        ldaa	0,x             ; Load upper ASCII nibble
        bsr     AsciiToBin4     ; convert to binary
        asla
        asla
        asla
        asla                    ; * 16
        tab                     ; save in b
        inx
        ldaa	0,x             ; Load lower ASCII nibble
        bsr     AsciiToBin4     ; convert to binary
        aba
        inx
        rts

AsciiToBin4:
;  Input:  a = ascii character
;  Output  a = Binary value
        suba	#'A             ; 
        bcc		AsciiToBin4a
        adda	#7
AsciiToBin4a:
        adda	#10
        rts

Bin8ToAscii:
;  Input:  a = 8 bit Binary
;  Output  a = most significant ASCII character
;          b = least significant ASCII character
;  Used:     
        psha
        anda    #$0F
        bsr     Bin4ToAscii
        tab
        pula
        lsra
        lsra
        lsra
        lsra
        bsr     Bin4ToAscii
        rts     


Bin4ToAscii:           ; Convert 4 bit binary value into Ascii Hex digit
;  Input:  a = 4 bit Binary
;  Output  a = ASCII character
;  Used:    
        adda    #'0
        cmpa    #'9
        bls     Bin4ToAsciiA
        adda    #7
Bin4ToAsciiA:
        rts

Bin8ToAsciiWr:           ; Convert 8 bit binary value into two Ascii Hex digits
;  Input:   a  = 8 bit Binary 
;           X = address to write to
;  Output:  X = X + 2
;  Used:   a, b, x
        jsr     Bin8ToAscii
        staa    0,x     ; MS nibble
        inx
        tba
        staa    0,x     ; LS nibble
        inx
        rts



****************     SERIAL IO   ***********************

GetSerial:              ; Get a character from the serial IF
        clrb            ; Terminal IO
        sei
        jsr     GETC	; Get a char from the RS-232 in A
        anda	#$7F	; Mask to 7 bits
        cli
        rts


WriteSerial:           ; Write a string to the serial IF    
;  Input:  X = NULL terminated string
;  Output  
;  Used:   a, b, x 
        sei
        clrb
        ldaa	0,x
        tsta
        beq		EndOfWrite
        anda	#$7F	; Mask to 7 bits
        jsr		PUTC
        inx
        bra		WriteSerial
        cli
EndOfWrite:	
        rts

****************     SERIAL IO  MISC ***********************

CrLfWr:
;  Input:   X = address to write to
;  Output:  X = X + 2
;  Used:   b, x
        ldab    #13
        stab    0,x
        inx
        ldab    #10
        stab    0,x
        inx
        rts

****************     SERIAL IO STRINGS   ***********************
Trml_Banner:	
	    fcb     13,10       ; LF, CR
	    fcc     "GotAHero "
	    fcc     "Version 1.00" 
	    fcb     13,10
	    fcb     0
Trml_Prompt:
	    fcb     13,10
	    fcc     "> "
	    fcb     0
Trml_Exit:
	    fcc     "Goodbye"
	    fcb     13,10
	    fcb     0
Trml_NoMatch:
	    fcb     13,10
	    fcc     "ERROR: Command not found."
	    fcb     7       ; Bell
	    fcb     13,10
	    fcb     0
Trml_Invalid:
	    fcb     13,10
	    fcc     "ERROR: Invalid Command."
	    fcb     7       ; Bell
	    fcb     13,10
	    fcb     0
Trml_Custom:
	    fcc     "QWERTYUIOP[]"
	    fcb     0
	    RMB     32


****************     Data Buffers   ***********************

CmdBuffer:              ; Command Buffer
	    RMB     128
ScratchPad:
	    RMB     2
ScratchPad2:
	    RMB     2


	END