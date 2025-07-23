using krestiki_noliki_api.DTOs;
using krestiki_noliki_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace krestiki_noliki_api.Controllers
{
    [ApiController]
    [Route("games")]
    public class GameController : ControllerBase
    {
        private readonly IGameService _service;

        public GameController(IGameService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGame([FromBody] GameCreateDto dto)
        {
            var game = await _service.CreateGameAsync(dto);
            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGame(Guid id)
        {
            var game = await _service.GetGameByIdAsync(id);
            if (game == null)
                return NotFound();

            return Ok(game);
        }

        [HttpPost("{id}/moves")]
        public async Task<IActionResult> MakeMove(Guid id, [FromBody] MoveRequestDto dto)
        {
            // Получаем игру по ID
            var game = await _service.GetGameByIdAsync(id);
            if (game == null)
                return NotFound("Игра не найдена");

            // Валидация координат
            if (dto.X < 0 || dto.X >= game.BoardSize)
                return BadRequest($"Координата X должна быть в диапазоне от 0 до {game.BoardSize - 1}");

            if (dto.Y < 0 || dto.Y >= game.BoardSize)
                return BadRequest($"Координата Y должна быть в диапазоне от 0 до {game.BoardSize - 1}");

            // Проверка текущего игрока
            if (dto.Player != game.CurrentTurn)
                return BadRequest($"Сейчас ход игрока {game.CurrentTurn}");

            // Проверка, что клетка свободна
            if (game.Moves.Any(m => m.X == dto.X && m.Y == dto.Y))
                return BadRequest("Эта клетка уже занята");

            // Генерация хеша для защиты от повторных или поддельных ходов
            string input = $"{dto.Player}:{dto.X}:{dto.Y}";

            string hash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create()
                    .ComputeHash(System.Text.Encoding.UTF8.GetBytes(input))
            );

            try
            {
                var move = await _service.MakeMoveAsync(id, dto, hash);
                return Ok(move);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


    }

}
