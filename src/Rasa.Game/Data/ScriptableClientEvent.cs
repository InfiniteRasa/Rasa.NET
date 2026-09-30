namespace Rasa.Data
{
    /// <summary>
    /// generated.client.scriptableclientevent: the player actions the client can report with
    /// ScriptableClientEvent once the server has asked it to with StartTrackingScriptableClientEvent
    /// (ScriptableClientEvents). The comments say where the 1.16.5 client fires each one.
    /// </summary>
    public enum ScriptableClientEvent : uint
    {
        /// <summary>inputhandlers.py: the move forward key.</summary>
        MoveForward = 1,
        /// <summary>inputhandlers.py: the move backward key.</summary>
        MoveBackward = 2,
        /// <summary>inputhandlers.py: the strafe left key.</summary>
        MoveLeft = 3,
        /// <summary>inputhandlers.py: the strafe right key.</summary>
        MoveRight = 4,
        /// <summary>weapondrawerwindow.py: any weapon drawer update; also on tracking start while a weapon slot is filled.</summary>
        EquippedWeapon = 5,
        /// <summary>manifestation.py SetAbilitySlot: an ability put in a tray slot; also on tracking start while a tray slot is filled.</summary>
        ArmedAbility = 6,
        /// <summary>radialwindow.py: the radial menu opened.</summary>
        OpenedRadialMenu = 7,
        /// <summary>manifestation.py ToggleCrouched: crouched (not on standing up).</summary>
        Crouched = 8,
        /// <summary>targeting.py: target lock switched on.</summary>
        UsedStickyTargetting = 9,
        /// <summary>actor.py: a weapon attack sent.</summary>
        FireWeapon = 10,
        /// <summary>actor.py: an alternate fire sent.</summary>
        FireWeaponAlt = 11,
        /// <summary>actor.py: a reload sent.</summary>
        FireReload = 12,
        /// <summary>actor.py: an ability sent.</summary>
        FireAbility = 13,
        /// <summary>actor.py: a use action on a usable sent.</summary>
        UseObject = 14,
        /// <summary>currentmissionswindow.py: the mission log shown; also on tracking start while it is open.</summary>
        OpenMissionLog = 15,
        /// <summary>inventory.py: anything added to the equipped (worn) inventory.</summary>
        EquippedWearable = 16
    }
}
