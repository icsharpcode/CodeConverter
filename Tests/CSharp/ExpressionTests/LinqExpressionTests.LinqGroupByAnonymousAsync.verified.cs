using System.Collections.Generic;
using System.Linq;

public partial class AccountEntry
{
    public object LookupAccountEntryTypeId { get; set; }
    public object LookupAccountEntrySourceId { get; set; }
    public object SponsorId { get; set; }
    public object LookupFundTypeId { get; set; }
    public object StartDate { get; set; }
    public object SatisfiedDate { get; set; }
    public object InterestStartDate { get; set; }
    public object ComputeInterestFlag { get; set; }
    public object SponsorClaimRevision { get; set; }
    public decimal Amount { get; set; }
    public List<object> AccountTransactions { get; set; }
    public List<AccountEntry> AccountEntryClaimDetails { get; set; }
}

internal static partial class Ext
{
    public static IEnumerable<AccountEntry> Reduce(this IEnumerable<AccountEntry> accountEntries)
    {
        return from _accountEntry in accountEntries
               where _accountEntry.Amount > 0m
               group _accountEntry by new
               {
                   _accountEntry.LookupAccountEntryTypeId,
                   _accountEntry.LookupAccountEntrySourceId,
                   _accountEntry.SponsorId,
                   _accountEntry.LookupFundTypeId,
                   _accountEntry.StartDate,
                   _accountEntry.SatisfiedDate,
                   _accountEntry.InterestStartDate,
                   _accountEntry.ComputeInterestFlag,
                   _accountEntry.SponsorClaimRevision
               } into Group
               let _keys = Group.Key
               select new AccountEntry()
               {
                   LookupAccountEntryTypeId = _keys.LookupAccountEntryTypeId,
                   LookupAccountEntrySourceId = _keys.LookupAccountEntrySourceId,
                   SponsorId = _keys.SponsorId,
                   LookupFundTypeId = _keys.LookupFundTypeId,
                   StartDate = _keys.StartDate,
                   SatisfiedDate = _keys.SatisfiedDate,
                   ComputeInterestFlag = _keys.ComputeInterestFlag,
                   InterestStartDate = _keys.InterestStartDate,
                   SponsorClaimRevision = _keys.SponsorClaimRevision,
                   Amount = Group.Sum(accountEntry => accountEntry.Amount),
                   AccountTransactions = new List<object>(),
                   AccountEntryClaimDetails = (from _accountEntry in Group
                                               from _claimDetail in _accountEntry.AccountEntryClaimDetails
                                               select _claimDetail).Reduce().ToList()
               };
    }
}