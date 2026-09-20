namespace AspNetCoreDomainLab.Infrastructure.Persistence;

// Bab 12 — constants mencegah schema ownership bergantung pada string tersebar di code.
public static class DatabaseSchemas
{
    public const string Healthcare = "healthcare";
    public const string Industrial = "industrial";
    public const string Logistics = "logistics";
    public const string Banking = "banking";
    public const string Auth = "auth";
    public const string Public = "public";
}
