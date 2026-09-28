using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// The weapons with a scope, and the camera profile each gets: WeaponInfo's last field,
    /// cameraProfile. When the weapon is readied the client passes the profile's name to the
    /// engine (manifestation.py SetWeaponCameraZoomProfile), and Toggle Zoom (ZoomCamera, the
    /// middle mouse button by default) then zooms through it. With 0 there is no profile, and
    /// the key does nothing.
    ///
    /// The profiles are generated.client.constant.cameraprofile, defined in
    /// cameraprofiles_internal.xml (data/misc02.glm):
    ///
    ///  - 1, TorqueShellRifleScope: first person at 3x behind camera_scope_sniper_v01.geo, a
    ///    quarter of the mouse sensitivity, facing locked to the view;
    ///  - 2, RocketLauncherSeries3Scope: the same through camera_rocketlauncher_series3_scope_v01.geo.
    ///
    /// Which weapon had which was per-weapon server data that is gone. These are the weapon
    /// classes (augmentations 2,4,6) whose client name is a Torqueshell Rifle - the Sniper's
    /// rifle, every damage type and the named Sunset ones - or a Series 3 Rocket Launcher,
    /// including the 32 whose class name is ZZZ_Delete_Me* but whose item templates still exist.
    /// Generated from client 1.16.5.0 (physicalentityclassnamelanguage).
    /// </summary>
    public static class WeaponScopes
    {
        public const int None = 0;
        public const int TorqueShellRifleScope = 1;
        public const int RocketLauncherSeries3Scope = 2;

        /// <summary>Torqueshell Rifles (123).</summary>
        public static readonly IReadOnlySet<uint> TorqueshellRifles = new HashSet<uint>
        {
            8932, 8934, 8937, 8942, 8946, 8948, 8951, 8956, 8960, 8962, 8965, 8970,
            8974, 8976, 8979, 8984, 8988, 8990, 8993, 8998, 12628, 12636, 12644, 12652,
            12660, 25104, 25105, 25106, 25107, 25108, 27369, 27370, 27371, 27372, 27373, 27374,
            27375, 27376, 27377, 27378, 27379, 27380, 27381, 27382, 27383, 27384, 27385, 27386,
            27387, 27388, 27389, 27390, 27391, 27392, 27393, 27394, 27395, 27396, 27397, 27398,
            27872, 27873, 27874, 27875, 27876, 27877, 27878, 27879, 27880, 27881, 27882, 27883,
            27884, 27885, 27886, 27887, 27888, 27889, 27890, 27891, 27892, 27893, 27894, 27895,
            27896, 27897, 27898, 27899, 27900, 27901, 28375, 28376, 28377, 28378, 28379, 28380,
            28381, 28382, 28383, 28384, 28385, 28386, 28387, 28388, 28389, 28390, 28391, 28392,
            28393, 28394, 28395, 28396, 28397, 28398, 28399, 28400, 28401, 28402, 28403, 28404,
            30687, 30707, 30715,
        };

        /// <summary>Series 3 Rocket Launchers (47).</summary>
        public static readonly IReadOnlySet<uint> Series3RocketLaunchers = new HashSet<uint>
        {
            29759, 29845, 29846, 29847, 29848, 29849, 29850, 29851, 29852, 29853, 29854, 29855,
            29856, 29857, 29858, 29859, 29860, 29861, 29862, 29863, 29864, 29865, 29866, 29867,
            29868, 29869, 29870, 29871, 29872, 29873, 29874, 29875, 29876, 30233, 30234, 30235,
            30236, 30237, 30238, 30239, 30240, 30241, 30242, 30243, 30244, 30682, 30703,
        };

        /// <summary>The camera profile for a weapon class; <see cref="None"/> for one without a scope.</summary>
        public static int ProfileOf(uint weaponClassId)
        {
            if (TorqueshellRifles.Contains(weaponClassId))
                return TorqueShellRifleScope;

            if (Series3RocketLaunchers.Contains(weaponClassId))
                return RocketLauncherSeries3Scope;

            return None;
        }
    }
}
