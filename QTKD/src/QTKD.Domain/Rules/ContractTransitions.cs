using QTKD.Domain.Enums;
using QTKD.Domain.Exceptions;

namespace QTKD.Domain.Rules;

/// <summary>
/// Bảng chuyển trạng thái hợp lệ của Contract (sơ đồ trạng thái Phase 1).
/// CTR-03: Draft không được chuyển thẳng sang Approved.
/// </summary>
public static class ContractTransitions
{
    private static readonly Dictionary<ContractStatus, ContractStatus[]> Allowed = new()
    {
        [ContractStatus.Draft]             = [ContractStatus.PendingApproval, ContractStatus.Cancelled],
        [ContractStatus.PendingApproval]   = [ContractStatus.Approved, ContractStatus.Rejected, ContractStatus.RevisionRequested],
        [ContractStatus.RevisionRequested] = [ContractStatus.PendingApproval, ContractStatus.Cancelled],
        [ContractStatus.Approved]          = [ContractStatus.Active, ContractStatus.Cancelled],
        [ContractStatus.Active]            = [ContractStatus.Expired, ContractStatus.Cancelled],
    };

    public static bool CanTransition(ContractStatus from, ContractStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static void EnsureCanTransition(ContractStatus from, ContractStatus to)
    {
        if (CanTransition(from, to)) return;

        var code = from == ContractStatus.Draft && to == ContractStatus.Approved ? "CTR-03" : "WRONG_STATE";
        throw new BusinessRuleException(
            code,
            code == "CTR-03"
                ? "Vi phạm CTR-03: không được chuyển trực tiếp từ Draft sang Approved."
                : $"Không thể chuyển trạng thái từ {from} sang {to}.",
            new[] { $"Từ: {from}", $"Sang: {to}" });
    }
}
