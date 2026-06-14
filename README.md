private static SwimSubscriptionsDBEntities _context;

public static SwimSubscriptionsDBEntities GetContext()
{
    if (_context == null)
        _context = new SwimSubscriptionsDBEntities();
    return _context;
}
