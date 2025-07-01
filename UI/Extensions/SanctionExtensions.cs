public static class SanctionExtensions
{
    public static bool IsActive(this SanctionDTO sanction)
    {
        var now = DateTime.UtcNow;

        switch (sanction.Type)
        {
            case SanctionType.ban:
                // Un baneo es activo si su fecha de inicio ya pasó
                return sanction.StartDate <= now;

            case SanctionType.suspension:
                // Una suspensión es activa si ha comenzado y no ha terminado
                return sanction.StartDate <= now && sanction.EndDate.HasValue && sanction.EndDate.Value > now;

            default:
                return false; // Otros tipos (como warning) no se consideran activos para login
        }
    }
}
