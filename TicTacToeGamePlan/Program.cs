using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGamePlan
{
    public enum CellState : byte
    {
        Empty = 0,
        Circle = 1,
        Cross = 2
    }

    public class Position
    {
        private readonly CellState[] _cells;
        public const int TotalCells = 9;

        public Position()
            :
            this(new CellState[TotalCells])
        {
        }

        public Position(CellState[] cells)
        {
            if (cells.Length != TotalCells)
            {
                throw new InvalidOperationException($"Cannot initialize position with {cells.Length} cells");
            }
            _cells = cells;
        }

        public CellState this[int idx]
        {
            get { return _cells[idx]; }
        }

        public bool HasEmptyCells()
        {
            return _cells.Any(c => c == CellState.Empty);
        }

        public Position Move(int cell, CellState content)
        {
            if (_cells[cell] != CellState.Empty)
            {
                throw new InvalidOperationException($"Cannot put {content} in cell {cell}, it already contains {_cells[cell]}");
            }

            var newCells = new CellState[TotalCells];
            Array.Copy(_cells, newCells, TotalCells);
            newCells[cell] = content;
            return new Position(newCells);
        }
    }

    public static class PositionConverter
    {
        private static readonly int[] _powers = { 1, 3, 9, 27, 81, 243, 729, 2187, 6561 }; // powers of 3

        public static int ToInt(Position position)
        {
            int n = 0;
            for (int i = 0; i < Position.TotalCells; ++i)
            {
                n += (int)position[i] * _powers[i];
            }
            return n;
        }
    }

    public static class PositionScore
    {
        private static readonly int[][] _winningStreaks = 
        {
            new[] {0,1,2},
            new[] {3,4,5},
            new[] {6,7,8},
            new[] {0,3,6},
            new[] {1,4,7},
            new[] {2,5,8},
            new[] {0,4,8},
            new[] {2,4,6}
        };

        public static bool IsWinner(Position position, CellState side)
        {
            return _winningStreaks.Any(streak => streak.All(n => position[n] == side));
        }

        public static double Of(Position p)
        {
            if (IsWinner(p, CellState.Cross)) return 1.0;
            if (IsWinner(p, CellState.Circle)) return -1.0;
            return 0.0;
        }
    }

    public class PositionTree
    {
        public PositionTree(Position position, CellState whoMoves, Dictionary<int, PositionTree> children)
        {
            Position = position;
            WhoMoves = whoMoves;
            Children = children;

            if (Children.Any())
            {
                Score = Children.Values.Average(c => c.Score);
                if (whoMoves == CellState.Circle)
                {
                    // this position resulted from the cross's move; 
                    // recommended move for circles is the one with the minimum score
                    RecommendedMove = Children.Aggregate((curMin, x) => x.Value.Score < curMin.Value.Score ? x : curMin).Key;
                }
            }
            else
            {
                Score = PositionScore.Of(position);
            }
        }

        public Position Position { get; }
        public CellState WhoMoves { get; }
        public double Score { get; private set; }
        public Dictionary<int, PositionTree> Children { get; }
        public int RecommendedMove { get; private set; }
    }

    public class PositionTreeBuilder
    {
        private readonly Dictionary<int, PositionTree> _cache = new Dictionary<int, PositionTree>();

        public Dictionary<int, int> GetRecommendedMoves()
        {
            BuildTree(new Position(), CellState.Cross);
            return _cache
                .Where(pair=>pair.Value.WhoMoves == CellState.Circle)
                .ToDictionary(pair => pair.Key, pair => pair.Value.RecommendedMove);
        }

        private PositionTree BuildTree(Position position, CellState whoMoves)
        {
            int n = PositionConverter.ToInt(position);
            if (_cache.TryGetValue(n, out var tree))
            {
                return tree;
            }

            var newTree = BuildTreeNoCache(position, whoMoves);
            _cache[n] = newTree;
            return newTree;
        }

        private PositionTree BuildTreeNoCache(Position position, CellState whoMoves)
        {
            if (!position.HasEmptyCells() || PositionScore.Of(position) != 0)
            {
                // final position, no children possible
                return new PositionTree(position, whoMoves, new Dictionary<int, PositionTree>());
            }

            var otherSide = whoMoves == CellState.Cross ? CellState.Circle : CellState.Cross;
            var children = new Dictionary<int, PositionTree>();

            for (int cell = 0; cell < Position.TotalCells; ++cell)
            {
                if (position[cell] != CellState.Empty) continue;
                children[cell] = BuildTree(position.Move(cell, whoMoves), otherSide); // recursive call
            }

            return new PositionTree(position, whoMoves, children);
        }
    }

    class Program
    {
        static void Main()
        {
            var moves = new PositionTreeBuilder().GetRecommendedMoves();
            
            Console.Write("function getMoves() { return {");
            foreach (var key in moves.Keys.OrderBy(n => n))
            {
                Console.Write($"{key}:{moves[key]},");
            }
            Console.Write("};}");
        }
    }
}
