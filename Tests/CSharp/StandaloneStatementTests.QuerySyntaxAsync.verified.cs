{
    var cmccIds = new List<int>();
    foreach (var scr in _sponsorPayment.SponsorClaimRevisions)
    {
        foreach (var claim in (IEnumerable)((dynamic)scr).Claims)
        {
            if (((dynamic)claim).ClaimSummary is ClaimSummary)
            {
                {
                    var withBlock = (ClaimSummary)((dynamic)claim).ClaimSummary;
                    cmccIds.AddRange(withBlock.UnpaidClaimMealCountCalculationsIds);
                }
            }
        }
    }
}

2 source compilation errors:
BC30451: '_sponsorPayment' is not declared. It may be inaccessible due to its protection level.
BC30002: Type 'ClaimSummary' is not defined.
2 target compilation errors:
CS0103: The name '_sponsorPayment' does not exist in the current context
CS0246: The type or namespace name 'ClaimSummary' could not be found (are you missing a using directive or an assembly reference?)