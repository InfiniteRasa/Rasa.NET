namespace Rasa.Structures
{
    public class ActorStats
    {
        public Body Body { get; set; } = new Body();
        public Mind Mind { get; set; } = new Mind();
        public Spirit Spirit { get; set; } = new Spirit();
        public Health Health { get; set; } = new Health();
        public Chi Chi { get; set; } = new Chi ();
        public Power Power { get; set; } = new Power();
        public Aware Aware { get; set; } = new Aware();
        public Armor Armor { get; set; } = new Armor();
        public Speed Speed { get; set; } = new Speed();
        public Regen Regen { get; set; } = new Regen();
    }
    // BODY 1
    public class Body
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // MIND 2
    public class Mind
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // SPIRIT 3
    public class Spirit
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // HEALTH 4
    public class Health
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // CHI 5
    public class Chi
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // POWER 6
    public class Power
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // AWERE 7
    public class Aware
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // ARMOR 8
    public class Armor
    {
        public double NormalMax { get; set; }
        public double CurrentMax { get; set; }
        public double Current { get; set; }
        public double RefreshAmount { get; set; }
        public double RefreshPeriod { get; set; }
    }
    // SPEED 9
    public class Speed
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
    // REGEN 10
    public class Regen
    {
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }
    }
}
