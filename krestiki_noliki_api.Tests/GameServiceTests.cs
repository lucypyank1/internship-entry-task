using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using krestiki_noliki_api.Services;
using krestiki_noliki_api.Models;
using krestiki_noliki_api.DTOs;

namespace krestiki_noliki_api.Tests
{
    public class GameServiceTests
    {
        private readonly KrestikiNolikiContext _context;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly GameService _service;

        public GameServiceTests()
        {
            var options = new DbContextOptionsBuilder<KrestikiNolikiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new KrestikiNolikiContext(options);
            _mockConfig = new Mock<IConfiguration>();

            _service = new GameService(_context, _mockConfig.Object);
        }

        // Новый метод для генерации RequestHash
        private string GenerateRequestHash(int player, int x, int y)
        {
            var raw = $"{player}:{x}:{y}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        [Fact(DisplayName = "Создание новой игры с параметрами по умолчанию")]
        public async Task CreateGameAsync_DefaultParameters_ShouldCreateGame()
        {
            _mockConfig.Setup(c => c["BOARD_SIZE"]).Returns("3");
            _mockConfig.Setup(c => c["WIN_LENGTH"]).Returns("3");

            var dto = new GameCreateDto();

            var game = await _service.CreateGameAsync(dto);

            Assert.NotNull(game);
            Assert.Equal(3, game.BoardSize);
            Assert.Equal(3, game.WinLength);
            Assert.Equal("InProgress", game.State);
        }

        [Fact(DisplayName = "Получение существующей игры по ID")]
        public async Task GetGameByIdAsync_ExistingId_ShouldReturnGame()
        {
            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                WinLength = 3,
                State = "InProgress",
                Moves = new List<Move>()
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            var result = await _service.GetGameByIdAsync(gameId);

            Assert.NotNull(result);
            Assert.Equal(gameId, result!.Id);
        }

        [Fact(DisplayName = "Валидный ход - обновление игры")]
        public async Task MakeMoveAsync_ValidMove_ShouldUpdateGame()
        {
            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                WinLength = 3,
                State = "InProgress",
                CurrentTurn = 1,
                Moves = new List<Move>()
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            var dto = new MoveRequestDto { Player = 1, X = 0, Y = 0 };
            string requestHash = GenerateRequestHash(dto.Player, dto.X, dto.Y);

            var updatedGame = await _service.MakeMoveAsync(gameId, dto, requestHash);

            Assert.NotNull(updatedGame);
            Assert.Single(updatedGame.Moves);
            Assert.Equal(1, updatedGame.Moves[0].Player);
            Assert.Equal(0, updatedGame.Moves[0].X);
            Assert.Equal(0, updatedGame.Moves[0].Y);
            Assert.Equal(2, updatedGame.CurrentTurn);
        }

       
        [Fact(DisplayName = "Проверка победы")]
        public async Task MakeMoveAsync_WinningMove_ShouldSetGameStateToWin()
        {
            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                WinLength = 3,
                State = "InProgress",
                CurrentTurn = 1,
                Moves = new List<Move>
                {
                    new Move
                    {
                        GameId = gameId,
                        Player = 1,
                        X = 0,
                        Y = 0,
                        RequestHash = GenerateRequestHash(1, 0, 0),
                        CreatedAt = DateTime.UtcNow.AddSeconds(-3)
                    },
                    new Move
                    {
                        GameId = gameId,
                        Player = 1,
                        X = 1,
                        Y = 0,
                        RequestHash = GenerateRequestHash(1, 1, 0),
                        CreatedAt = DateTime.UtcNow.AddSeconds(-2)
                    }
                }
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            var dto = new MoveRequestDto { Player = 1, X = 2, Y = 0 };
            string requestHash = GenerateRequestHash(dto.Player, dto.X, dto.Y);

            var updatedGame = await _service.MakeMoveAsync(gameId, dto, requestHash);

            Assert.Equal("Player1Won", updatedGame.State);
        }

        [Fact(DisplayName = "Проверка ничьей")]
        public async Task MakeMoveAsync_Draw_ShouldSetGameStateToDraw()
        {
            var gameId = Guid.NewGuid();

            var moves = new List<Move>();
            int size = 3;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    moves.Add(new Move
                    {
                        GameId = gameId,
                        Player = (x + y) % 2 + 1,
                        X = x,
                        Y = y,
                        RequestHash = GenerateRequestHash((x + y) % 2 + 1, x, y),
                        CreatedAt = DateTime.UtcNow.AddSeconds(-(size * size - (x * size + y)))
                    });
                }
            }
            moves.RemoveAt(moves.Count - 1); 

            var game = new Game
            {
                Id = gameId,
                BoardSize = size,
                WinLength = 3,
                State = "InProgress",
                CurrentTurn = 1,
                Moves = moves
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            var dto = new MoveRequestDto { Player = 1, X = 2, Y = 2 };
            string requestHash = GenerateRequestHash(dto.Player, dto.X, dto.Y);

            var updatedGame = await _service.MakeMoveAsync(gameId, dto, requestHash);

            Assert.Equal("Draw", updatedGame.State);
        }

        [Fact(DisplayName = "Повторный ход с тем же хэшем (идемпотентность)")]
        public async Task MakeMoveAsync_SameRequestHash_ReturnsSameGame()
        {
            var gameId = Guid.NewGuid();
            var requestHash = "somehash";

            var existingMove = new Move
            {
                GameId = gameId,
                Player = 1,
                X = 0,
                Y = 0,
                RequestHash = requestHash,
                CreatedAt = DateTime.UtcNow
            };

            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                WinLength = 3,
                State = "InProgress",
                CurrentTurn = 2,
                Moves = new List<Move> { existingMove }
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            var dto = new MoveRequestDto { Player = 1, X = 0, Y = 0 };

            var result = await _service.MakeMoveAsync(gameId, dto, requestHash);

            Assert.Equal(1, result.Moves.Count);
            Assert.Equal(requestHash, result.Moves[0].RequestHash);
        }
    }
}
