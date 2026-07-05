using System;
using Lobby.Application.Entities;

namespace Lobby.Application.Models;

public record ServerInstanceModel(
    Guid Id,
    string ContainerId,
    string Image,
    string Name,
    int Port,
    DateTime? EmptySince,
    DateTime CreatedAt,
    int PlayerCount,
    DateTime UpdatedAt,
    ServerInstanceStatus Status
);