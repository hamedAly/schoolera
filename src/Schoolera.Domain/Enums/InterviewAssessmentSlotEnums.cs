namespace Schoolera.Domain.Enums;

public enum SlotKind
{
    Interview = 1,
    Assessment = 2,
}

public enum SlotDeliveryMode
{
    Online = 1,
    OnSite = 2,
}

public enum SlotStatus
{
    Draft = 1,
    Open = 2,
    Closed = 3,
    Cancelled = 4,
}

public enum SlotResourceKind
{
    StaffMember = 1,
}

public enum SlotRecurrenceFrequency
{
    Daily = 1,
    Weekly = 2,
}
