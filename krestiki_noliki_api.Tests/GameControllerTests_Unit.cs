using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using krestiki_noliki_api.Controllers;
using krestiki_noliki_api.DTOs;
using krestiki_noliki_api.Models;
using krestiki_noliki_api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace krestiki_noliki_api.Tests
{
    public class GameControllerTests_Unit
    {
        private readonly Mock<IGameService> _mockService;
        private readonly GameController _controller;

        public GameControllerTests_Unit()
        {
            _mockService = new Mock<IGameService>();
            _controller = new GameController(_mockService.Object);
        }

        [Fact]
        // “ест создани€ новой игры - провер€ем, что возвращаетс€ CreatedAtAction с игрой
        public async Task CreateGame_ReturnsCreatedGame()
        {
            var dto = new GameCreateDto { BoardSize = 3, WinLength = 3 };
            var game = new Game { Id = Guid.NewGuid(), BoardSize = 3, WinLength = 3 };

            _mockService.Setup(s => s.CreateGameAsync(dto)).ReturnsAsync(game);

            var result = await _controller.CreateGame(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(GameController.GetGame), createdResult.ActionName);
            Assert.Equal(game.Id, ((Game)createdResult.Value).Id);
        }

        [Fact]
        // “ест получени€ игры по существующему ID - провер€ем возврат Ok с игрой
        public async Task GetGame_ExistingId_ReturnsOkWithGame()
        {
            var gameId = Guid.NewGuid();
            var game = new Game { Id = gameId };

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync(game);

            var result = await _controller.GetGame(gameId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(game, okResult.Value);
        }

        [Fact]
        // “ест получени€ игры по несуществующему ID - провер€ем возврат NotFound
        public async Task GetGame_NonExistingId_ReturnsNotFound()
        {
            var gameId = Guid.NewGuid();

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync((Game?)null);

            var result = await _controller.GetGame(gameId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        // “ест успешного хода игрока - провер€ем возврат Ok с обновлЄнной игрой
        public async Task MakeMove_ValidMove_ReturnsOkWithGame()
        {
            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                CurrentTurn = 1,
                Moves = new List<Move>()
            };
            var moveDto = new MoveRequestDto { Player = 1, X = 0, Y = 0 };

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync(game);
            _mockService.Setup(s => s.MakeMoveAsync(It.IsAny<Guid>(), moveDto, It.IsAny<string>()))
                        .ReturnsAsync(game);

            var result = await _controller.MakeMove(gameId, moveDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(game, okResult.Value);
        }

        [Fact]
        // “ест хода с неверным ID игры - провер€ем NotFound
        public async Task MakeMove_GameNotFound_ReturnsNotFound()
        {
            var gameId = Guid.NewGuid();
            var moveDto = new MoveRequestDto { Player = 2, X = 0, Y = 0 };

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync((Game?)null);

            var result = await _controller.MakeMove(gameId, moveDto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("»гра не найдена", notFoundResult.Value);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(3, 0)]
        [InlineData(0, -1)]
        [InlineData(0, 3)]
        // “ест ходов с координатами вне диапазона - провер€ем BadRequest
        public async Task MakeMove_InvalidCoordinates_ReturnsBadRequest(int x, int y)
        {
            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                CurrentTurn = 1,
                Moves = new List<Move>()
            };
            var moveDto = new MoveRequestDto { Player = 1, X = x, Y = y };

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync(game);

            var result = await _controller.MakeMove(gameId, moveDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains(" оордината", badRequest.Value.ToString());
        }

        [Fact]
        // “ест хода не текущего игрока - провер€ем BadRequest с правильным сообщением
        public async Task MakeMove_NotCurrentPlayer_ReturnsBadRequest()
        {
            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                CurrentTurn = 1,
                Moves = new List<Move>()
            };
            var moveDto = new MoveRequestDto { Player = 2, X = 0, Y = 0 };

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync(game);

            var result = await _controller.MakeMove(gameId, moveDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"—ейчас ход игрока {game.CurrentTurn}", badRequest.Value);
        }

        [Fact]
        // “ест хода в зан€тую клетку - провер€ем BadRequest с сообщением
        public async Task MakeMove_CellAlreadyTaken_ReturnsBadRequest()
        {
            var gameId = Guid.NewGuid();
            var moves = new List<Move> { new Move { X = 1, Y = 1 } };
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                CurrentTurn = 1,
                Moves = moves
            };
            var moveDto = new MoveRequestDto { Player = 1, X = 1, Y = 1 };

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync(game);

            var result = await _controller.MakeMove(gameId, moveDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Ёта клетка уже зан€та", badRequest.Value);
        }

        [Fact]
        // “ест обработки ошибки при попытке хода. провер€ем, что BadRequest содержит сообщение ошибки
        public async Task MakeMove_ServiceThrowsException_ReturnsBadRequestWithError()
        {
            var gameId = Guid.NewGuid();
            var game = new Game
            {
                Id = gameId,
                BoardSize = 3,
                CurrentTurn = 1,
                Moves = new List<Move>()
            };
            var moveDto = new MoveRequestDto { Player = 1, X = 0, Y = 0 };

            _mockService.Setup(s => s.GetGameByIdAsync(gameId)).ReturnsAsync(game);
            _mockService.Setup(s => s.MakeMoveAsync(It.IsAny<Guid>(), moveDto, It.IsAny<string>()))
                        .ThrowsAsync(new Exception("Some error"));

            var result = await _controller.MakeMove(gameId, moveDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Some error", badRequest.Value.ToString());
        }
    }
}
