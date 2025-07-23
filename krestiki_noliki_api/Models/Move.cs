using System;
using System.Collections.Generic;

namespace krestiki_noliki_api.Models;

public partial class Move
{
    public int Id { get; set; }

    public Guid GameId { get; set; }

    public int Player { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public bool IsReversed { get; set; }

    public string RequestHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Game Game { get; set; } = null!;
}
