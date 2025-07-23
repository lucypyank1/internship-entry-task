using krestiki_noliki_api.DTOs;
using krestiki_noliki_api.Models;
using Microsoft.EntityFrameworkCore;

namespace krestiki_noliki_api.Services
{
    public class GameService : IGameService
    {
        private readonly KrestikiNolikiContext _context;
        private readonly IConfiguration _config;

        public GameService(KrestikiNolikiContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<Game> CreateGameAsync(GameCreateDto dto)
        {
            int defaultBoardSize = int.Parse(_config["BOARD_SIZE"] ?? "3");
            int defaultWinLength = int.Parse(_config["WIN_LENGTH"] ?? "3");

            int boardSize = dto.BoardSize ?? defaultBoardSize;
            int winLength = dto.WinLength ?? defaultWinLength;

            // Валидация параметров
            if (boardSize < 3 || boardSize > 20)
                throw new Exception("Размер доски должен быть от 3 до 20");

            if (winLength < 3 || winLength > boardSize)
                throw new Exception("Длина выигрышной последовательности должна быть от 3 до размера доски");

            var game = new Game
            {
                Id = Guid.NewGuid(),
                BoardSize = boardSize,
                WinLength = winLength,
                State = "InProgress",
                CurrentTurn = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return game;
        }

        public async Task<Game?> GetGameByIdAsync(Guid id)
        {
            return await _context.Games
                .Include(g => g.Moves)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Game> MakeMoveAsync(Guid gameId, MoveRequestDto dto, string requestHash)
        {
            var game = await _context.Games.Include(g => g.Moves).FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
                throw new Exception("Игра не найдена");

            if (game.State != "InProgress")
                throw new Exception("Игра уже завершена");

            // Валидация входных данных хода
            if (dto.Player != 1 && dto.Player != 2)
                throw new Exception("Неверный номер игрока. Допустимы только 1 или 2");

            if (dto.X < 0 || dto.X >= game.BoardSize || dto.Y < 0 || dto.Y >= game.BoardSize)
                throw new Exception($"Координаты хода должны быть в пределах от 0 до {game.BoardSize - 1}");

            // Проверяем идемпотентность по requestHash
            var existingMove = await _context.Moves.FirstOrDefaultAsync(m => m.GameId == gameId && m.RequestHash == requestHash);
            if (existingMove != null)
                return game;

            // Проверяем занятость клетки напрямую из базы
            var cellTaken = await _context.Moves.AnyAsync(m => m.GameId == gameId && m.X == dto.X && m.Y == dto.Y);
            if (cellTaken)
                throw new Exception("Ячейка уже занята");

            // Логика случайного реверса игрока (как у тебя было)
            var reverse = (game.Moves.Count + 1) % 3 == 0 && new Random().NextDouble() < 0.1;
            int actualPlayer = reverse ? 3 - dto.Player : dto.Player;

            var move = new Move
            {
                GameId = game.Id,
                Player = actualPlayer,
                X = dto.X,
                Y = dto.Y,
                IsReversed = reverse,
                CreatedAt = DateTime.UtcNow,
                RequestHash = requestHash
            };

            _context.Moves.Add(move);
            await _context.SaveChangesAsync();

            // Перезагружаем игру с ходами
            game = await _context.Games
                .Include(g => g.Moves)
                .FirstOrDefaultAsync(g => g.Id == gameId)
                ?? throw new Exception("Игра не найдена после сохранения хода");

            game.Moves = game.Moves.OrderBy(m => m.CreatedAt).ToList();

            var allMoves = game.Moves.ToList();

            bool isWin = CheckWin(
                allMoves,
                game.BoardSize,
                game.WinLength,
                move.X,
                move.Y,
                actualPlayer
            );

            if (isWin)
            {
                game.State = actualPlayer == 1 ? "Player1Won" : "Player2Won";
            }
            else if (allMoves.Count >= game.BoardSize * game.BoardSize)
            {
                game.State = "Draw";
            }
            else
            {
                game.CurrentTurn = 3 - game.CurrentTurn;
            }

            game.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return game;
        }


        private bool CheckWin(List<Move> moves, int boardSize, int winLength, int lastX, int lastY, int player)
        {
            int[][] directions = new[]
            {
                new[] { 1, 0 },  // горизонталь
                new[] { 0, 1 },  // вертикаль
                new[] { 1, 1 },  // диагональ вправо-вниз
                new[] { 1, -1 }  // диагональ вправо-вверх
            };

            var playerMoves = moves
                .Where(m => m.Player == player)
                .Select(m => (m.X, m.Y))
                .ToHashSet();

            foreach (var dir in directions)
            {
                int count = 1;

                // вперёд
                int dx = dir[0], dy = dir[1];
                int x = lastX + dx, y = lastY + dy;
                while (playerMoves.Contains((x, y)))
                {
                    count++;
                    x += dx;
                    y += dy;
                }

                // назад
                x = lastX - dx;
                y = lastY - dy;
                while (playerMoves.Contains((x, y)))
                {
                    count++;
                    x -= dx;
                    y -= dy;
                }

                if (count >= winLength)
                    return true;
            }

            return false;
        }
    }
}
