// -----------------------------------------------------------------------
// <copyright file="GenericRepository.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Data;
using OutreachGenie.Api.Domain.Abstractions;

namespace OutreachGenie.Api.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated via dependency injection")]
public sealed class GenericRepository<T> : IRepository<T>
    where T : class
{
    private readonly OutreachGenieDbContext context;
    private readonly DbSet<T> dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericRepository{T}"/> class.
    /// </summary>
    public GenericRepository(OutreachGenieDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        this.context = context;
        this.dbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public async Task<T?> FindById(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default)
    {
        return await this.dbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Add(T entity, CancellationToken cancellationToken = default)
    {
        await this.dbSet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task Update(T entity, CancellationToken cancellationToken = default)
    {
        this.dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Remove(T entity, CancellationToken cancellationToken = default)
    {
        this.dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
    {
        return await this.context.SaveChangesAsync(cancellationToken);
    }
}

