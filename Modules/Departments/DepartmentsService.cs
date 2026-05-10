using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Departments.Dtos;
using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Common.Extensions;
using netcore_api_rbac_starter.Security;

namespace netcore_api_rbac_starter.Modules.Departments;

public interface IDepartmentsService
{
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken ct);
    Task<PagedResult<DepartmentDto>> GetAllAsync(ListDepartmentQuery query, CancellationToken ct);
    Task<DepartmentDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public class DepartmentsService : IDepartmentsService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventDispatcher? _eventDispatcher;
    private readonly ILogger<DepartmentsService> _logger;

    public DepartmentsService(
        AppDbContext db,
        ICurrentUserService currentUserService,
        ILogger<DepartmentsService> logger,
        IEventDispatcher? eventDispatcher = null)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken ct)
    {
        var requestId = _currentUserService.RequestId;
        var userId = _currentUserService.UserId;

        _logger.LogInformation("RequestId: {RequestId}, UserId: {UserId}", requestId, userId);

        var exists = await _db.Departments
            .AnyAsync(d => d.Name == request.Name, ct);

        if (exists)
            throw new ConflictException($"Department '{request.Name}' already exists.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var dept = new Department
        {
            Name = request.Name,
            Description = request.Description
        };

        _db.Departments.Add(dept);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await DispatchAuditAsync(
            new DepartmentCreatedEvent(dept.Id, dept.Name, dept.Description, requestId),
            ct);

        return MapToDto(dept);
    }

    public async Task<PagedResult<DepartmentDto>> GetAllAsync(ListDepartmentQuery query, CancellationToken ct)
    {
        var term = (query.Search ?? query.Q)?.Trim();
        var dbQuery = _db.Departments.AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term}%";
            dbQuery = dbQuery.Where(d =>
                EF.Functions.ILike(d.Name, pattern) ||
                (d.Description != null && EF.Functions.ILike(d.Description, pattern))
            );
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var limit = query.Limit < 1 ? 10 : Math.Min(query.Limit, 100);

        var sortParam = query.Sort ?? "createdAt:desc";
        dbQuery = dbQuery.ApplySorting(sortParam);

        var total = await dbQuery.CountAsync(ct);

        var items = await dbQuery
            .ApplyPagination(page, limit)
            .Select(d => MapToDto(d))
            .ToListAsync(ct);

        return new PagedResult<DepartmentDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public async Task<DepartmentDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var dept = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("Department", id);

        return MapToDto(dept);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct)
    {
        var dept = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("Department", id);

        var before = new
        {
            dept.Id,
            dept.Name,
            dept.Description,
            dept.IsActive
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (request.Name != null && request.Name != dept.Name)
        {
            var nameExists = await _db.Departments
                .AnyAsync(d => d.Name == request.Name, ct);

            if (nameExists)
                throw new ConflictException($"Department '{request.Name}' already exists.");

            dept.Name = request.Name;
        }

        if (request.Description != null)
            dept.Description = request.Description;

        if (request.IsActive.HasValue)
            dept.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await DispatchAuditAsync(
            new DepartmentUpdatedEvent(
                dept.Id,
                before,
                new { dept.Id, dept.Name, dept.Description, dept.IsActive },
                _currentUserService.RequestId),
            ct);

        return MapToDto(dept);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var dept = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("Department", id);

        var before = new
        {
            dept.Id,
            dept.Name,
            dept.Description,
            dept.IsActive,
            dept.IsDeleted
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        dept.IsDeleted = true;
        dept.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await DispatchAuditAsync(
            new DepartmentDeletedEvent(dept.Id, before, _currentUserService.RequestId),
            ct);
    }

    private static DepartmentDto MapToDto(Department d) =>
        new(d.Id, d.Name, d.Description, d.IsActive, d.CreatedAt, d.UpdatedAt);

    private Task DispatchAuditAsync(IAuditableEvent auditEvent, CancellationToken ct)
        => _eventDispatcher?.DispatchAsync(auditEvent, ct) ?? Task.CompletedTask;
}
