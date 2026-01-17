// -----------------------------------------------------------------------
// <copyright file="EventActor.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents an actor performing an action.
/// </summary>
public enum EventActor
{
    /// <summary>
    /// Action performed by the AI agent.
    /// </summary>
    Agent,

    /// <summary>
    /// Action performed by a user.
    /// </summary>
    User,

    /// <summary>
    /// Action performed by the system.
    /// </summary>
    System,
}

