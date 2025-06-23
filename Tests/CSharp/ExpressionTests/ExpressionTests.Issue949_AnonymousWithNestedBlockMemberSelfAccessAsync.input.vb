Dim anonymousType = New With {
Key .A = 1, 'Comment gets duplicated
Key .B = New With {
        Key .A = 2,
        Key .B = .A
    }
}