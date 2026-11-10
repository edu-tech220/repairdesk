using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Exceptions;

public class InvalidCaseTransitionException : Exception
{
    public InvalidCaseTransitionException(CaseStatus from, string action)
        : base($"Cannot {action} a case while it is in status '{from}'.")
    {
    }
}
