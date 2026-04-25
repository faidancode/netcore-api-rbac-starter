using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Departments.Dtos;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Modules.Departments;

public interface IDepartmentsService
{
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request);
    Task<IEnumerable<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto> GetByIdAsync(Guid id);
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentRequest request);
    Task DeleteAsync(Guid id);
}

public class DepartmentsService : IDepartmentsService
{
    private readonly AppDbContext _db;

    public DepartmentsService(AppDbContext db) => _db = db;

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request)
    {
        var exists = await _db.Departments.AnyAsync(d => d.Name == request.Name);
        if (exists)
            throw new ConflictException($"Department '{request.Name}' already exists.");

        var dept = new Department { Name = request.Name, Description = request.Description };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();
        return MapToDto(dept);
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        return await _db.Departments
            .OrderBy(d => d.Name)
            .Select(d => MapToDto(d))
            .ToListAsync();
    }

    public async Task<DepartmentDto> GetByIdAsync(Guid id)
    {
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Department", id);
        return MapToDto(dept);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentRequest request)
    {
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Department", id);

        if (request.Name != null && request.Name != dept.Name)
        {
            var nameExists = await _db.Departments.AnyAsync(d => d.Name == request.Name);
            if (nameExists)
                throw new ConflictException($"Department '{request.Name}' already exists.");
            dept.Name = request.Name;
        }

        if (request.Description != null) dept.Description = request.Description;

        await _db.SaveChangesAsync();
        return MapToDto(dept);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Department", id);

        dept.IsDeleted = true;
        dept.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static DepartmentDto MapToDto(Department d) =>
        new(d.Id, d.Name, d.Description, d.CreatedAt, d.UpdatedAt);
}