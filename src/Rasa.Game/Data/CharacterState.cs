using System;

namespace Rasa.Data
{
    public enum CharacterStateType
    {
        Posture                 = 1,
        Movement                = 2,
        Action                  = 3,
        Combat                  = 4,
        Control                 = 5,
        Tool                    = 6,
        Direction               = 7,
        WeaponAnimConditionCode = 8,
        ActionId                = 9,
        ActionArgId             = 10
    }

    public enum CharacterState
    {
        Standing      =  1, // Posture
        Sitting       =  2, // Posture
        LyingDown     =  3, // Posture
        Swimming      =  4, // Posture
        Dead          =  5, // Control
        Stopped       =  6, // Movement
        Slow          =  7, // Movement
        Fast          =  8, // Movement
        Flying        =  9, // Posture
        Flailing      = 10, // Posture
        Normal        = 11, // Control
        Uncontrolled  = 12, // Control
        Stunned       = 13, // Control
        Crouched      = 14, // Posture
        AtPeace       = 15, // Combat
        CombatEngaged = 17, // Combat
        Idle          = 18, // Action
        Recovery      = 19, // Action
        Windup        = 20, // Action
        NoTool        = 21, // Tool
        ToolReady     = 22, // Tool
        Special       = 25, // Movement
        Dying         = 26, // Control
    }

    public static class CharacterStateHelper
    {
        public static CharacterStateType GetType(CharacterState state)
        {
            return state switch
            {
                CharacterState.Standing or CharacterState.Sitting       or CharacterState.LyingDown    or CharacterState.Swimming or
                CharacterState.Flying   or CharacterState.Flailing      or CharacterState.Crouched                                      => CharacterStateType.Posture,

                CharacterState.Dead     or CharacterState.Normal        or CharacterState.Uncontrolled or CharacterState.Stunned or
                CharacterState.Dying                                                                                                    => CharacterStateType.Control,

                CharacterState.Stopped  or CharacterState.Slow          or CharacterState.Fast         or CharacterState.Special        => CharacterStateType.Movement,

                CharacterState.AtPeace  or CharacterState.CombatEngaged                                                                 => CharacterStateType.Combat,

                CharacterState.Idle     or CharacterState.Recovery      or CharacterState.Windup                                        => CharacterStateType.Action,

                CharacterState.NoTool   or CharacterState.ToolReady                                                                     => CharacterStateType.Tool,
                _ => throw new Exception($"Invalid CharacterState: ${state}"),
            };
        }
    }
}
