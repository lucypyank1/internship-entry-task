using krestiki_noliki_api.DTOs;
using krestiki_noliki_api.Models;

namespace krestiki_noliki_api.Services
{
    public interface IGameService
    {

        Task<Game> CreateGameAsync(GameCreateDto dto);
        Task<Game?> GetGameByIdAsync(Guid id);
        Task<Game> MakeMoveAsync(Guid gameId, MoveRequestDto dto, string requestHash);
    }

}
