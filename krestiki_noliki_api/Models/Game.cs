using System;
using System.Collections.Generic;

namespace krestiki_noliki_api.Models;

public partial class Game
{
    public Guid Id { get; set; }

    public int BoardSize { get; set; }

    public int WinLength { get; set; }

    public string State { get; set; } = null!;

    public int CurrentTurn { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<Move> Moves { get; set; } = new();
}
