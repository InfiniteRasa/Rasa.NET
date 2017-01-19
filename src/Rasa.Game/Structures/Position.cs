using System;
namespace Rasa.Structures
{
    public class Position
    {
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public static double Distance (Position player, Position other)
        {
            return Math.Sqrt(Math.Pow(other.PosX - player.PosX, 2) + Math.Pow(other.PosY - player.PosY, 2) + Math.Pow(other.PosZ - other.PosZ, 2));
        }
    }
}
