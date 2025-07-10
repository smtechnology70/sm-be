using System;
using System.Collections.Generic;
using System.Linq;

namespace SM_BE.Hubs
{
    public enum GameStatus { Playing, Finished }
    public record Box(int Value, bool Revealed);

    public class GameState
    {
        public Box[] Boxes { get; init; } = Array.Empty<Box>();
        public int CurrentPlayer { get; private set; } = 1;
        public int? Winner { get; private set; }
        public GameStatus Status { get; private set; } = GameStatus.Playing;

        public static GameState CreateNew()
        {
            var rnd = new Random();
            var total = 49;
            var zeros = total * 30 / 100;        // 15 zeros
            var nums = Enumerable.Range(1, 999)
                                   .OrderBy(_ => rnd.Next())
                                   .Take(total - zeros)
                                   .ToList();

            var vals = Enumerable.Repeat(0, zeros).Concat(nums)
                                 .OrderBy(_ => rnd.Next());

            return new GameState
            {
                Boxes = vals.Select(v => new Box(v, false)).ToArray(),
            };
        }

        public void ApplyMove(int idx)
        {
            if (idx < 0 || idx >= Boxes.Length)
                throw new ArgumentOutOfRangeException(nameof(idx));

            Boxes[idx] = Boxes[idx] with { Revealed = true };

            if (Boxes[idx].Value == 0)
            {
                Winner = CurrentPlayer == 1 ? 2 : 1;
                Status = GameStatus.Finished;
            }
            else
            {
                CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
            }
        }
    }
}
