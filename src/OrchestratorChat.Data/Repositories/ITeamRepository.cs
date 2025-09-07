using OrchestratorChat.Core.Teams;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data.Repositories;

public interface ITeamRepository : IRepository<Team>
{
    /// <summary>
    /// Gets team by ID with complete member information
    /// </summary>
    Task<Team?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets team by session ID
    /// </summary>
    Task<Team?> GetBySessionIdAsync(string sessionId);

    /// <summary>
    /// Adds a new team to the repository
    /// </summary>
    new Task AddAsync(Team team);

    /// <summary>
    /// Updates an existing team
    /// </summary>
    new Task UpdateAsync(Team team);

    /// <summary>
    /// Deletes a team by ID
    /// </summary>
    Task DeleteAsync(Guid teamId);
}