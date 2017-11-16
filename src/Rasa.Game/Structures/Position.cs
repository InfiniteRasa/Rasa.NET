using System;
namespace Rasa.Structures
{
    public class Position
    {
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }

        public Position(double posX, double posY, double posZ)
        {
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
        }

        public static double Distance (Position player, Position other)
        {
            return Math.Sqrt(Math.Pow(other.PosX - player.PosX, 2) + Math.Pow(other.PosY - player.PosY, 2) + Math.Pow(other.PosZ - other.PosZ, 2));
        }
    }

    public class Quaternion
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }
    }
}
