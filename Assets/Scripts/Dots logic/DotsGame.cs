using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dots
{
    public class DotsGame
    {
        private DotColor[,] _dots;

        public int Width { get; private set; }
        public int Height { get; private set; }

        private int _luckModifier = 15;
        private int _luck = -1;

        public void Generate(int height, int width)
        {
            Width = width;
            Height = height;

            _dots = new DotColor[height, width];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    AddDot(i, j);
                }
            }
        }

        public event Action<Vector2Int> OnDotSpawn;
        /// <summary>
        /// Two elements in array, from position and to position
        /// </summary>
        public event Action<Vector2Int[]> OnDotMove;
        public event Action<Vector2Int> OnDotRemove;

        public DotColor this[int x, int y] => _dots[x, y];

        private DotColor[] GetUniqueNeighbourColors(int x, int y)
        {
            HashSet<DotColor> dotColors = new HashSet<DotColor>();

            if (x > 0 && _dots[x - 1, y] != DotColor.Undefined) dotColors.Add(_dots[x - 1, y]);
            if (y > 0 && _dots[x, y - 1] != DotColor.Undefined) dotColors.Add(_dots[x, y - 1]);
            if (x < Width - 1 && _dots[x + 1, y] != DotColor.Undefined) dotColors.Add(_dots[x + 1, y]);
            if (y < Height - 1 && _dots[x, y + 1] != DotColor.Undefined) dotColors.Add(_dots[x, y + 1]);

            return dotColors.ToArray();
        }

        private DotColor GenerateDot(params DotColor[] luckyColors)
        {
            int redRoll = 100, blueRoll = 100, greenRoll = 100, purpleRoll = 100, yellowRoll = 100;
            if (!luckyColors.Contains(DotColor.Red)) redRoll -= _luck * _luckModifier;
            if (!luckyColors.Contains(DotColor.Blue)) blueRoll -= _luck * _luckModifier;
            if (!luckyColors.Contains(DotColor.Green)) greenRoll -= _luck * _luckModifier;
            if (!luckyColors.Contains(DotColor.Purple)) purpleRoll -= _luck * _luckModifier;
            if (!luckyColors.Contains(DotColor.Yellow)) yellowRoll -= _luck * _luckModifier;

            int luckBonus = Mathf.RoundToInt(_luck * (5 - luckyColors.Length) * _luckModifier / (float) luckyColors.Length);
            if (luckyColors.Contains(DotColor.Red)) redRoll += luckBonus;
            if (luckyColors.Contains(DotColor.Blue)) blueRoll += luckBonus;
            if (luckyColors.Contains(DotColor.Green)) greenRoll += luckBonus;
            if (luckyColors.Contains(DotColor.Purple)) purpleRoll += luckBonus;
            if (luckyColors.Contains(DotColor.Yellow)) yellowRoll += luckBonus;

            DotColor dotColor = RollColor(redRoll, blueRoll, greenRoll, purpleRoll, yellowRoll);
            if (luckyColors.Contains(dotColor))
            {
                _luck -= 2;
                if (_luck < -3) _luck = -3;
            }
            else
            {
                _luck++;
            }

            return dotColor;
        }

        public int ConnectPoints(Vector2Int[] path)
        {
            int score = 0;

            for (int i = 1; i < path.Length; i++)
            {
                if (!CanConnect(path[i - 1], path[i]))
                {
                    Debug.LogError("Points can't be connected.");

                    return 0;
                }

                score += i;

                for (int j = i - 1; j >= 0; j--)
                {
                    if (path[j] == path[i]) score *= 2;
                }
            }

            foreach (var point in path)
            {
                RemoveDot(point.x, point.y);
            }

            return score;
        }

        private void AddDot(int x, int y)
        {
            _dots[x, y] = GenerateDot(GetUniqueNeighbourColors(x, y));
            OnDotSpawn?.Invoke(new Vector2Int(x, y));
        }

        private void RemoveDot(int x, int y)
        {
            _dots[x, y] = DotColor.Undefined;

            OnDotRemove?.Invoke(new Vector2Int(x, y));

            if (y > 0) MoveDot(x, y - 1, x, y);
            else AddDot(x, y);
        }

        private void MoveDot(int fromX, int fromY, int toX, int toY)
        {
            _dots[toX, toY] = _dots[fromX, fromY];
            _dots[fromX, fromY] = DotColor.Undefined;

            OnDotMove?.Invoke(new Vector2Int[] { new Vector2Int(fromX, fromY), new Vector2Int(toX, toY) });

            if (fromY > 0) MoveDot(fromX, fromY - 1, fromX, fromY);
            else AddDot(fromX, fromY);
        }

        private DotColor RollColor(int redRoll, int blueRoll, int greenRoll, int purpleRoll, int yellowRoll)
        {
            int roll = Random.Range(0, redRoll + blueRoll + greenRoll + purpleRoll + yellowRoll);

            blueRoll += redRoll;
            greenRoll += blueRoll;
            purpleRoll += greenRoll;

            if (redRoll > roll) return DotColor.Red;
            if (blueRoll > roll) return DotColor.Blue;
            if (greenRoll > roll) return DotColor.Green;
            if (purpleRoll > roll) return DotColor.Purple;
            return DotColor.Yellow;
        }

        public bool CanConnect(int x1, int y1, int x2, int y2)
        {
            if (x1 == x2 && y1 == y2) return false;
            if (Mathf.RoundToInt(Mathf.Abs(x1 - x2)) == 1 && Mathf.RoundToInt(Mathf.Abs(y1 - y2)) == 1) return false;
            if (Mathf.RoundToInt(Mathf.Abs(x1 - x2)) > 1 || Mathf.RoundToInt(Mathf.Abs(y1 - y2)) > 1) return false;

            return this[x1, y1] == this[x2, y2];
        }
        public bool CanConnect(Vector2Int pos1, Vector2Int pos2)
        {
            return CanConnect(pos1.x, pos1.y, pos2.x, pos2.y);
        }
    }
}