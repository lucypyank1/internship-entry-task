using krestiki_noliki_api.DTOs;
using krestiki_noliki_api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

public class GameControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GameControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGame_ShouldReturnCreatedGame()
    {
        var createDto = new GameCreateDto { BoardSize = 3, WinLength = 3 };

        var response = await _client.PostAsJsonAsync("/games", createDto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<GameCreateDto>();
        Assert.NotNull(game);
        Assert.Equal(3, createDto.BoardSize);
    }

    [Fact]
    public async Task GetGame_ShouldReturnGame()
    {
        var createDto = new GameCreateDto { BoardSize = 3, WinLength = 3 };
        var createResponse = await _client.PostAsJsonAsync("/games", createDto);
        var createdGame = await createResponse.Content.ReadFromJsonAsync<Game>();

        var getResponse = await _client.GetAsync($"/games/{createdGame.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task MakeMove_ValidMove_ShouldReturnOk()
    {
        var createDto = new GameCreateDto { BoardSize = 3, WinLength = 3 };
        var createResponse = await _client.PostAsJsonAsync("/games", createDto);
        var createdGame = await createResponse.Content.ReadFromJsonAsync<Game>();

        var moveDto = new MoveRequestDto { Player = 1, X = 0, Y = 0 };
        var moveResponse = await _client.PostAsJsonAsync($"/games/{createdGame.Id}/moves", moveDto);

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
    }

    [Fact]
    public async Task MakeMove_CellTaken_ShouldReturnBadRequest()
    {
        var createDto = new GameCreateDto { BoardSize = 3, WinLength = 3 };
        var createResponse = await _client.PostAsJsonAsync("/games", createDto);
        var createdGame = await createResponse.Content.ReadFromJsonAsync<Game>();

        var moveDto = new MoveRequestDto { Player = 1, X = 0, Y = 0 };
        await _client.PostAsJsonAsync($"/games/{createdGame.Id}/moves", moveDto);

        // Попытка поставить в уже занятую клетку
        var moveDto2 = new MoveRequestDto { Player = 2, X = 0, Y = 0 };
        var moveResponse2 = await _client.PostAsJsonAsync($"/games/{createdGame.Id}/moves", moveDto2);

        Assert.Equal(HttpStatusCode.BadRequest, moveResponse2.StatusCode);
    }
}
