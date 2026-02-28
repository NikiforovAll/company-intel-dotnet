using CompanyIntel.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CompanyIntel.Api.Data;

public class IngestionDbContext(DbContextOptions<IngestionDbContext> options) : DbContext(options)
{
    public DbSet<IngestionRecord> IngestionRecords => Set<IngestionRecord>();
    public DbSet<ChatSuggestion> ChatSuggestions => Set<ChatSuggestion>();
}
