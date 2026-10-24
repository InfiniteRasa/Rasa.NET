namespace Rasa.Repositories.Char.Character
{
    /// <summary>
    /// One character's experience, level and clone credits, to be written over what the server
    /// expects its row to hold (ICharacterRepository.UpdateCharactersProgression).
    /// </summary>
    public sealed class CharacterProgressionChange
    {
        public uint Id { get; init; }

        /// <summary>What the row must still hold for the change to be written: the server's figures before it.</summary>
        public uint ExpectedExperience { get; init; }
        public byte ExpectedLevel { get; init; }
        public uint ExpectedCloneCredits { get; init; }

        public uint Experience { get; init; }
        public byte Level { get; init; }
        public uint CloneCredits { get; init; }
    }
}
