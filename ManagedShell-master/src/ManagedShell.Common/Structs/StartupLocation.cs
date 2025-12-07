using ManagedShell.Common.Enums;

namespace ManagedShell.Common.Structs
{
    struct StartupLocation
    {
        internal StartupEntryType Type;
        internal string Location;
        internal string ApprovedLocation;
        internal StartupEntryScope Scope;
    }
}
