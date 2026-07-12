using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class ActivityLogRepository(FlowBoardDbContext context)
    : Repository<ActivityLog>(context), IActivityLogRepository;
