namespace Rasa.Game.Handlers
{
    using Data;
    using Managers;
    using Packets;
    using Packets.Game.Client;
    using Packets.Game.Server;

    public partial class ClientPacketHandler
    {
        /// <summary>
        /// The player is spending attribute points on body, mind, and spirt.
        /// </summary>        
        [PacketHandler(GameOpcode.AllocateAttributePoints)]
        private void AllocateAttributePoints(AllocateAttributePointsPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(AllocateAttributePoints)}");
        }

        [PacketHandler(GameOpcode.AutoFireKeepAlive)]
        private void AutoFireKeepAlive(AutoFireKeepAlivePacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(AutoFireKeepAlive)}");
        }

        [PacketHandler(GameOpcode.ChangeTitle)]
        private void ChangeTitle(ChangeTitlePacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(ChangeTitle)}");
        }

        [PacketHandler(GameOpcode.ChangeShowHelmet)]
        private void ChangeShowHelmet(ChangeShowHelmetPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(ChangeShowHelmet)}");
        }

        [PacketHandler(GameOpcode.ClearTargetId)]
        private void ClearTargetId(ClearTargetIdPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(ClearTargetId)}");
        }

        [PacketHandler(GameOpcode.ClearTrackingTarget)]
        private void ClearTrackingTarget(ClearTrackingTargetPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(ClearTrackingTarget)}");
        }

        [PacketHandler(GameOpcode.GetCustomizationChoices)]
        private void GetCustomizationChoices(GetCustomizationChoicesPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(GetCustomizationChoices)}");
        }

        [PacketHandler(GameOpcode.LevelSkills)]
        private void LevelSkills(LevelSkillsPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(LevelSkills)}");
        }

        [PacketHandler(GameOpcode.RequestArmAbility)]
        private void RequestArmAbility(RequestArmAbilityPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestArmAbility)}");
        }

        [PacketHandler(GameOpcode.RequestArmWeapon)]
        private void RequestArmWeapon(RequestArmWeaponPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestArmWeapon)}");
        }

        [PacketHandler(GameOpcode.RequestCustomization)]
        private void RequestCustomization(RequestCustomizationPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestCustomization)}");
        }

        [PacketHandler(GameOpcode.RequestPerformAbility)]
        private void RequestPerformAbility(RequestPerformAbilityPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestPerformAbility)}");
        }

        [PacketHandler(GameOpcode.RequestSetAbilitySlot)]
        private void RequestSetAbilitySlot(RequestSetAbilitySlotPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestSetAbilitySlot)}");
        }

        [PacketHandler(GameOpcode.RequestSwapAbilitySlots)]
        private void RequestSwapAbilitySlots(RequestSwapAbilitySlotsPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestSwapAbilitySlots)}");
        }

        [PacketHandler(GameOpcode.RequestToggleRun)]
        private void RequestToggleRun(RequestToggleRunPacket packet)
        {
            _manifestationManager.RequestToggleRun(Client);
        }

        [PacketHandler(GameOpcode.RequestWeaponDraw)]
        private void RequestWeaponDraw(RequestWeaponDrawPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestWeaponDraw)}");
        }

        [PacketHandler(GameOpcode.RequestWeaponReload)]
        private void RequestWeaponReload(RequestWeaponReloadPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestWeaponReload)}");
        }

        [PacketHandler(GameOpcode.RequestWeaponStow)]
        private void RequestWeaponStow(RequestWeaponStowPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(RequestWeaponStow)}");
        }

        [PacketHandler(GameOpcode.SaveCharacterOptions)]
        private void SaveCharacterOptions(SaveCharacterOptionsPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(SaveCharacterOptions)}");
        }

        [PacketHandler(GameOpcode.SaveUserOptions)]
        private void SaveUserOptions(SaveUserOptionsPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(SaveUserOptions)}");
        }

        [PacketHandler(GameOpcode.SetTargetId)]
        private void SetTargetId(SetTargetIdPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(SetTargetId)}");
        }

        [PacketHandler(GameOpcode.SetTrackingTarget)]
        private void SetTrackingTarget(SetTrackingTargetPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(SetTrackingTarget)}");
        }

        [PacketHandler(GameOpcode.StartAutoFire)]
        private void StartAutoFire(StartAutoFirePacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(StartAutoFire)}");
        }

        [PacketHandler(GameOpcode.StopAutoFire)]
        private void StopAutoFire(StopAutoFirePacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo {nameof(StopAutoFire)}");
        }
    }
}
