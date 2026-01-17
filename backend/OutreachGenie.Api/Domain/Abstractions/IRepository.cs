// -----------------------------------------------------------------------
// <copyright file="IRepository.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Domain.Abstractions;

/// <summary>
/// Repository for accessing domain entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T>
    where T : class
{
    /// <summary>
    /// Finds an entity by its identifier.
    /// </summary>
    Task<T?> FindById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    Task<IEnumerable<T>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    Task Add(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    Task Update(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity.
    /// </summary>
    Task Remove(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes.
    /// </summary>
    Task<int> SaveChanges(CancellationToken cancellationToken = default);
}

