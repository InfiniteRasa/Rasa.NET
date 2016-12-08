namespace Rasa.Data
{
    public enum CreateCharacterResult
    {
        Success = 0,
        InvalidEncoding = 1,
        NameTooShort = 2,
        NameTooLong = 3,
        NameFormatInvalid = 4,
        NameUnacceptable = 5,
        NameReserved = 6,
        NameInUse = 7,
        InvalidCharacterName = 8,
        TechnicalDifficulty = 9,
        CharacterSlotInUse = 10,
        InvalidCharacterToCloneFrom = 11,
        InvalidCharacterHeight = 12,
        NotEnoughCloneCredits = 13,
        FamilyNameReserved = 14,
        CharacterCreationInvalidRace = 15
    }
}
