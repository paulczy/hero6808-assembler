        ORG $0100

        LDS #$0FDF
        STS $20
        BITA #$02
        BITB $30
        INCB
        DECB
        PSHB
        PULB
        TSX
        TXS
        RTI

        END
