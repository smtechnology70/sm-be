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
        public int CurrentPlayer { get; private set; } = 1; // Player1 (first to enter room) always starts
        public int? Winner { get; private set; }
        public GameStatus Status { get; private set; } = GameStatus.Playing;
        public int? Player1Id { get; private set; } // First player to enter room
        public int? Player2Id { get; private set; } // Second player to enter room

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
                // CurrentPlayer starts at 1, meaning Player1 (first to enter) gets first move
            };
        }

        public void SetPlayers(int player1Id, int player2Id)
        {
            Player1Id = player1Id;  // First player to enter room
            Player2Id = player2Id;  // Second player to enter room
            // CurrentPlayer remains 1, so Player1 gets the first move
        }

        public void ApplyMove(int idx)
        {
            if (idx < 0 || idx >= Boxes.Length)
                throw new ArgumentOutOfRangeException(nameof(idx));

            Boxes[idx] = Boxes[idx] with { Revealed = true };

            if (Boxes[idx].Value == 0)
            {
                // Current player loses, other player wins
                Winner = CurrentPlayer == 1 ? 2 : 1;
                Status = GameStatus.Finished;
            }
            else
            {
                // Switch turns: 1 -> 2, 2 -> 1
                CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
            }
        }
    }
}
