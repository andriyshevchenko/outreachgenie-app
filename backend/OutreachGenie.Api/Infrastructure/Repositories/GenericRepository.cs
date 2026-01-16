using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Data;
using OutreachGenie.Api.Domain.Abstractions;

namespace OutreachGenie.Api.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public class GenericRepository<T> : IRepository<T>
    where T : class
{
    private readonly OutreachGenieDbContext _context;
    private readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericRepository{T}"/> class.
    /// </summary>
    public GenericRepository(OutreachGenieDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        this._context = context;
        this._dbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public async Task<T?> FindById(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default)
    {
        return await this._dbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Add(T entity, CancellationToken cancellationToken = default)
    {
        await this._dbSet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task Update(T entity, CancellationToken cancellationToken = default)
    {
        this._dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Remove(T entity, CancellationToken cancellationToken = default)
    {
        this._dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
    {
        return await this._context.SaveChangesAsync(cancellationToken);
    }
}
