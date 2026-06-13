using FlowBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence;

public sealed class FlowBoardDbContext(DbContextOptions<FlowBoardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardList> BoardLists => Set<BoardList>();
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowBoardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
