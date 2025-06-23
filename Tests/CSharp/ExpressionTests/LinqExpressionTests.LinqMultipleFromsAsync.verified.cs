private static void LinqSub()
{
    var _result = from _claimProgramSummary in new List<List<List<List<string>>>>()
                  from _claimComponentSummary in _claimProgramSummary.First()
                  from _lineItemCalculation in _claimComponentSummary.Last()
                  select _lineItemCalculation;
}