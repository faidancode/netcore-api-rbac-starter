using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Common.Extensions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Employees.Dtos;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Modules.Employees;

public interface IEmployeesService
{
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct);
    Task<PagedResult<EmployeeDto>> GetAllAsync(EmployeeListQuery query, CancellationToken ct);
    Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<PositionHistoryDto>> GetPositionHistoriesAsync(Guid id, CancellationToken ct);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public class EmployeesService : IEmployeesService
{
    private readonly AppDbContext _db;

    public EmployeesService(AppDbContext db) => _db = db;

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct)
    {
        // 🔥 Parallel validation → reduce DB round trips
        var nipExistsTask = _db.Employees.AnyAsync(e => e.Nip == request.Nip, ct);

        var userExistsTask = request.UserId.HasValue
            ? _db.Users.AnyAsync(u => u.Id == request.UserId.Value, ct)
            : Task.FromResult(true);

        var userLinkedTask = request.UserId.HasValue
            ? _db.Employees.AnyAsync(e => e.UserId == request.UserId, ct)
            : Task.FromResult(false);

        var positionExistsTask = _db.Positions.AnyAsync(p => p.Id == request.PositionId, ct);

        var departmentExistsTask = request.DepartmentId.HasValue
            ? _db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, ct)
            : Task.FromResult(true);

        var managerExistsTask = request.ManagerId.HasValue
            ? _db.Employees.AnyAsync(e => e.Id == request.ManagerId.Value, ct)
            : Task.FromResult(true);

        await Task.WhenAll(
            nipExistsTask,
            userExistsTask,
            userLinkedTask,
            positionExistsTask,
            departmentExistsTask,
            managerExistsTask
        );

        if (nipExistsTask.Result)
            throw new ConflictException($"Employee with NIP '{request.Nip}' already exists.");

        if (request.UserId.HasValue)
        {
            if (!userExistsTask.Result)
                throw new NotFoundException("User", request.UserId.Value);

            if (userLinkedTask.Result)
                throw new ConflictException("This user is already linked to another employee.");
        }

        if (!positionExistsTask.Result)
            throw new NotFoundException("Position", request.PositionId);

        if (!departmentExistsTask.Result)
            throw new NotFoundException("Department", request.DepartmentId!.Value);

        if (!managerExistsTask.Result)
            throw new NotFoundException("Manager (Employee)", request.ManagerId!.Value);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct); // ✅ cancellation-aware transaction

        var employee = new Employee
        {
            FullName = request.FullName,
            Nip = request.Nip,
            Gender = request.Gender,
            PositionId = request.PositionId,
            DateOfJoining = request.DateOfJoining,
            DateOfActivePosition = request.DateOfActivePosition,
            EmployeeStatus = request.EmployeeStatus,
            IsActive = request.IsActive,
            UserId = request.UserId,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId
        };

        _db.Employees.Add(employee);

        // ✅ Single SaveChanges → prevent partial write + reduce DB calls
        _db.PositionHistories.Add(new PositionHistory
        {
            EmployeeId = employee.Id,
            PositionId = request.PositionId,
            StartDate = request.DateOfActivePosition ?? request.DateOfJoining,
            IsActive = true,
            Notes = "Initial position"
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await GetByIdAsync(employee.Id, ct); // 🔁 propagate ct
    }

    public async Task<PagedResult<EmployeeDto>> GetAllAsync(EmployeeListQuery query, CancellationToken ct)
    {
        var term = query.Q?.Trim();

        var dbQuery = _db.Employees
            .Include(e => e.User)
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term}%";

            dbQuery = dbQuery.Where(e =>
                EF.Functions.ILike(e.FullName, pattern) ||
                (e.Nip != null && EF.Functions.ILike(e.Nip, pattern))
            );
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.IsActive == query.IsActive.Value);
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var limit = query.Limit < 1 ? 10 : Math.Min(query.Limit, 100);

        var sortParam = string.IsNullOrWhiteSpace(query.Sort)
            ? "createdAt:desc"
            : query.Sort;

        dbQuery = ApplySorting(dbQuery, sortParam);

        var total = await dbQuery.CountAsync(ct); // ✅ cancellation-aware

        var items = await dbQuery
            .ApplyPagination(page, limit)
            .Select(e => MapToDto(e))
            .ToListAsync(ct);

        return new PagedResult<EmployeeDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var employee = await _db.Employees
            .Include(e => e.User)
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Employee", id);

        return MapToDto(employee);
    }

    public async Task<IEnumerable<PositionHistoryDto>> GetPositionHistoriesAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == id, ct))
            throw new NotFoundException("Employee", id);

        return await _db.PositionHistories
            .Include(ph => ph.Position)
            .Where(ph => ph.EmployeeId == id)
            .OrderByDescending(ph => ph.StartDate)
            .Select(ph => new PositionHistoryDto(
                ph.Id,
                ph.EmployeeId,
                ph.PositionId,
                ph.Position.Name,
                ph.StartDate,
                ph.EndDate,
                ph.IsActive,
                ph.Notes,
                ph.CreatedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        var employee = await _db.Employees
            .Include(e => e.PositionHistories)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Employee", id);

        if (request.Nip != null && request.Nip != employee.Nip)
        {
            if (await _db.Employees.AnyAsync(e => e.Nip == request.Nip && e.Id != id, ct))
                throw new ConflictException($"NIP '{request.Nip}' is already in use.");

            employee.Nip = request.Nip;
        }

        if (request.UserId != null && request.UserId != employee.UserId)
        {
            if (request.UserId != Guid.Empty)
            {
                if (!await _db.Users.AnyAsync(u => u.Id == request.UserId.Value, ct))
                    throw new NotFoundException("User", request.UserId.Value);

                if (await _db.Employees.AnyAsync(e => e.UserId == request.UserId && e.Id != id, ct))
                    throw new ConflictException("This user is already linked to another employee.");
            }

            employee.UserId = request.UserId == Guid.Empty ? null : request.UserId;
        }

        if (request.DepartmentId.HasValue)
        {
            if (!await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, ct))
                throw new NotFoundException("Department", request.DepartmentId.Value);

            employee.DepartmentId = request.DepartmentId;
        }

        if (request.ManagerId.HasValue)
        {
            if (request.ManagerId == id)
                throw new AppException("An employee cannot be their own manager.", 400);

            if (!await _db.Employees.AnyAsync(e => e.Id == request.ManagerId.Value, ct))
                throw new NotFoundException("Manager (Employee)", request.ManagerId.Value);

            employee.ManagerId = request.ManagerId;
        }

        bool positionChanged = request.PositionId.HasValue && request.PositionId != employee.PositionId;

        if (positionChanged)
        {
            if (!await _db.Positions.AnyAsync(p => p.Id == request.PositionId!.Value, ct))
                throw new NotFoundException("Position", request.PositionId!.Value);
        }

        if (request.FullName != null) employee.FullName = request.FullName;
        if (request.Gender.HasValue) employee.Gender = request.Gender.Value;
        if (request.EmployeeStatus.HasValue) employee.EmployeeStatus = request.EmployeeStatus.Value;
        if (request.IsActive.HasValue) employee.IsActive = request.IsActive.Value;
        if (request.DateOfJoining.HasValue) employee.DateOfJoining = request.DateOfJoining.Value;
        if (request.DateOfActivePosition.HasValue) employee.DateOfActivePosition = request.DateOfActivePosition;

        if (positionChanged)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var activeHistories = await _db.PositionHistories
                .Where(ph => ph.EmployeeId == id && ph.IsActive)
                .ToListAsync(ct);

            foreach (var h in activeHistories)
            {
                h.IsActive = false;
                h.EndDate = today;
            }

            employee.PositionId = request.PositionId!.Value;
            employee.DateOfActivePosition = request.DateOfActivePosition ?? today;

            _db.PositionHistories.Add(new PositionHistory
            {
                EmployeeId = id,
                PositionId = request.PositionId.Value,
                StartDate = request.DateOfActivePosition ?? today,
                IsActive = true
            });

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        else
        {
            await _db.SaveChangesAsync(ct); // ✅ single atomic write
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Employee", id);

        employee.IsDeleted = true;
        employee.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct); // ✅ single write → no transaction needed
    }

    // 🔽 helper tetap sama
    private static IQueryable<Employee> ApplySorting(IQueryable<Employee> q, string? sort)
    {
        return sort switch
        {
            "createdAt:asc" => q.OrderBy(e => e.CreatedAt),
            "createdAt:desc" => q.OrderByDescending(e => e.CreatedAt),
            "fullName:asc" => q.OrderBy(e => e.FullName),
            "fullName:desc" => q.OrderByDescending(e => e.FullName),
            "nip:asc" => q.OrderBy(e => e.Nip),
            "nip:desc" => q.OrderByDescending(e => e.Nip),
            _ => q.OrderByDescending(e => e.CreatedAt)
        };
    }

    private static EmployeeDto MapToDto(Employee e) => new(
        e.Id,
        e.FullName,
        e.Nip,
        e.Gender.ToString(),
        e.EmployeeStatus.ToString(),
        e.IsActive,
        e.DateOfJoining,
        e.DateOfActivePosition,
        e.UserId,
        e.User?.Name,
        e.DepartmentId,
        e.Department?.Name,
        e.PositionId,
        e.Position?.Name ?? string.Empty,
        e.ManagerId,
        e.Manager?.FullName,
        e.CreatedAt,
        e.UpdatedAt
    );
}