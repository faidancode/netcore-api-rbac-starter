using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Positions.Dtos;
using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Common.Extensions;

namespace netcore_api_rbac_starter.Modules.Positions;

public interface IPositionsService
{
    Task<PositionDto> CreateAsync(CreatePositionRequest request);
    Task<PagedResult<PositionDto>> GetAllAsync(ListPositionQuery query);
    Task<PositionDto> GetByIdAsync(Guid id);
    Task<PositionDto> UpdateAsync(Guid id, UpdatePositionRequest request);
    Task DeleteAsync(Guid id);
}

public class PositionsService : IPositionsService
{
    private readonly AppDbContext _db;

    public PositionsService(AppDbContext db) => _db = db;

    public async Task<PositionDto> CreateAsync(CreatePositionRequest request)
    {
        var deptExists = await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId);
        if (!deptExists)
            throw new NotFoundException("Department", request.DepartmentId);

        var exists = await _db.Positions
            .AnyAsync(p => p.DepartmentId == request.DepartmentId && p.Name == request.Name);
        if (exists)
            throw new ConflictException($"Position '{request.Name}' already exists in this department.");

        var position = new Position
        {
            Name = request.Name,
            Description = request.Description,
            DepartmentId = request.DepartmentId
        };

        _db.Positions.Add(position);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(position.Id);
    }

    public async Task<PagedResult<PositionDto>> GetAllAsync(ListPositionQuery query)
    {
        var term = (query.Search ?? query.Q)?.Trim();
        var dbQuery = _db.Positions.AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term}%";
            dbQuery = dbQuery.Where(d => EF.Functions.ILike(d.Name, pattern) ||
                                         (d.Description != null && EF.Functions.ILike(d.Description, pattern)));
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var limit = query.Limit < 1 ? 10 : Math.Min(query.Limit, 100);

        var sortParam = query.Sort ?? "createdAt:desc";
        dbQuery = dbQuery.ApplySorting(sortParam);

        var total = await dbQuery.CountAsync();
        var items = await dbQuery
            .ApplyPagination(page, limit)
            .Select(d => MapToDto(d))
            .ToListAsync();

        return new PagedResult<PositionDto>
        {
            Items = items,
            Total = total
        };
    }

    public async Task<PositionDto> GetByIdAsync(Guid id)
    {
        var position = await _db.Positions
            .Include(p => p.Department)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Position", id);

        return MapToDto(position);
    }

    public async Task<PositionDto> UpdateAsync(Guid id, UpdatePositionRequest request)
    {
        var position = await _db.Positions
            .Include(p => p.Department)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Position", id);

        var targetDeptId = request.DepartmentId ?? position.DepartmentId;

        if (request.DepartmentId.HasValue && request.DepartmentId != position.DepartmentId)
        {
            var deptExists = await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value);
            if (!deptExists)
                throw new NotFoundException("Department", request.DepartmentId.Value);
            position.DepartmentId = request.DepartmentId.Value;
        }

        if (request.Name != null && (request.Name != position.Name || targetDeptId != position.DepartmentId))
        {
            var nameExists = await _db.Positions
                .AnyAsync(p => p.DepartmentId == targetDeptId && p.Name == request.Name && p.Id != id);
            if (nameExists)
                throw new ConflictException($"Position '{request.Name}' already exists in this department.");
            position.Name = request.Name;
        }

        if (request.Description != null) position.Description = request.Description;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(position.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var position = await _db.Positions.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Position", id);

        position.IsDeleted = true;
        position.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static PositionDto MapToDto(Position p) =>
        new(p.Id, p.Name, p.Description, p.DepartmentId, p.Department!.Name, p.CreatedAt, p.UpdatedAt);
}