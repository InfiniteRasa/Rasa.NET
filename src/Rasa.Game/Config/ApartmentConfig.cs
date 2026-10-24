namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's Apartments: players' apartments (Managers.Apartments) - whether there
    /// are any, how much one may hold, how many may stand open at once and how long an empty one
    /// stands. Every value has a default, so a file without the section has apartments on.
    /// </summary>
    public class ApartmentConfig
    {
        /// <summary>Off, .apartment and SendMeToMyApartment refuse, and nobody is let into one; a player inside one is sent out on their next login.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The folder of the plot and decoration tables (plots.tsv, plot-caps.tsv, decor.tsv),
        /// from the server's folder when it is not a full path.
        /// </summary>
        public string ContentDirectory { get; set; } = "apartments";

        /// <summary>
        /// The most items an apartment may have put down, whatever its plot allows. Every item
        /// is an entity the retail client is sent and keeps while it is in the apartment, so the
        /// plots' own budgets (150 to 800) are held to this; a plot's per-category caps are scaled
        /// down with it (Managers.ApartmentCatalogue.CapOf).
        /// </summary>
        public int ItemBudget { get; set; } = 150;

        /// <summary>Edits (place, move, turn, delete...) a character may make: this many in a burst ...</summary>
        public int EditBurst { get; set; } = 20;

        /// <summary>... and this many a second after it.</summary>
        public double EditsPerSecond { get; set; } = 4;

        /// <summary>The most apartment copies standing at once; none is opened past it (PmFailedToCreateApartment).</summary>
        public int MaxOpenCopies { get; set; } = 64;

        /// <summary>How long an apartment copy with nobody in it or on the way stands before it is closed.</summary>
        public int IdleCloseSeconds { get; set; } = 300;

        /// <summary>Wrong passcodes an account may give before it is locked out of passcodes ...</summary>
        public int PasscodeTries { get; set; } = 5;

        /// <summary>... for this long, twice as long for each lockout in a row, an hour at most.</summary>
        public int PasscodeLockoutSeconds { get; set; } = 30;

        /// <summary>How far in front of the player .decor place puts an item when no distance is given, in metres.</summary>
        public double PlaceDistance { get; set; } = 2;

        /// <summary>The furthest .decor place, here or near reach, in metres.</summary>
        public double Reach { get; set; } = 30;
    }
}
