using System;
using System.Collections.Generic;
using System.Linq;

namespace SM_BE.Hubs
{
    public enum SingleGameStatus { Playing, Won, Lost }
    public record SingleBox(int Value, bool Revealed);

    public class SingleGameState
    {
        public SingleBox[] Boxes { get; init; } = Array.Empty<SingleBox>();
        public int PlayerId { get; private set; }
        public SingleGameStatus Status { get; private set; } = SingleGameStatus.Playing;
        public int RevealedBoxesCount { get; private set; } = 0;
        public int CurrentSum { get; private set; } = 0;
        public int MaxBoxes { get; } = 30;

        public static SingleGameState CreateNew(int playerId)
        {
            var rnd = new Random();
            var totalBoxes = 30; // Total boxes available
            var values = new List<int>();

            // 60-70% of boxes should have negative numbers
            var negativePercentage = rnd.Next(45, 65); // 60% to 70%
            var negativeCount = (totalBoxes * negativePercentage) / 100;
            var positiveCount = totalBoxes - negativeCount;
            
            Console.WriteLine($"Generating game with {negativeCount} negative boxes ({negativePercentage}%) and {positiveCount} positive boxes");

            // Generate positive values (1 to 10 only)
            for (int i = 0; i < positiveCount; i++)
            {
                var value = rnd.Next(1, 10); // 1-10 range for positive values
                values.Add(value);
            }

            // Generate negative values (-1 to -15 only)
            for (int i = 0; i < negativeCount; i++)
            {
                var value = rnd.Next(-10, 0); // -15 to -1 range for negative values
                values.Add(value);
            }

            // Shuffle the values
            values = values.OrderBy(_ => rnd.Next()).ToList();
            
            // Calculate final sum
            var finalSum = values.Sum();
            Console.WriteLine($"Generated game with final sum: {finalSum}");
            Console.WriteLine($"Positive values: {values.Count(v => v > 0)} boxes, Negative values: {values.Count(v => v < 0)} boxes");
            Console.WriteLine($"Value ranges: Positive (1-10), Negative (-15 to -1)");

            return new SingleGameState
            {
                Boxes = values.Select(v => new SingleBox(v, false)).ToArray(),
                PlayerId = playerId
            };
        }

        public void ApplyMove(int idx)
        {
            if (idx < 0 || idx >= Boxes.Length)
                throw new ArgumentOutOfRangeException(nameof(idx));

            if (Boxes[idx].Revealed)
                throw new InvalidOperationException("Box already revealed");

            if (Status != SingleGameStatus.Playing)
                throw new InvalidOperationException("Game is not in playing state");

            // Reveal the box
            Boxes[idx] = Boxes[idx] with { Revealed = true };
            RevealedBoxesCount++;
            CurrentSum += Boxes[idx].Value;

            // Check win/lose conditions
            if (RevealedBoxesCount >= MaxBoxes)
            {
                // Player has opened maximum boxes, check if sum is positive
                Status = CurrentSum > 0 ? SingleGameStatus.Won : SingleGameStatus.Lost;
            }
            // Game continues if less than max boxes revealed
        }

        public void StopGameEarly()
        {
            if (Status != SingleGameStatus.Playing)
                throw new InvalidOperationException("Game is not in playing state");

            Status = CurrentSum > 0 ? SingleGameStatus.Won : SingleGameStatus.Lost;
        }

        public bool CanRevealMoreBoxes => RevealedBoxesCount < MaxBoxes && Status == SingleGameStatus.Playing;
        
        public int RemainingBoxes => MaxBoxes - RevealedBoxesCount;
    }
}