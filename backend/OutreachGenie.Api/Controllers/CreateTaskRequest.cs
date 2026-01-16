using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;
using OutreachGenie.Api.Domain.Services;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Request model for creating a task.
/// </summary>
/// <param name="Title">Task title.</param>
/// <param name="Description">Task description.</param>
/// <param name="OrderIndex">Task order index.</param>
/// <param name="RequiresApproval">Whether task requires approval.</param>
public record CreateTaskRequest(string Title, string Description, int? OrderIndex, bool? RequiresApproval);
