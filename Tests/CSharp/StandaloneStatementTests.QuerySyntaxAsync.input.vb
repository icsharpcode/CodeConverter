Dim cmccIds As New List(Of Integer)
For Each scr In _sponsorPayment.SponsorClaimRevisions
    For Each claim In scr.Claims
        If TypeOf claim.ClaimSummary Is ClaimSummary Then
            With DirectCast(claim.ClaimSummary, ClaimSummary)
                cmccIds.AddRange(.UnpaidClaimMealCountCalculationsIds)
            End With
        End If
    Next
Next