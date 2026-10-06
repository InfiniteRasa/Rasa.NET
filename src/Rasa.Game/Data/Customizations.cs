using System;
using System.Collections.Generic;

namespace Rasa.Data
{
    using Structures;

    /// <summary>
    /// The customization items - the classes with the Customization augmentation (19) - as the
    /// client knows them, from generated.client.customizationdata of client 1.16.5.0:
    ///
    ///  - customizationClass: what each one changes (<see cref="CustomizationType"/>);
    ///  - customizationClassHueChoice: the 25 colours an armour paint offers, the swatches of
    ///    colorcustomizationwindow.py, in swatch order;
    ///  - customizationRestriction: the item classes it may be used on, which the client checks
    ///    in CustomizeAction.CheckAction (Customization.IsIncludedClass). A class with no list
    ///    may be used on anything;
    ///  - customizationChoice: the hair and the faces the two modification items offer;
    ///  - customizationClassPalette: the texture a hair or skin colour is picked from, whose
    ///    colours are here (<see cref="Palettes"/>).
    ///
    /// Every armour paint with a list shares the same 2550 armour classes; some add a few more
    /// (the Sunset officer uniform, the AFS police armour, the detail faces), and the Boxing
    /// Gear kit has three of its own.
    /// </summary>
    public static class Customizations
    {
        /// <summary>colorcustomizationwindow.py kColorSwatchCount.</summary>
        public const int HuesPerItem = 25;

        /// <summary>
        /// Ours: how far a channel of the colour asked for may be from the swatch it is taken
        /// for. The client sends the swatch widget's colour as it reads it back.
        /// </summary>
        public const int HueTolerance = 2;

        /// <summary>
        /// Ours: the same for a colour picked off a palette texture. The client sends the pixel
        /// it clicked as it decodes the texture, and a DXT1 texel is 5, 6 and 5 bits a channel:
        /// one decoder's 8 bits and another's differ by up to 7.
        /// </summary>
        public const int PaletteTolerance = 8;

        public static readonly IReadOnlyDictionary<uint, CustomizationType> Types = new Dictionary<uint, CustomizationType>
        {
            [1068] = CustomizationType.Hairstyle, // Hairstyle Modification
            [1069] = CustomizationType.FaceTexture, // Face Modification
            [3600] = CustomizationType.ClothingColor, // Combat Suit Paint Tier 3 Soldier
            [3734] = CustomizationType.SkinColor, // Melanotan for Caucasians
            [3735] = CustomizationType.HairColor, // Hair Color Modification 3
            [3741] = CustomizationType.ClothingColor, // Combat Suit Paint Tier 3 Specialist
            [3742] = CustomizationType.ClothingColor, // Body Armor Paint
            [3743] = CustomizationType.ClothingColor, // Combat Suit Paint Tier 2 Soldier
            [3744] = CustomizationType.ClothingColor, // Combat Suit Paint Tier 2 Specialist
            [3745] = CustomizationType.ClothingColor, // Combat Suit Paint Tier 4 Soldier
            [3746] = CustomizationType.ClothingColor, // Combat Suit Paint Tier 4 Specialist
            [4086] = CustomizationType.SkinColor, // Melanotan for Africans
            [4087] = CustomizationType.SkinColor, // Skin Color Modification
            [4088] = CustomizationType.HairColor, // Hair Color Modification 2
            [4089] = CustomizationType.HairColor, // Hair Color Modification 1
            [9587] = CustomizationType.ClothingColor, // Rare Body Armor Paint
            [24611] = CustomizationType.ClothingColor, // Standard Armor Paint
            [24612] = CustomizationType.ClothingColor, // Off-White Armor Paint
            [24613] = CustomizationType.ClothingColor, // Near-Black Armor Paint
            [24614] = CustomizationType.ClothingColor, // Bright White Armor Paint
            [24615] = CustomizationType.ClothingColor, // Charcoal Armor Paint
            [24616] = CustomizationType.ClothingColor, // Homebrew Neutral Fuchsia Armor Paint
            [24617] = CustomizationType.ClothingColor, // Homebrew Bright Teal Armor Paint
            [24618] = CustomizationType.ClothingColor, // Homebrew Neutral Teal Armor Paint
            [24619] = CustomizationType.ClothingColor, // Homebrew Bright Fuchsia Armor Paint
            [24620] = CustomizationType.ClothingColor, // Homebrew Dark Teal Armor Paint
            [24621] = CustomizationType.ClothingColor, // Homebrew Dark Fuchsia Armor Paint
            [24622] = CustomizationType.ClothingColor, // Homebrew Dark Yellow Armor Paint
            [24623] = CustomizationType.ClothingColor, // Homebrew Neutral Yellow Armor Paint
            [24624] = CustomizationType.ClothingColor, // Homebrew Bright Yellow Armor Paint
            [24625] = CustomizationType.ClothingColor, // Homebrew Dark Red Armor Paint
            [24626] = CustomizationType.ClothingColor, // Homebrew Neutral Red Armor Paint
            [24627] = CustomizationType.ClothingColor, // Homebrew Bright Red Armor Paint
            [24628] = CustomizationType.ClothingColor, // Homebrew Dark Green Armor Paint
            [24629] = CustomizationType.ClothingColor, // Homebrew Neutral Green Armor Paint
            [24630] = CustomizationType.ClothingColor, // Homebrew Bright Green Armor Paint
            [24631] = CustomizationType.ClothingColor, // Homebrew Dark Blue Armor Paint
            [24632] = CustomizationType.ClothingColor, // Homebrew Neutral Blue Armor Paint
            [24633] = CustomizationType.ClothingColor, // Homebrew Bright Blue Armor Paint
            [24634] = CustomizationType.ClothingColor, // Homebrew Dark Orange Armor Paint
            [24635] = CustomizationType.ClothingColor, // Homebrew Neutral Orange Armor Paint
            [24636] = CustomizationType.ClothingColor, // Homebrew Bright Orange Armor Paint
            [24637] = CustomizationType.ClothingColor, // Homebrew Dark Lime Armor Paint
            [24638] = CustomizationType.ClothingColor, // Homebrew Neutral Lime Armor Paint
            [24639] = CustomizationType.ClothingColor, // Homebrew Bright Lime Armor Paint
            [24640] = CustomizationType.ClothingColor, // Homebrew Dark Mint Armor Paint
            [24641] = CustomizationType.ClothingColor, // Homebrew Neutral Mint Armor Paint
            [24642] = CustomizationType.ClothingColor, // Homebrew Bright Mint Armor Paint
            [24643] = CustomizationType.ClothingColor, // Homebrew Dark Azure Armor Paint
            [24644] = CustomizationType.ClothingColor, // Homebrew Neutral Azure Armor Paint
            [24645] = CustomizationType.ClothingColor, // Homebrew Bright Azure Armor Paint
            [24646] = CustomizationType.ClothingColor, // Homebrew Dark Violet Armor Paint
            [24647] = CustomizationType.ClothingColor, // Homebrew Neutral Violet Armor Paint
            [24648] = CustomizationType.ClothingColor, // Homebrew Bright Violet Armor Paint
            [24649] = CustomizationType.ClothingColor, // Homebrew Dark Purple Armor Paint
            [24650] = CustomizationType.ClothingColor, // Homebrew Neutral Purple Armor Paint
            [24651] = CustomizationType.ClothingColor, // Homebrew Bright Purple Armor Paint
            [24652] = CustomizationType.ClothingColor, // Homebrew Dark Gray Armor Paint
            [24653] = CustomizationType.ClothingColor, // Homebrew Neutral Gray Armor Paint
            [24654] = CustomizationType.ClothingColor, // Homebrew Bright Gray Armor Paint
            [26380] = CustomizationType.ClothingColor, // Bane Blood Red Armor Paint
            [26381] = CustomizationType.ClothingColor, // Light Bender Yellow Armor Paint
            [26382] = CustomizationType.ClothingColor, // Shield Drone Green Armor Paint
            [26383] = CustomizationType.ClothingColor, // AFS Blue Armor Paint
            [29218] = CustomizationType.ClothingColor, // Boxing Gear Color Change Kit
        };

        /// <summary>The colours each paint offers, as Color packs them (red in the low byte, alpha 255).</summary>
        public static readonly IReadOnlyDictionary<uint, uint[]> Hues = new Dictionary<uint, uint[]>
        {
            // Body Armor Paint
            [3742] = new uint[]
            {
                0xFF393971, 0xFF395571, 0xFF397171, 0xFF397155, 0xFF397139, 0xFF557139, 0xFF717139, 0xFF715539, 0xFF713939,
                0xFF713955, 0xFF713971, 0xFF553971, 0xFF5556AA, 0xFF5580AA, 0xFF397171, 0xFF397155, 0xFF397139, 0xFF557139,
                0xFF717139, 0xFF715539, 0xFF713939, 0xFF713955, 0xFF713971, 0xFF553971, 0xFF437784,
            },
            // Rare Body Armor Paint
            [9587] = new uint[]
            {
                0xFF2A2A80, 0xFF2A5680, 0xFF2B8080, 0xFF2B8055, 0xFF2B802A, 0xFF56802A, 0xFF80802A, 0xFF80552A, 0xFF802A2A,
                0xFF802A55, 0xFF802A80, 0xFF552A80, 0xFF4041BF, 0xFF4080BF, 0xFF2A8080, 0xFF2B8055, 0xFF2B802A, 0xFF56802A,
                0xFF80802A, 0xFF80552A, 0xFF802A2A, 0xFF802A55, 0xFF802A80, 0xFF552A80, 0xFF818080,
            },
            // Standard Armor Paint
            [24611] = new uint[]
            {
                0xFF363663, 0xFF364C63, 0xFF366363, 0xFF36634C, 0xFF366336, 0xFF4C6336, 0xFF636336, 0xFF634D36, 0xFF633636,
                0xFF63364C, 0xFF633663, 0xFF4C3663, 0xFF818080, 0xFF593E74, 0xFF743E74, 0xFF743E59, 0xFF743E3E, 0xFF74593E,
                0xFF74743E, 0xFF59743E, 0xFF3E743E, 0xFF3E7459, 0xFF3E7474, 0xFF3E5974, 0xFF3E3E74,
            },
            // Off-White Armor Paint
            [24612] = new uint[]
            {
                0xFFE7E7FE, 0xFFE7F3FE, 0xFFE7FFFE, 0xFFE7FFF2, 0xFFE7FFE6, 0xFFF3FFE6, 0xFFFFFFE6, 0xFFFFF3E6, 0xFFFFE7E6,
                0xFFFFE7F2, 0xFFFFE7FE, 0xFFF3E7FE, 0xFFB5B5FD, 0xFFB5DAFD, 0xFFE7FFFE, 0xFFE7FFF2, 0xFFE7FFE6, 0xFFF3FFE6,
                0xFFFFFFE6, 0xFFFFF3E6, 0xFFFFE7E6, 0xFFFFE7F2, 0xFFFFE7FE, 0xFFF3E7FE, 0xFF818080,
            },
            // Near-Black Armor Paint
            [24613] = new uint[]
            {
                0xFF262626, 0xFF2D2D2D, 0xFF333333, 0xFF393939, 0xFF404040, 0xFF262626, 0xFF2D2D2D, 0xFF333333, 0xFF393939,
                0xFF404040, 0xFF262626, 0xFF2D2D2D, 0xFF333333, 0xFF393939, 0xFF404040, 0xFF262626, 0xFF2D2D2D, 0xFF333333,
                0xFF393939, 0xFF404040, 0xFF262626, 0xFF2D2D2D, 0xFF333333, 0xFF393939, 0xFF404040,
            },
            // Bright White Armor Paint
            [24614] = new uint[]
            {
                0xFFC0C0C0, 0xFFC2C2C2, 0xFFC4C4C4, 0xFFC6C6C6, 0xFFC8C8C8, 0xFFCACACA, 0xFFCCCCCC, 0xFFCECECE, 0xFFD0D0D0,
                0xFFD2D2D2, 0xFFD4D4D4, 0xFFD6D6D6, 0xFFD8D8D8, 0xFFDADADA, 0xFFDCDCDC, 0xFFDEDEDE, 0xFFE0E0E0, 0xFFE2E2E2,
                0xFFE4E4E4, 0xFFE6E6E6, 0xFFE8E8E8, 0xFFEAEAEA, 0xFFECECEC, 0xFFEEEEEE, 0xFFF0F0F0,
            },
            // Charcoal Armor Paint
            [24615] = new uint[]
            {
                0xFF202020, 0xFF222222, 0xFF242424, 0xFF262626, 0xFF282828, 0xFF2A2A2A, 0xFF2C2C2C, 0xFF2E2E2E, 0xFF303030,
                0xFF323232, 0xFF343434, 0xFF363636, 0xFF383838, 0xFF3A3A3A, 0xFF3C3C3C, 0xFF3E3E3E, 0xFF404040, 0xFF424242,
                0xFF444444, 0xFF464646, 0xFF484848, 0xFF4A4A4A, 0xFF4C4C4C, 0xFF4E4E4E, 0xFF505050,
            },
            // Homebrew Neutral Fuchsia Armor Paint
            [24616] = new uint[]
            {
                0xFF382640, 0xFF3F2B48, 0xFF463050, 0xFF4D3558, 0xFF543960, 0xFF362640, 0xFF3C2B48, 0xFF433050, 0xFF4A3558,
                0xFF503960, 0xFF332640, 0xFF392B48, 0xFF403050, 0xFF463558, 0xFF4C3960, 0xFF302640, 0xFF372B48, 0xFF3D3050,
                0xFF433558, 0xFF493960, 0xFF2E2640, 0xFF342B48, 0xFF393050, 0xFF3F3558, 0xFF453960,
            },
            // Homebrew Bright Teal Armor Paint
            [24617] = new uint[]
            {
                0xFF6E8217, 0xFF778D19, 0xFF80981B, 0xFF89A31D, 0xFF92AD1F, 0xFF788217, 0xFF828D19, 0xFF8C981B, 0xFF96A31D,
                0xFFA0AD1F, 0xFF838217, 0xFF8E8D19, 0xFF99981B, 0xFFA4A31D, 0xFFAEAD1F, 0xFF827717, 0xFF8E8119, 0xFF998B1B,
                0xFFA4951D, 0xFFAE9F1F, 0xFF826D17, 0xFF8D7619, 0xFF987F1B, 0xFFA4881D, 0xFFAE911F,
            },
            // Homebrew Neutral Teal Armor Paint
            [24618] = new uint[]
            {
                0xFF3B4026, 0xFF42482B, 0xFF495030, 0xFF515835, 0xFF586039, 0xFF3D4026, 0xFF45482B, 0xFF4C5030, 0xFF545835,
                0xFF5C6039, 0xFF404026, 0xFF48482B, 0xFF505030, 0xFF585835, 0xFF606039, 0xFF403D26, 0xFF48452B, 0xFF504C30,
                0xFF585435, 0xFF605C39, 0xFF403B26, 0xFF48422B, 0xFF504930, 0xFF585135, 0xFF605839,
            },
            // Homebrew Bright Fuchsia Armor Paint
            [24619] = new uint[]
            {
                0xFF621882, 0xFF6A1A8D, 0xFF721C98, 0xFF7A1EA3, 0xFF8320AD, 0xFF571882, 0xFF5E1A8D, 0xFF661C98, 0xFF6D1EA3,
                0xFF7420AD, 0xFF4C1882, 0xFF531A8D, 0xFF591C98, 0xFF601EA3, 0xFF6620AD, 0xFF421882, 0xFF471A8D, 0xFF4D1C98,
                0xFF521EA3, 0xFF5820AD, 0xFF371882, 0xFF3C1A8D, 0xFF401C98, 0xFF451EA3, 0xFF4920AD,
            },
            // Homebrew Dark Teal Armor Paint
            [24620] = new uint[]
            {
                0xFF48570F, 0xFF516211, 0xFF5B6C13, 0xFF647715, 0xFF6E8217, 0xFF50570F, 0xFF5A6211, 0xFF636C13, 0xFF6D7715,
                0xFF788217, 0xFF57570F, 0xFF626211, 0xFF6C6C13, 0xFF777715, 0xFF838217, 0xFF57500F, 0xFF625A11, 0xFF6C6313,
                0xFF776D15, 0xFF827717, 0xFF57480F, 0xFF625111, 0xFF6C5B13, 0xFF776415, 0xFF826D17,
            },
            // Homebrew Dark Fuchsia Armor Paint
            [24621] = new uint[]
            {
                0xFF410F57, 0xFF491162, 0xFF52136C, 0xFF5A1577, 0xFF621882, 0xFF3A0F57, 0xFF411162, 0xFF49136C, 0xFF501577,
                0xFF571882, 0xFF330F57, 0xFF391162, 0xFF40136C, 0xFF461577, 0xFF4C1882, 0xFF2C0F57, 0xFF311162, 0xFF37136C,
                0xFF3C1577, 0xFF421882, 0xFF250F57, 0xFF291162, 0xFF2E136C, 0xFF321577, 0xFF371882,
            },
            // Homebrew Dark Yellow Armor Paint
            [24622] = new uint[]
            {
                0xFF0F4857, 0xFF115162, 0xFF135B6C, 0xFF156477, 0xFF176E82, 0xFF0F5057, 0xFF115A62, 0xFF13636C, 0xFF156D77,
                0xFF177882, 0xFF0F5757, 0xFF116262, 0xFF136C6C, 0xFF157777, 0xFF188382, 0xFF0F5750, 0xFF11625A, 0xFF136C63,
                0xFF15776D, 0xFF188277, 0xFF0F5748, 0xFF116251, 0xFF136C5B, 0xFF157764, 0xFF18826D,
            },
            // Homebrew Neutral Yellow Armor Paint
            [24623] = new uint[]
            {
                0xFF263B40, 0xFF2B4248, 0xFF304950, 0xFF355158, 0xFF395860, 0xFF263D40, 0xFF2B4548, 0xFF304D50, 0xFF355458,
                0xFF395C60, 0xFF264040, 0xFF2B4848, 0xFF305050, 0xFF355858, 0xFF396060, 0xFF26403D, 0xFF2B4845, 0xFF30504C,
                0xFF355854, 0xFF39605C, 0xFF26403B, 0xFF2B4842, 0xFF305049, 0xFF355851, 0xFF396058,
            },
            // Homebrew Bright Yellow Armor Paint
            [24624] = new uint[]
            {
                0xFF176E82, 0xFF19778D, 0xFF1B8098, 0xFF1E89A3, 0xFF2092AD, 0xFF177882, 0xFF1A828D, 0xFF1C8C98, 0xFF1E96A3,
                0xFF20A0AD, 0xFF188382, 0xFF1A8E8D, 0xFF1C9998, 0xFF1EA4A3, 0xFF20AEAD, 0xFF188277, 0xFF1A8E81, 0xFF1C998B,
                0xFF1EA495, 0xFF20AE9F, 0xFF18826D, 0xFF1A8D76, 0xFF1C987F, 0xFF1EA488, 0xFF20AE91,
            },
            // Homebrew Dark Red Armor Paint
            [24625] = new uint[]
            {
                0xFF1E0F57, 0xFF211162, 0xFF25136C, 0xFF291577, 0xFF2C1882, 0xFF160F57, 0xFF191162, 0xFF1C136C, 0xFF1F1577,
                0xFF221882, 0xFF0F0F57, 0xFF111162, 0xFF13136C, 0xFF151577, 0xFF171882, 0xFF0F1657, 0xFF111962, 0xFF131C6C,
                0xFF151F77, 0xFF172382, 0xFF0F1E57, 0xFF112162, 0xFF13256C, 0xFF152977, 0xFF172D82,
            },
            // Homebrew Neutral Red Armor Paint
            [24626] = new uint[]
            {
                0xFF2B2640, 0xFF312B48, 0xFF363050, 0xFF3C3558, 0xFF413960, 0xFF292640, 0xFF2E2B48, 0xFF333050, 0xFF383558,
                0xFF3D3960, 0xFF262640, 0xFF2B2B48, 0xFF303050, 0xFF353558, 0xFF393960, 0xFF262940, 0xFF2B2E48, 0xFF303350,
                0xFF353858, 0xFF393D60, 0xFF262B40, 0xFF2B3148, 0xFF303650, 0xFF353C58, 0xFF394160,
            },
            // Homebrew Bright Red Armor Paint
            [24627] = new uint[]
            {
                0xFF2C1882, 0xFF301A8D, 0xFF341C98, 0xFF371EA3, 0xFF3B20AD, 0xFF221882, 0xFF241A8D, 0xFF271C98, 0xFF2A1EA3,
                0xFF2D20AD, 0xFF171882, 0xFF191A8D, 0xFF1B1C98, 0xFF1D1EA3, 0xFF1F20AD, 0xFF172382, 0xFF19258D, 0xFF1B2898,
                0xFF1D2BA3, 0xFF1F2EAD, 0xFF172D82, 0xFF19318D, 0xFF1B3598, 0xFF1D38A3, 0xFF1F3CAD,
            },
            // Homebrew Dark Green Armor Paint
            [24628] = new uint[]
            {
                0xFF0F571E, 0xFF116221, 0xFF136C25, 0xFF157729, 0xFF18822C, 0xFF0F5716, 0xFF116219, 0xFF136C1C, 0xFF15771F,
                0xFF188222, 0xFF0F570F, 0xFF116211, 0xFF136C13, 0xFF157715, 0xFF188217, 0xFF16570F, 0xFF196211, 0xFF1C6C13,
                0xFF1F7715, 0xFF238217, 0xFF1E570F, 0xFF216211, 0xFF256C13, 0xFF297715, 0xFF2D8217,
            },
            // Homebrew Neutral Green Armor Paint
            [24629] = new uint[]
            {
                0xFF26402B, 0xFF2B4831, 0xFF305036, 0xFF35583C, 0xFF396041, 0xFF264029, 0xFF2B482E, 0xFF305033, 0xFF355838,
                0xFF39603D, 0xFF264026, 0xFF2B482B, 0xFF305030, 0xFF355835, 0xFF396039, 0xFF294026, 0xFF2E482B, 0xFF335030,
                0xFF385835, 0xFF3D6039, 0xFF2B4026, 0xFF31482B, 0xFF365030, 0xFF3C5835, 0xFF416039,
            },
            // Homebrew Bright Green Armor Paint
            [24630] = new uint[]
            {
                0xFF18822C, 0xFF1A8D30, 0xFF1C9834, 0xFF1EA337, 0xFF20AD3B, 0xFF188222, 0xFF1A8D24, 0xFF1C9827, 0xFF1EA32A,
                0xFF20AD2D, 0xFF188217, 0xFF1A8D19, 0xFF1C981B, 0xFF1EA31D, 0xFF20AD1F, 0xFF238217, 0xFF258D19, 0xFF28981B,
                0xFF2BA31D, 0xFF2EAD1F, 0xFF2D8217, 0xFF318D19, 0xFF35981B, 0xFF38A31D, 0xFF3CAD1F,
            },
            // Homebrew Dark Blue Armor Paint
            [24631] = new uint[]
            {
                0xFF571E0F, 0xFF622111, 0xFF6C2513, 0xFF772915, 0xFF822C17, 0xFF57160F, 0xFF621911, 0xFF6C1C13, 0xFF771F15,
                0xFF822217, 0xFF570F0F, 0xFF621111, 0xFF6C1313, 0xFF771515, 0xFF821717, 0xFF570F16, 0xFF621119, 0xFF6C131C,
                0xFF77151F, 0xFF821722, 0xFF570F1E, 0xFF621121, 0xFF6C1325, 0xFF771529, 0xFF82172C,
            },
            // Homebrew Neutral Blue Armor Paint
            [24632] = new uint[]
            {
                0xFF402B26, 0xFF48312B, 0xFF503630, 0xFF583C35, 0xFF604139, 0xFF402926, 0xFF482E2B, 0xFF503330, 0xFF583835,
                0xFF603D39, 0xFF402626, 0xFF482B2B, 0xFF503030, 0xFF583535, 0xFF603939, 0xFF402629, 0xFF482B2E, 0xFF503033,
                0xFF583538, 0xFF60393D, 0xFF40262B, 0xFF482B31, 0xFF503036, 0xFF58353C, 0xFF603941,
            },
            // Homebrew Bright Blue Armor Paint
            [24633] = new uint[]
            {
                0xFF822C17, 0xFF8D3019, 0xFF98341B, 0xFFA3371D, 0xFFAD3B1F, 0xFF822217, 0xFF8D2419, 0xFF98271B, 0xFFA32A1D,
                0xFFAD2D1F, 0xFF821717, 0xFF8D1919, 0xFF981B1B, 0xFFA31D1D, 0xFFAD1F1F, 0xFF821722, 0xFF8D1924, 0xFF981B27,
                0xFFA31D2A, 0xFFAD1F2D, 0xFF82172C, 0xFF8D1930, 0xFF981B34, 0xFFA31D37, 0xFFAD1F3B,
            },
            // Homebrew Dark Orange Armor Paint
            [24634] = new uint[]
            {
                0xFF0F2557, 0xFF112962, 0xFF132E6C, 0xFF153277, 0xFF173882, 0xFF0F2C57, 0xFF113162, 0xFF13376C, 0xFF153C77,
                0xFF174382, 0xFF0F3357, 0xFF113962, 0xFF13406C, 0xFF154677, 0xFF174D82, 0xFF0F3A57, 0xFF114162, 0xFF13496C,
                0xFF155077, 0xFF175882, 0xFF0F4157, 0xFF114962, 0xFF13526C, 0xFF155A77, 0xFF176382,
            },
            // Homebrew Neutral Orange Armor Paint
            [24635] = new uint[]
            {
                0xFF262E40, 0xFF2B3448, 0xFF303950, 0xFF353F58, 0xFF394560, 0xFF263040, 0xFF2B3748, 0xFF303D50, 0xFF354358,
                0xFF394960, 0xFF263340, 0xFF2B3948, 0xFF304050, 0xFF354658, 0xFF394C60, 0xFF263640, 0xFF2B3C48, 0xFF304350,
                0xFF354A58, 0xFF395060, 0xFF263840, 0xFF2B3F48, 0xFF304650, 0xFF354D58, 0xFF395460,
            },
            // Homebrew Bright Orange Armor Paint
            [24636] = new uint[]
            {
                0xFF173882, 0xFF193D8D, 0xFF1B4198, 0xFF1D46A3, 0xFF1F4AAD, 0xFF174382, 0xFF19488D, 0xFF1B4E98, 0xFF1D53A3,
                0xFF1F59AD, 0xFF174D82, 0xFF19548D, 0xFF1B5A98, 0xFF1D61A3, 0xFF1F67AD, 0xFF175882, 0xFF195F8D, 0xFF1B6798,
                0xFF1D6EA3, 0xFF1F75AD, 0xFF176382, 0xFF196B8D, 0xFF1B7398, 0xFF1D7BA3, 0xFF2084AD,
            },
            // Homebrew Dark Lime Armor Paint
            [24637] = new uint[]
            {
                0xFF0F5741, 0xFF116249, 0xFF136C52, 0xFF15775A, 0xFF188262, 0xFF0F573A, 0xFF116241, 0xFF136C49, 0xFF157750,
                0xFF188257, 0xFF0F5733, 0xFF116239, 0xFF136C40, 0xFF157746, 0xFF18824C, 0xFF0F572C, 0xFF116231, 0xFF136C37,
                0xFF15773C, 0xFF188242, 0xFF0F5725, 0xFF116229, 0xFF136C2E, 0xFF157732, 0xFF188237,
            },
            // Homebrew Neutral Lime Armor Paint
            [24638] = new uint[]
            {
                0xFF264038, 0xFF2B483F, 0xFF305046, 0xFF35584D, 0xFF396054, 0xFF264036, 0xFF2B483C, 0xFF305043, 0xFF35584A,
                0xFF396050, 0xFF264033, 0xFF2B4839, 0xFF305040, 0xFF355846, 0xFF39604C, 0xFF264030, 0xFF2B4837, 0xFF30503D,
                0xFF355843, 0xFF396049, 0xFF26402E, 0xFF2B4834, 0xFF305039, 0xFF35583F, 0xFF396045,
            },
            // Homebrew Bright Lime Armor Paint
            [24639] = new uint[]
            {
                0xFF188262, 0xFF1A8D6A, 0xFF1C9872, 0xFF1EA37A, 0xFF20AE83, 0xFF188257, 0xFF1A8D5E, 0xFF1C9866, 0xFF1EA36D,
                0xFF20AD74, 0xFF18824C, 0xFF1A8D53, 0xFF1C9859, 0xFF1EA360, 0xFF20AD66, 0xFF188242, 0xFF1A8D47, 0xFF1C984D,
                0xFF1EA352, 0xFF20AD58, 0xFF188237, 0xFF1A8D3C, 0xFF1C9840, 0xFF1EA345, 0xFF20AD49,
            },
            // Homebrew Dark Mint Armor Paint
            [24640] = new uint[]
            {
                0xFF25570F, 0xFF296211, 0xFF2E6C13, 0xFF327715, 0xFF388217, 0xFF2C570F, 0xFF316211, 0xFF376C13, 0xFF3C7715,
                0xFF438217, 0xFF33570F, 0xFF396211, 0xFF406C13, 0xFF467715, 0xFF4E8217, 0xFF3A570F, 0xFF416211, 0xFF496C13,
                0xFF507715, 0xFF588217, 0xFF41570F, 0xFF496211, 0xFF526C13, 0xFF5A7715, 0xFF638217,
            },
            // Homebrew Neutral Mint Armor Paint
            [24641] = new uint[]
            {
                0xFF2E4026, 0xFF34482B, 0xFF395030, 0xFF3F5835, 0xFF456039, 0xFF304026, 0xFF37482B, 0xFF3D5030, 0xFF435835,
                0xFF496039, 0xFF334026, 0xFF39482B, 0xFF405030, 0xFF465835, 0xFF4D6039, 0xFF364026, 0xFF3C482B, 0xFF435030,
                0xFF4A5835, 0xFF506039, 0xFF384026, 0xFF3F482B, 0xFF465030, 0xFF4D5835, 0xFF546039,
            },
            // Homebrew Bright Mint Armor Paint
            [24642] = new uint[]
            {
                0xFF388217, 0xFF3D8D19, 0xFF41981B, 0xFF46A31D, 0xFF4AAD1F, 0xFF438217, 0xFF488D19, 0xFF4E981B, 0xFF53A31D,
                0xFF59AD1F, 0xFF4E8217, 0xFF548D19, 0xFF5A981B, 0xFF61A31D, 0xFF67AD1F, 0xFF588217, 0xFF5F8D19, 0xFF67981B,
                0xFF6EA31D, 0xFF75AD1F, 0xFF638217, 0xFF6B8D19, 0xFF73981B, 0xFF7BA31D, 0xFF84AD1F,
            },
            // Homebrew Dark Azure Armor Paint
            [24643] = new uint[]
            {
                0xFF57410F, 0xFF624911, 0xFF6C5213, 0xFF775A15, 0xFF826217, 0xFF573A0F, 0xFF624111, 0xFF6C4913, 0xFF775015,
                0xFF825717, 0xFF57330F, 0xFF623911, 0xFF6C4013, 0xFF774615, 0xFF824D17, 0xFF572C0F, 0xFF623111, 0xFF6C3713,
                0xFF773C15, 0xFF824217, 0xFF57250F, 0xFF622911, 0xFF6C2E13, 0xFF773215, 0xFF823717,
            },
            // Homebrew Neutral Azure Armor Paint
            [24644] = new uint[]
            {
                0xFF403826, 0xFF483F2B, 0xFF504630, 0xFF584D35, 0xFF605439, 0xFF403626, 0xFF483C2B, 0xFF504330, 0xFF584A35,
                0xFF605039, 0xFF403326, 0xFF48392B, 0xFF504030, 0xFF584635, 0xFF604D39, 0xFF403026, 0xFF48372B, 0xFF503D30,
                0xFF584335, 0xFF604939, 0xFF402E26, 0xFF48342B, 0xFF503930, 0xFF583F35, 0xFF604539,
            },
            // Homebrew Bright Azure Armor Paint
            [24645] = new uint[]
            {
                0xFF826217, 0xFF8D6A19, 0xFF98721B, 0xFFA37A1D, 0xFFAE831F, 0xFF825717, 0xFF8D5E19, 0xFF98661B, 0xFFA36D1D,
                0xFFAD741F, 0xFF824D17, 0xFF8D5319, 0xFF98591B, 0xFFA3601D, 0xFFAD661F, 0xFF824217, 0xFF8D4719, 0xFF984D1B,
                0xFFA3521D, 0xFFAD581F, 0xFF823717, 0xFF8D3C19, 0xFF98401B, 0xFFA3451D, 0xFFAD491F,
            },
            // Homebrew Dark Violet Armor Paint
            [24646] = new uint[]
            {
                0xFF570F25, 0xFF621129, 0xFF6C132E, 0xFF771532, 0xFF821737, 0xFF570F2C, 0xFF621131, 0xFF6C1337, 0xFF77153C,
                0xFF821742, 0xFF570F33, 0xFF621139, 0xFF6C1340, 0xFF771546, 0xFF82174C, 0xFF570F3A, 0xFF621141, 0xFF6C1349,
                0xFF771550, 0xFF821757, 0xFF570F41, 0xFF621149, 0xFF6C1352, 0xFF77155A, 0xFF821762,
            },
            // Homebrew Neutral Violet Armor Paint
            [24647] = new uint[]
            {
                0xFF40262E, 0xFF482B34, 0xFF503039, 0xFF58353F, 0xFF603945, 0xFF402630, 0xFF482B37, 0xFF50303D, 0xFF583543,
                0xFF603949, 0xFF402633, 0xFF482B39, 0xFF503040, 0xFF583546, 0xFF60394C, 0xFF402636, 0xFF482B3C, 0xFF503043,
                0xFF58354A, 0xFF603950, 0xFF402638, 0xFF482B3F, 0xFF503046, 0xFF58354D, 0xFF603954,
            },
            // Homebrew Bright Violet Armor Paint
            [24648] = new uint[]
            {
                0xFF821737, 0xFF8D193C, 0xFF981B40, 0xFFA31D45, 0xFFAD1F49, 0xFF821742, 0xFF8D1947, 0xFF981B4D, 0xFFA31D52,
                0xFFAD1F58, 0xFF82174C, 0xFF8D1953, 0xFF981B59, 0xFFA31D60, 0xFFAD1F66, 0xFF821757, 0xFF8D195E, 0xFF981B66,
                0xFFA31D6D, 0xFFAD1F74, 0xFF821762, 0xFF8D196A, 0xFF981B72, 0xFFA31D7A, 0xFFAD2083,
            },
            // Homebrew Dark Purple Armor Paint
            [24649] = new uint[]
            {
                0xFF570F48, 0xFF621151, 0xFF6C135B, 0xFF771564, 0xFF82176D, 0xFF570F50, 0xFF62115A, 0xFF6C1363, 0xFF77156D,
                0xFF821777, 0xFF570F57, 0xFF621162, 0xFF6C136C, 0xFF771577, 0xFF821882, 0xFF500F57, 0xFF5A1162, 0xFF63136C,
                0xFF6D1577, 0xFF771882, 0xFF480F57, 0xFF511162, 0xFF5B136C, 0xFF641577, 0xFF6D1882,
            },
            // Homebrew Neutral Purple Armor Paint
            [24650] = new uint[]
            {
                0xFF40263B, 0xFF482B42, 0xFF503049, 0xFF583551, 0xFF603958, 0xFF40263D, 0xFF482B45, 0xFF50304C, 0xFF583554,
                0xFF60395C, 0xFF402640, 0xFF482B48, 0xFF503050, 0xFF583558, 0xFF603960, 0xFF3D2640, 0xFF452B48, 0xFF4D3050,
                0xFF543558, 0xFF5C3960, 0xFF3B2640, 0xFF422B48, 0xFF493050, 0xFF513558, 0xFF583960,
            },
            // Homebrew Bright Purple Armor Paint
            [24651] = new uint[]
            {
                0xFF82176D, 0xFF8D1976, 0xFF981B7F, 0xFFA31E88, 0xFFAD2091, 0xFF821777, 0xFF8D1A81, 0xFF981C8B, 0xFFA31E95,
                0xFFAD209F, 0xFF821882, 0xFF8D1A8D, 0xFF981C98, 0xFFA31EA3, 0xFFAD20AD, 0xFF771882, 0xFF811A8D, 0xFF8B1C98,
                0xFF951EA3, 0xFF9F20AD, 0xFF6D1882, 0xFF761A8D, 0xFF7F1C98, 0xFF881EA3, 0xFF9120AD,
            },
            // Homebrew Dark Gray Armor Paint
            [24652] = new uint[]
            {
                0xFF202020, 0xFF222222, 0xFF242424, 0xFF262626, 0xFF282828, 0xFF2A2A2A, 0xFF2C2C2C, 0xFF2E2E2E, 0xFF303030,
                0xFF323232, 0xFF343434, 0xFF363636, 0xFF383838, 0xFF3A3A3A, 0xFF3C3C3C, 0xFF3E3E3E, 0xFF404040, 0xFF424242,
                0xFF444444, 0xFF464646, 0xFF484848, 0xFF4A4A4A, 0xFF4C4C4C, 0xFF4E4E4E, 0xFF505050,
            },
            // Homebrew Neutral Gray Armor Paint
            [24653] = new uint[]
            {
                0xFF606060, 0xFF626262, 0xFF646464, 0xFF666666, 0xFF686868, 0xFF6A6A6A, 0xFF6C6C6C, 0xFF6E6E6E, 0xFF707070,
                0xFF727272, 0xFF747474, 0xFF767676, 0xFF787878, 0xFF7A7A7A, 0xFF7C7C7C, 0xFF7E7E7E, 0xFF808080, 0xFF828282,
                0xFF848484, 0xFF868686, 0xFF888888, 0xFF8A8A8A, 0xFF8C8C8C, 0xFF8E8E8E, 0xFF909090,
            },
            // Homebrew Bright Gray Armor Paint
            [24654] = new uint[]
            {
                0xFFA0A0A0, 0xFFA2A2A2, 0xFFA4A4A4, 0xFFA6A6A6, 0xFFA8A8A8, 0xFFAAAAAA, 0xFFACACAC, 0xFFAEAEAE, 0xFFB0B0B0,
                0xFFB2B2B2, 0xFFB4B4B4, 0xFFB6B6B6, 0xFFB8B8B8, 0xFFBABABA, 0xFFBCBCBC, 0xFFBEBEBE, 0xFFC0C0C0, 0xFFC2C2C2,
                0xFFC4C4C4, 0xFFC6C6C6, 0xFFC8C8C8, 0xFFCACACA, 0xFFCCCCCC, 0xFFCECECE, 0xFFD0D0D0,
            },
            // Bane Blood Red Armor Paint
            [26380] = new uint[]
            {
                0xFF2E36DA, 0xFF3840DC, 0xFF434BDE, 0xFF4E55E0, 0xFF5960E2, 0xFF2E41DA, 0xFF384BDC, 0xFF4355DE, 0xFF4E5FE0,
                0xFF5969E2, 0xFF2E4DDA, 0xFF3856DC, 0xFF435FDE, 0xFF4E69E0, 0xFF5972E2, 0xFF2E58DA, 0xFF3861DC, 0xFF436ADE,
                0xFF4E72E0, 0xFF597BE2, 0xFF2E64DA, 0xFF386CDC, 0xFF4374DE, 0xFF4E7CE0, 0xFF5A84E2,
            },
            // Light Bender Yellow Armor Paint
            [26381] = new uint[]
            {
                0xFF2F8FDA, 0xFF3995DC, 0xFF449BDE, 0xFF4FA1E0, 0xFF5AA7E2, 0xFF2F9BDA, 0xFF39A0DC, 0xFF44A5DE, 0xFF4FABE0,
                0xFF5AB0E2, 0xFF2FA6DA, 0xFF39ABDC, 0xFF44B0DE, 0xFF4FB4E0, 0xFF5AB9E2, 0xFF2FB2DA, 0xFF39B6DC, 0xFF44BADE,
                0xFF4FBEE0, 0xFF5AC2E2, 0xFF2FBDDA, 0xFF39C1DC, 0xFF44C4DE, 0xFF4FC8E0, 0xFF5ACBE2,
            },
            // Shield Drone Green Armor Paint
            [26382] = new uint[]
            {
                0xFF2FDA78, 0xFF39DC7F, 0xFF44DF86, 0xFF4FE18D, 0xFF5AE394, 0xFF2FDA6D, 0xFF39DC74, 0xFF44DE7C, 0xFF4FE184,
                0xFF5AE38B, 0xFF2FDA61, 0xFF39DC6A, 0xFF44DE72, 0xFF4FE07A, 0xFF5AE382, 0xFF2FDA56, 0xFF39DC5F, 0xFF44DE67,
                0xFF4FE070, 0xFF5AE279, 0xFF2FDA4A, 0xFF39DC54, 0xFF44DE5D, 0xFF4FE066, 0xFF5AE270,
            },
            // AFS Blue Armor Paint
            [26383] = new uint[]
            {
                0xFFD5DA2E, 0xFFD8DC38, 0xFFDADE43, 0xFFDCE04E, 0xFFDEE259, 0xFFDBD42E, 0xFFDDD738, 0xFFDFD943, 0xFFE1DB4E,
                0xFFE3DD59, 0xFFDBC92E, 0xFFDDCC38, 0xFFDFCE43, 0xFFE1D14E, 0xFFE3D459, 0xFFDBBD2E, 0xFFDDC138, 0xFFDFC443,
                0xFFE1C84E, 0xFFE3CB59, 0xFFDBB22E, 0xFFDDB638, 0xFFDFBA43, 0xFFE1BE4E, 0xFFE3C259,
            },
            // Boxing Gear Color Change Kit
            [29218] = new uint[]
            {
                0xFF8F8FEF, 0xFF5858E7, 0xFF2020DF, 0xFF1717A8, 0xFF101070, 0xFFA8EF8F, 0xFF7CE758, 0xFF50DF20, 0xFF3CA817,
                0xFF287010, 0xFFEFEF8F, 0xFFE7E758, 0xFFDFDF20, 0xFFA8A817, 0xFF707010, 0xFFEFA88F, 0xFFE77C58, 0xFFDF5020,
                0xFFA83C17, 0xFF702810, 0xFFEF8FBF, 0xFFE7589F, 0xFFDF2080, 0xFFA81760, 0xFF701040,
            },
        };

        // palette_hum_hair_cauc.dds, _asian and _african, of data\ui.glm: one file under three
        // names. 256 by 256, DXT1: 64 swatches, eight a row, with black lines between them.
        private static readonly uint[] HairPalette =
        {
            0xFFE7E3DE, 0xFFDEE3E7, 0xFFD6E3EF, 0xFFCEE7F7, 0xFFCEDFF7, 0xFFD6D7EF, 0xFFD6DBEF, 0xFFE4E1E1, 0xFFDEE3E6,
            0xFFDAE3E7, 0xFFD3E4EF, 0xFFD0E5F4, 0xFFCEDEF3, 0xFFD6D6EF, 0xFFD6DBEC, 0xFFCECFCE, 0xFFCECFD6, 0xFFC6D3DE,
            0xFFBDD3E7, 0xFFB5D3EF, 0xFFB5C7EF, 0xFFBDBAE7, 0xFFBDC3DE, 0xFFD2D0CE, 0xFFCBD0D3, 0xFFC5D2DA, 0xFFBAD4E4,
            0xFFB2D4EC, 0xFFB2C8EC, 0xFFBABBE6, 0xFFBDC4E1, 0xFFBDBAB5, 0xFFB5BEC6, 0xFFADBECE, 0xFF9CC3D6, 0xFF8CC3E7,
            0xFF8CB2E7, 0xFF9C9EDE, 0xFFA5AAD6, 0xFFBDBAB9, 0xFFB5BCC1, 0xFFAABECB, 0xFF9CC1D8, 0xFF8EC3E7, 0xFF8EB0E7,
            0xFF999CDB, 0xFFA4AAD2, 0xFFA4A4A4, 0xFF9CA6AD, 0xFF8CAABD, 0xFF7BAECE, 0xFF6BAEDE, 0xFF6B96DE, 0xFF7B7DCE,
            0xFF8492C6, 0xFFA7A3A2, 0xFF99A6AF, 0xFF8EAABA, 0xFF7EAFCE, 0xFF6BAFDE, 0xFF6A96DE, 0xFF787BD0, 0xFF8690C5,
            0xFF8E8A89, 0xFF7E8C9A, 0xFF6E92A4, 0xFF5A96BD, 0xFF429AD6, 0xFF4279D6, 0xFF5259C6, 0xFF6371B5, 0xFF8C8987,
            0xFF7F8D98, 0xFF6F93A5, 0xFF5A97BD, 0xFF4299D6, 0xFF4277D6, 0xFF5459C3, 0xFF6572B2, 0xFF635F5E, 0xFF52636F,
            0xFF426B7F, 0xFF2C6E9A, 0xFF186DB5, 0xFF1849B5, 0xFF26289F, 0xFF36468E, 0xFF446A7E, 0xFF2B6E9C, 0xFF186DB9,
            0xFF184AB9, 0xFF26299F, 0xFF393A39, 0xFF2E3A42, 0xFF253E4E, 0xFF18435E, 0xFF0C4177, 0xFF0C2A77, 0xFF141667,
            0xFF202858, 0xFF393939, 0xFF313A42, 0xFF184260, 0xFF0A4175, 0xFF0A2975, 0xFF141666, 0xFF1E2857, 0xFF020202,
            0xFF050505, 0xFF1C2025, 0xFF14222D, 0xFF0C2439, 0xFF082646, 0xFF051944, 0xFF0B0C39, 0xFF101531, 0xFF040404,
            0xFF000400, 0xFF082645, 0xFF0A0C39,
        };

        /// <summary>
        /// The colours a hair or skin colour item offers: every colour of its palette texture
        /// (customizationClassPalette, customizationwindow.py _SetupPalette) but that of the
        /// pixel at its corner, which the window takes for the border and does not let be
        /// picked (OnColorClicked) - on the hair palette the black of the lines, on a skin
        /// palette, which starts with a swatch, the first swatch. A click takes the pixel under
        /// it (GetPixelColor), so the blends DXT1 leaves along a swatch's edge are colours too.
        /// Packed as <see cref="Hues"/> are, in the order they are met reading the texture. The
        /// corner's colour may still be asked for where another colour of the palette is within
        /// <see cref="PaletteTolerance"/> of it: the hair palette's darkest swatch is all but black.
        /// </summary>
        public static readonly IReadOnlyDictionary<uint, uint[]> Palettes = new Dictionary<uint, uint[]>
        {
            [3735] = HairPalette, // Hair Color Modification 3
            [4088] = HairPalette, // Hair Color Modification 2
            [4089] = HairPalette, // Hair Color Modification 1
            // Melanotan for Caucasians: palette_hum_skin_cauc.dds, 16 swatches
            [3734] = new uint[]
            {
                0xFF626365, 0xFF9CA3B2, 0xFF636563, 0xFF9CA8B1, 0xFF9CACB1, 0xFF626363, 0xFF656365, 0xFF626265, 0xFF606265,
                0xFF636363, 0xFF94B2C6, 0xFF899FB2, 0xFF889EB1, 0xFF738899, 0xFF768899, 0xFF637584, 0xFF96B3C5, 0xFF758899,
                0xFF657683, 0xFF636163, 0xFF626362, 0xFF99A7C5, 0xFF8997B2, 0xFF73829C, 0xFF738199, 0xFF636E84, 0xFF98A8C5,
                0xFF8897B1, 0xFF758299, 0xFF656E83, 0xFF626465, 0xFF949EC6, 0xFF898EB2, 0xFF888FB1, 0xFF737B99, 0xFF737999,
                0xFF636984, 0xFF969FC5, 0xFF757A99, 0xFF636983,
            },
            // Melanotan for Africans: palette_hum_skin_african.dds, 16 swatches
            [4086] = new uint[]
            {
                0xFF31394D, 0xFF293842, 0xFF2D3842, 0xFF313652, 0xFF393552, 0xFF36344F, 0xFF33344C, 0xFF474A68, 0xFF29384A,
                0xFF39475A, 0xFF364657, 0xFF313052, 0xFF343157, 0xFF313852, 0xFF3E475F, 0xFF424163, 0xFF3C3F5D, 0xFF5A657B,
                0xFF3C475D, 0xFF5A597B, 0xFF3E3D5F, 0xFF3F3B5D, 0xFF3C495D, 0xFF3D485E, 0xFF5A5D7B, 0xFF3D3C5E, 0xFF3F3A5D,
                0xFF5A617B, 0xFF3C485D, 0xFF3C3F57, 0xFF39424F, 0xFF4A5563, 0xFF44435A, 0xFF52516B, 0xFF524E68, 0xFF3F4855,
                0xFF445260, 0xFF26293C, 0xFF282F3C, 0xFF292839, 0xFF29273C, 0xFF293039, 0xFF262F3C, 0xFF3C4252, 0xFF262F39,
                0xFF262E3C, 0xFF474857, 0xFF2B283C, 0xFF2B263C, 0xFF444A57, 0xFF31384A, 0xFF262D37, 0xFF313842, 0xFF393C42,
                0xFF2E2A36, 0xFF393842, 0xFF393845, 0xFF2E3239, 0xFF151523, 0xFF101821, 0xFF131920, 0xFF292431, 0xFF232931,
                0xFF242A31, 0xFF141624, 0xFF36363E, 0xFF1B1B23, 0xFF141A24, 0xFF2D2C35, 0xFF262531, 0xFF313439, 0xFF252A31,
            },
            // Skin Color Modification: palette_hum_skin_asian.dds, 16 swatches
            [4087] = new uint[]
            {
                0xFF9AB1CE, 0xFF84A2C6, 0xFF86A2C8, 0xFF6B7F9C, 0xFF6A7F9C, 0xFF627387, 0xFF5A7594, 0xFF5E7594, 0xFFADBAD6,
                0xFFADB6CE, 0xFF8492C6, 0xFF848EB5, 0xFF7689B0, 0xFF68759A, 0xFF687394, 0xFF637594, 0xFF5E6990, 0xFF63698C,
                0xFFAFB7D3, 0xFF919EC8, 0xFF8494C9, 0xFF6B759C, 0xFF5E6994, 0xFF949FC2, 0xFF8394C8, 0xFF8694C8, 0xFF6B7594,
                0xFF6A759C, 0xFF5E6987, 0xFF5A6994, 0xFF8697B2, 0xFF8C9AB5, 0xFF525D6B, 0xFF4F5B70, 0xFF4A5563, 0xFF353E4A,
                0xFF363F4C, 0xFF293039, 0xFF181C20, 0xFF182021, 0xFF8094AF, 0xFF657183, 0xFF4C5A6D, 0xFF333D47, 0xFF181A1C,
                0xFF8394B1, 0xFF637184, 0xFF4F5A6E, 0xFF4F5A6D, 0xFF363C47, 0xFF7688A1, 0xFF7E8BA7, 0xFF52556B, 0xFF4C5A6B,
                0xFF4C535F, 0xFF3E414C, 0xFF39414A, 0xFF2E3337, 0xFF23252C, 0xFF292829, 0xFF8187AF, 0xFF687089, 0xFF4F536B,
                0xFF41424C, 0xFF394142, 0xFF181821, 0xFF8387B1, 0xFF6B718C, 0xFF4F536D, 0xFF525163, 0xFF41424E, 0xFF393C42,
                0xFF18191B, 0xFF18191E,
            },
        };

        /// <summary>
        /// The hair and the faces a modification item offers (customizationChoice), each with
        /// the item template the windows name it by - the one the creation window has for it
        /// (generated.client.starterinfo) - and in that window's order. All but Bald are human.
        /// </summary>
        public static readonly IReadOnlyDictionary<uint, (uint ClassId, uint TemplateId)[]> Choices = new Dictionary<uint, (uint, uint)[]>
        {
            // Hairstyle Modification
            [1068] = new (uint, uint)[]
            {
                (3672, 42), // Style A
                (3663, 36), // Style B
                (9355, 1775), // Style C
                (9356, 1776), // Style D
                (9781, 1941), // Style E
                (9782, 1942), // Style F
                (9783, 1943), // Style G
                (9784, 1944), // Style H
                (3812, 60), // Bald
            },
            // Face Modification
            [1069] = new (uint, uint)[]
            {
                (20824, 42276), // Asian v3 (Average)
                (4083, 99), // Asian v1 (Average)
                (10239, 2245), // Asian v2 (Average)
                (20825, 42277), // Asian v4 (Average)
                (24016, 49684), // Asian v1 (Round)
                (24017, 49685), // Asian v2 (Round)
                (24018, 49686), // Asian v3 (Round)
                (24019, 49687), // Asian v4 (Round)
                (24028, 49696), // Asian v1 (Square)
                (24029, 49697), // Asian v2 (Square)
                (24030, 49698), // Asian v3 (Square)
                (24031, 49699), // Asian v4 (Square)
                (24004, 49672), // Asian v1 (Long)
                (24005, 49673), // Asian v2 (Long)
                (24006, 49674), // Asian v3 (Long)
                (24007, 49675), // Asian v4 (Long)
                (3813, 61), // Caucasian v1 (Average)
                (7508, 611), // Caucasian v2 (Average)
                (7694, 683), // Caucasian v3 (Average)
                (7695, 684), // Caucasian v4 (Average)
                (24020, 49688), // Caucasian v1 (Round)
                (24021, 49689), // Caucasian v2 (Round)
                (24022, 49690), // Caucasian v3 (Round)
                (24023, 49691), // Caucasian v4 (Round)
                (24032, 49700), // Caucasian v1 (Square)
                (24033, 49701), // Caucasian v2 (Square)
                (24034, 49702), // Caucasian v3 (Square)
                (24035, 49703), // Caucasian v4 (Square)
                (24008, 49676), // Caucasian v1 (Long)
                (24009, 49677), // Caucasian v2 (Long)
                (24010, 49678), // Caucasian v3 (Long)
                (24011, 49679), // Caucasian v4 (Long)
                (3667, 39), // African v1 (Average)
                (7506, 609), // African v2 (Average)
                (7507, 610), // African v3 (Average)
                (7693, 682), // African v4 (Average)
                (24012, 49680), // African v1 (Round)
                (24013, 49681), // African v2 (Round)
                (24014, 49682), // African v3 (Round)
                (24015, 49683), // African v4 (Round)
                (24024, 49692), // African v1 (Square)
                (24025, 49693), // African v2 (Square)
                (24026, 49694), // African v3 (Square)
                (24027, 49695), // African v4 (Square)
                (24000, 49668), // African v1 (Long)
                (24001, 49669), // African v2 (Long)
                (24002, 49670), // African v3 (Long)
                (24003, 49671), // African v4 (Long)
            },
        };

        /// <summary>The armour classes every armour paint with a restriction list may colour (2550).</summary>
        private static readonly HashSet<uint> Armor = new HashSet<uint>
        {
            6470, 6473, 6474, 6495, 6498, 6499, 6590, 6592, 6593, 6594, 6690, 6692,
            6693, 6694, 6715, 6717, 6718, 6719, 6720, 6721, 6722, 6723, 6724, 6725,
            6727, 6728, 6729, 6780, 6782, 6783, 6784, 6855, 6857, 6858, 6859, 6880,
            6881, 6882, 6883, 6884, 6885, 6886, 6887, 6888, 6889, 9489, 9490, 9491,
            9516, 9517, 9518, 9531, 9532, 9533, 9815, 9819, 9820, 9827, 9829, 9837,
            9844, 9852, 9856, 9860, 9862, 9869, 9870, 9873, 9875, 9886, 9897, 9961,
            9962, 9967, 9968, 9971, 9975, 9986, 9987, 9992, 9994, 10003, 10006, 10008,
            10015, 10019, 10022, 10026, 10032, 10042, 10049, 10050, 10051, 10065, 10066, 10068,
            10079, 10080, 13008, 13013, 13018, 13022, 13025, 13028, 13030, 13035, 13040, 13044,
            13047, 13050, 13052, 13057, 13062, 13066, 13069, 13072, 13074, 13079, 13084, 13088,
            13091, 13094, 13096, 13101, 13106, 13110, 13113, 13116, 13118, 13123, 13128, 13133,
            13138, 13143, 13148, 13153, 13154, 13159, 13164, 13169, 13174, 13179, 13184, 13189,
            13190, 13195, 13200, 13205, 13210, 13215, 13220, 13225, 13226, 13231, 13236, 13241,
            13246, 13251, 13256, 13261, 13262, 13267, 13272, 13277, 13282, 13287, 13292, 13297,
            13298, 13303, 13308, 13313, 13318, 13323, 13328, 13333, 13334, 13339, 13344, 13349,
            13354, 13359, 13364, 13369, 13370, 13375, 13380, 13385, 13390, 13395, 13400, 13405,
            13406, 13411, 13416, 13421, 13426, 13431, 13436, 13441, 13442, 13447, 13452, 13457,
            13462, 13467, 13472, 13477, 13478, 13483, 13487, 13491, 13498, 13501, 13504, 13506,
            13511, 13515, 13519, 13526, 13529, 13532, 13534, 13539, 13543, 13547, 13554, 13557,
            13560, 13562, 13567, 13571, 13575, 13582, 13585, 13588, 13590, 13595, 13599, 13603,
            13610, 13613, 13616, 13618, 13623, 13628, 13633, 13638, 13643, 13648, 13653, 13658,
            13663, 13664, 13669, 13674, 13679, 13684, 13689, 13694, 13699, 13704, 13709, 13710,
            13715, 13720, 13725, 13730, 13735, 13740, 13745, 13750, 13755, 13756, 13761, 13766,
            13771, 13776, 13781, 13786, 13791, 13796, 13801, 13802, 13807, 13812, 13817, 13822,
            13827, 13832, 13837, 13842, 13847, 13848, 13853, 13858, 13863, 13868, 13873, 13878,
            13883, 13888, 13893, 13894, 13899, 13904, 13909, 13914, 13919, 13924, 13929, 13934,
            13939, 13940, 13945, 13950, 13955, 13960, 13965, 13970, 13975, 13980, 13985, 13986,
            13991, 13996, 14001, 14006, 14011, 14016, 14021, 14026, 14031, 14032, 14037, 14042,
            14047, 14052, 14057, 14062, 14067, 14072, 14077, 14078, 14083, 14088, 14093, 14098,
            14103, 14108, 14113, 14118, 14123, 14124, 14129, 14134, 14139, 14144, 14149, 14154,
            14159, 14164, 14169, 14170, 14175, 14180, 14185, 14190, 14195, 14200, 14205, 14210,
            14215, 14216, 14221, 14226, 14231, 14236, 14241, 14246, 14251, 14256, 14261, 14262,
            14267, 14272, 14277, 14282, 14287, 14292, 14297, 14302, 14307, 14308, 14313, 14318,
            14323, 14328, 14333, 14338, 14343, 14348, 14353, 14354, 14359, 14364, 14369, 14374,
            14379, 14384, 14389, 14394, 14399, 14400, 14405, 14410, 14415, 14420, 14425, 14430,
            14435, 14440, 14445, 14446, 14451, 14456, 14461, 14466, 14471, 14476, 14481, 14486,
            14491, 14492, 14497, 14502, 14507, 14512, 14517, 14522, 14527, 14532, 14537, 14538,
            14543, 14551, 14554, 14557, 14559, 14564, 14572, 14577, 14579, 14584, 14589, 14593,
            14596, 14599, 14601, 14606, 14614, 14619, 14621, 14626, 14634, 14637, 14640, 14642,
            14647, 14652, 14657, 14662, 14667, 14672, 14677, 14678, 14683, 14688, 14693, 14698,
            14703, 14708, 14713, 14714, 14719, 14724, 14729, 14734, 14739, 14744, 14749, 14750,
            14755, 14760, 14765, 14770, 14775, 14780, 14785, 14786, 14791, 14796, 14801, 14806,
            14811, 14816, 14821, 14822, 14827, 14832, 14837, 14842, 14847, 14852, 14857, 14858,
            14863, 14868, 14873, 14878, 14883, 14888, 14893, 14894, 14899, 14904, 14909, 14914,
            14919, 14924, 14929, 14930, 14935, 14940, 14945, 14950, 14955, 14960, 14965, 14966,
            14971, 14976, 14981, 14986, 14991, 14996, 15001, 15002, 15007, 15012, 15017, 15022,
            15027, 15032, 15037, 15038, 15043, 15048, 15053, 15058, 15063, 15068, 15073, 15074,
            15079, 15084, 15089, 15094, 15099, 15104, 15109, 15110, 15115, 15120, 15125, 15130,
            15135, 15140, 15145, 15146, 15151, 15156, 15161, 15166, 15171, 15176, 15181, 15182,
            15187, 15192, 15197, 15202, 15207, 15212, 15217, 15218, 15223, 15228, 15233, 15238,
            15243, 15248, 15253, 15254, 15259, 15264, 15269, 15274, 15279, 15284, 15289, 15290,
            15295, 15300, 15305, 15310, 15315, 15320, 15325, 15326, 15331, 15336, 15341, 15346,
            15351, 15356, 15361, 15362, 15367, 15372, 15377, 15382, 15387, 15392, 15397, 15398,
            15403, 15408, 15413, 15418, 15423, 15428, 15433, 15434, 15439, 15444, 15449, 15454,
            15459, 15464, 15469, 15470, 15475, 15480, 15485, 15490, 15495, 15500, 15505, 15506,
            15511, 15516, 15521, 15526, 15531, 15536, 15541, 15542, 15553, 15557, 15564, 15567,
            15570, 15572, 15583, 15587, 15594, 15597, 15600, 15602, 15613, 15617, 15624, 15627,
            15630, 15632, 15643, 15647, 15654, 15657, 15660, 15662, 15673, 15677, 15684, 15687,
            15690, 15692, 15696, 15701, 15706, 15711, 15716, 15721, 15726, 15731, 15736, 15741,
            15742, 15746, 15751, 15756, 15761, 15766, 15771, 15776, 15781, 15786, 15791, 15792,
            15796, 15801, 15806, 15811, 15816, 15821, 15826, 15831, 15836, 15841, 15842, 15846,
            15851, 15856, 15861, 15866, 15871, 15876, 15881, 15886, 15891, 15892, 15896, 15901,
            15906, 15911, 15916, 15921, 15926, 15931, 15936, 15941, 15942, 15946, 15951, 15956,
            15961, 15966, 15971, 15976, 15981, 15986, 15991, 15992, 15996, 16001, 16006, 16011,
            16016, 16021, 16026, 16031, 16036, 16041, 16042, 16046, 16051, 16056, 16061, 16066,
            16071, 16076, 16081, 16086, 16091, 16092, 16096, 16101, 16106, 16111, 16116, 16121,
            16126, 16131, 16136, 16141, 16142, 16146, 16151, 16156, 16161, 16166, 16171, 16176,
            16181, 16186, 16191, 16192, 16196, 16201, 16206, 16211, 16216, 16221, 16226, 16231,
            16236, 16241, 16242, 16246, 16251, 16256, 16261, 16266, 16271, 16276, 16281, 16286,
            16291, 16292, 16296, 16301, 16306, 16311, 16316, 16321, 16326, 16331, 16336, 16341,
            16342, 16346, 16351, 16356, 16361, 16366, 16371, 16376, 16381, 16386, 16391, 16392,
            16396, 16401, 16406, 16411, 16416, 16421, 16426, 16431, 16436, 16441, 16442, 16446,
            16451, 16456, 16461, 16466, 16471, 16476, 16481, 16486, 16491, 16492, 16496, 16501,
            16506, 16511, 16516, 16521, 16526, 16531, 16536, 16541, 16542, 16546, 16551, 16556,
            16561, 16566, 16571, 16576, 16581, 16586, 16591, 16592, 16596, 16601, 16606, 16611,
            16616, 16621, 16626, 16631, 16636, 16641, 16642, 16646, 16651, 16656, 16661, 16666,
            16671, 16676, 16681, 16686, 16691, 16692, 16696, 16701, 16706, 16711, 16716, 16721,
            16726, 16731, 16736, 16741, 16742, 16746, 16751, 16756, 16761, 16766, 16771, 16776,
            16781, 16786, 16791, 16792, 16796, 16801, 16806, 16811, 16816, 16821, 16826, 16831,
            16836, 16841, 16842, 16846, 16851, 16856, 16861, 16866, 16871, 16876, 16881, 16886,
            16891, 16892, 16896, 16901, 16906, 16911, 16916, 16921, 16926, 16931, 16936, 16941,
            16942, 16946, 16951, 16956, 16961, 16966, 16971, 16976, 16981, 16986, 16991, 16992,
            16996, 17001, 17006, 17011, 17016, 17021, 17026, 17031, 17036, 17041, 17042, 17046,
            17051, 17056, 17061, 17066, 17071, 17076, 17081, 17086, 17091, 17092, 17096, 17101,
            17106, 17111, 17116, 17121, 17126, 17131, 17136, 17141, 17142, 17146, 17151, 17156,
            17161, 17166, 17171, 17176, 17181, 17186, 17191, 17192, 17196, 17201, 17206, 17211,
            17216, 17221, 17226, 17231, 17236, 17241, 17242, 17246, 17251, 17256, 17261, 17266,
            17271, 17276, 17281, 17286, 17291, 17292, 17296, 17301, 17306, 17311, 17316, 17321,
            17326, 17331, 17336, 17341, 17342, 17346, 17351, 17356, 17361, 17366, 17371, 17376,
            17381, 17386, 17391, 17392, 17396, 17401, 17406, 17411, 17416, 17421, 17426, 17431,
            17436, 17441, 17442, 17447, 17452, 17456, 17458, 17461, 17463, 17468, 17473, 17477,
            17480, 17483, 17485, 17490, 17495, 17499, 17502, 17505, 17507, 17512, 17517, 17521,
            17523, 17526, 17528, 17533, 17538, 17542, 17544, 17547, 17549, 17554, 17559, 17564,
            17569, 17574, 17579, 17584, 17585, 17590, 17595, 17600, 17605, 17610, 17615, 17620,
            17621, 17626, 17631, 17636, 17641, 17646, 17651, 17656, 17657, 17662, 17667, 17672,
            17677, 17682, 17687, 17692, 17693, 17698, 17703, 17708, 17713, 17718, 17723, 17728,
            17729, 17734, 17739, 17744, 17749, 17754, 17759, 17764, 17765, 17770, 17775, 17780,
            17785, 17790, 17795, 17800, 17801, 17806, 17811, 17816, 17821, 17826, 17831, 17836,
            17837, 17842, 17847, 17852, 17857, 17862, 17867, 17872, 17873, 17878, 17883, 17888,
            17893, 17898, 17903, 17908, 17909, 17914, 17919, 17924, 17929, 17934, 17939, 17944,
            17945, 17950, 17955, 17960, 17965, 17970, 17975, 17980, 17981, 17986, 17991, 17996,
            18001, 18006, 18011, 18016, 18017, 18022, 18027, 18032, 18037, 18042, 18047, 18052,
            18053, 18058, 18063, 18068, 18073, 18078, 18083, 18088, 18089, 18095, 18100, 18105,
            18110, 18115, 18120, 18125, 18126, 18131, 18136, 18141, 18146, 18151, 18157, 18162,
            18163, 18168, 18173, 18178, 18183, 18188, 18193, 18198, 18199, 18204, 18209, 18214,
            18219, 18224, 18229, 18234, 18235, 18240, 18245, 18250, 18255, 18260, 18265, 18270,
            18271, 18276, 18280, 18284, 18291, 18294, 18297, 18299, 18305, 18309, 18313, 18320,
            18323, 18326, 18328, 18333, 18337, 18341, 18348, 18351, 18354, 18356, 18361, 18365,
            18369, 18376, 18379, 18382, 18384, 18389, 18393, 18397, 18404, 18407, 18410, 18412,
            18417, 18422, 18427, 18432, 18437, 18442, 18447, 18452, 18457, 18458, 18463, 18468,
            18473, 18478, 18483, 18488, 18493, 18498, 18503, 18504, 18509, 18514, 18519, 18524,
            18529, 18534, 18539, 18544, 18549, 18550, 18555, 18560, 18565, 18570, 18575, 18580,
            18585, 18590, 18595, 18596, 18601, 18606, 18611, 18616, 18621, 18626, 18631, 18636,
            18641, 18642, 18647, 18652, 18657, 18662, 18667, 18672, 18677, 18682, 18687, 18688,
            18693, 18698, 18703, 18708, 18713, 18718, 18723, 18728, 18733, 18734, 18739, 18744,
            18749, 18754, 18759, 18764, 18769, 18774, 18779, 18780, 18785, 18790, 18795, 18800,
            18805, 18810, 18815, 18820, 18825, 18826, 18831, 18836, 18841, 18846, 18851, 18856,
            18861, 18866, 18871, 18872, 18877, 18882, 18887, 18892, 18897, 18902, 18907, 18912,
            18917, 18918, 18923, 18928, 18933, 18938, 18943, 18948, 18953, 18958, 18963, 18964,
            18969, 18974, 18979, 18984, 18989, 18994, 18999, 19004, 19009, 19010, 19015, 19020,
            19025, 19030, 19035, 19040, 19045, 19050, 19055, 19056, 19061, 19066, 19071, 19076,
            19081, 19086, 19091, 19096, 19101, 19102, 19107, 19112, 19117, 19122, 19127, 19132,
            19137, 19142, 19147, 19148, 19153, 19158, 19163, 19168, 19173, 19178, 19183, 19188,
            19193, 19194, 19199, 19204, 19209, 19214, 19219, 19224, 19229, 19234, 19239, 19240,
            19245, 19250, 19255, 19260, 19265, 19270, 19275, 19280, 19285, 19286, 19291, 19296,
            19301, 19306, 19311, 19316, 19321, 19326, 19331, 19332, 19337, 19342, 19346, 19349,
            19352, 19354, 19359, 19364, 19368, 19371, 19374, 19376, 19381, 19386, 19390, 19393,
            19396, 19398, 19403, 19408, 19412, 19415, 19418, 19420, 19425, 19430, 19434, 19437,
            19440, 19442, 19447, 19452, 19457, 19462, 19467, 19472, 19477, 19478, 19483, 19488,
            19493, 19498, 19503, 19508, 19513, 19514, 19519, 19524, 19529, 19534, 19539, 19544,
            19549, 19550, 19555, 19560, 19565, 19570, 19575, 19580, 19585, 19586, 19591, 19596,
            19601, 19606, 19611, 19616, 19621, 19622, 19627, 19632, 19637, 19642, 19647, 19652,
            19657, 19658, 19663, 19668, 19673, 19678, 19683, 19688, 19693, 19694, 19699, 19704,
            19709, 19714, 19719, 19724, 19729, 19730, 19735, 19740, 19745, 19750, 19755, 19760,
            19765, 19766, 19771, 19776, 19781, 19786, 19791, 19796, 19801, 19802, 19807, 19812,
            19817, 19822, 19827, 19832, 19837, 19838, 19843, 19848, 19853, 19858, 19863, 19868,
            19873, 19874, 19879, 19884, 19889, 19894, 19899, 19904, 19909, 19910, 19915, 19920,
            19925, 19930, 19935, 19940, 19945, 19946, 19951, 19956, 19961, 19966, 19971, 19976,
            19981, 19982, 19987, 19992, 19997, 20002, 20007, 20012, 20017, 20018, 20023, 20028,
            20033, 20038, 20043, 20048, 20053, 20054, 20059, 20064, 20069, 20074, 20079, 20084,
            20089, 20090, 20095, 20100, 20105, 20110, 20115, 20120, 20125, 20126, 20131, 20136,
            20141, 20146, 20151, 20156, 20161, 20162, 20167, 20172, 20177, 20182, 20183, 20188,
            20193, 20198, 20203, 20204, 20209, 20214, 20219, 20224, 20225, 20230, 20235, 20240,
            20245, 20246, 20251, 20256, 20261, 20266, 21516, 21517, 21518, 21519, 21520, 21521,
            21522, 21523, 21524, 21525, 21526, 21527, 21528, 21529, 21530, 21531, 21532, 21533,
            21534, 21535, 23199, 23200, 23201, 23202, 23203, 23204, 23205, 23206, 23207, 23208,
            23209, 23210, 23211, 23212, 23213, 23214, 23215, 23216, 23217, 23218, 23219, 23220,
            23221, 23222, 23223, 23224, 23225, 23226, 23227, 23228, 23229, 23230, 23231, 23232,
            23233, 23234, 23235, 23236, 23237, 23238, 23239, 23240, 23241, 23242, 23243, 23244,
            23245, 23246, 23247, 23248, 23249, 23250, 23251, 23252, 23253, 23254, 23255, 23256,
            23257, 23258, 23259, 23260, 23261, 23262, 23263, 23264, 23265, 23266, 23267, 23268,
            23269, 23270, 23271, 23272, 23273, 23274, 23275, 23276, 23277, 23278, 23279, 23280,
            23281, 23282, 23283, 23284, 23285, 23286, 23287, 23288, 23289, 23290, 23291, 23292,
            23293, 23294, 23295, 23296, 23297, 23298, 23299, 23300, 23301, 23302, 23303, 23304,
            23305, 23306, 23307, 23308, 23309, 23310, 23311, 23312, 23313, 23314, 23315, 23316,
            23317, 23318, 23319, 23320, 23321, 23322, 23323, 23324, 23325, 23326, 23327, 23328,
            23329, 23330, 23331, 23332, 23333, 23334, 23335, 23336, 23337, 23338, 23339, 23340,
            23341, 23342, 23343, 23344, 23345, 23346, 23347, 23348, 23349, 23350, 23351, 23352,
            23353, 23354, 23355, 23356, 23357, 23358, 23359, 23360, 23361, 23362, 23363, 23364,
            23365, 23366, 23367, 23368, 23369, 23370, 23371, 23372, 23373, 23374, 23375, 23376,
            23377, 23378, 23379, 23380, 23381, 23382, 23383, 23384, 23385, 23386, 23387, 23388,
            23389, 23390, 23391, 23392, 23393, 23394, 23395, 23396, 23397, 23398, 23399, 23400,
            23401, 23402, 23403, 23404, 23405, 23406, 23407, 23408, 23409, 23410, 23411, 23412,
            23413, 23414, 23415, 23416, 23417, 23418, 23419, 23420, 23421, 23422, 23423, 23424,
            23425, 23426, 23427, 23428, 23429, 23430, 23431, 23432, 23433, 23434, 23435, 23436,
            23437, 23438, 23439, 23440, 23441, 23442, 23443, 23444, 23445, 23446, 23447, 23448,
            23449, 23450, 23451, 23452, 23453, 23454, 23455, 23456, 23457, 23458, 23459, 23460,
            23461, 23462, 23463, 23464, 23465, 23466, 23467, 23468, 23469, 23470, 23471, 23472,
            23473, 23474, 23475, 23476, 23477, 23478, 23479, 23480, 23481, 23482, 23483, 23484,
            23485, 23486, 23487, 23488, 23489, 23490, 23491, 23492, 23493, 23494, 23495, 23496,
            23497, 23498, 23499, 23500, 23501, 23502, 23503, 23504, 23505, 23506, 23507, 23508,
            23509, 23510, 23511, 23512, 23513, 23514, 23515, 23516, 23517, 23518, 23519, 23520,
            23521, 23522, 23523, 23524, 23525, 23526, 23527, 23528, 23529, 23530, 23531, 23532,
            23533, 23534, 23535, 23536, 23537, 23538, 23539, 23540, 23541, 23542, 23543, 23544,
            23545, 23546, 23547, 23548, 23549, 23550, 23551, 23552, 23553, 23554, 23555, 23556,
            23557, 23558, 23559, 23560, 23561, 23562, 23563, 23568, 23569, 23570, 23571, 23572,
            23573, 23574, 23575, 23576, 23577, 23578, 23579, 23580, 23581, 23582, 23583, 23584,
            23585, 23586, 23587, 23588, 23589, 23590, 23591, 23592, 23593, 23594, 23595, 23596,
            23597, 23598, 23599, 23600, 23601, 23602, 23603, 23604, 23605, 23606, 23607, 23608,
            23609, 23610, 23611, 23612, 23613, 23614, 23615, 23616, 23617, 23618, 23619, 23620,
            23621, 23622, 23623, 23624, 23625, 23626, 23627, 23628, 23629, 23630, 23631, 23632,
            23633, 23634, 23635, 23636, 23637, 23638, 23639, 23640, 23641, 23642, 23643, 23644,
            23645, 23646, 23647, 23648, 23649, 23650, 23651, 23652, 23653, 23654, 23655, 23656,
            23657, 23658, 23659, 23660, 23661, 23662, 23663, 23664, 23665, 23666, 23667, 23668,
            23669, 23670, 23671, 23672, 23673, 23674, 23675, 23676, 23677, 23678, 23679, 23680,
            23681, 23682, 23683, 23684, 23685, 23686, 23687, 23688, 23689, 23690, 23691, 23692,
            23693, 23694, 23695, 23696, 23697, 23698, 23699, 23700, 23701, 23702, 23703, 23704,
            23705, 23706, 23707, 23708, 23709, 23710, 23711, 23712, 23713, 23714, 23715, 23716,
            23717, 23718, 23719, 23720, 23721, 23722, 23723, 23724, 23725, 23726, 23727, 23728,
            23729, 23730, 23731, 23732, 23733, 23734, 23735, 23736, 23737, 23738, 23739, 23740,
            23741, 23742, 23743, 23744, 23745, 23746, 23747, 23748, 23749, 23750, 23751, 23752,
            23753, 23754, 23755, 23756, 23757, 23758, 23759, 23760, 23761, 23762, 23763, 23764,
            23765, 23766, 23767, 23768, 23769, 23770, 23771, 23772, 23773, 23774, 23775, 23776,
            23777, 23778, 23779, 23780, 23781, 23782, 23783, 23784, 23785, 23786, 23787, 23788,
            23789, 23790, 23791, 23792, 23793, 23794, 23795, 23796, 23797, 23798, 23799, 23800,
            23801, 23802, 23803, 23804, 23805, 23806, 23807, 23808, 23809, 23810, 23811, 23812,
            23813, 23814, 23815, 23816, 23817, 23818, 23819, 23820, 23821, 23822, 23823, 23824,
            23825, 23826, 23827, 23828, 23829, 23830, 23831, 23832, 23833, 23834, 23835, 23836,
            23837, 23838, 23839, 23840, 23841, 23842, 23843, 23844, 23845, 23846, 23847, 23848,
            23849, 23850, 23851, 23852, 23853, 23854, 23855, 23856, 23857, 23858, 23859, 23860,
            23861, 23862, 23863, 23864, 23865, 23866, 23867, 23868, 23869, 23870, 23871, 23872,
            23873, 23874, 23875, 23876, 23877, 23878, 23879, 23880, 23881, 23882, 25109, 25110,
            25111, 25112, 25113, 25114, 25115, 25116, 25117, 25118, 25119, 25120, 25121, 25122,
            25123, 25124, 25125, 25126, 25127, 25128, 25129, 25130, 25131, 25132, 25133, 25134,
            25135, 25136, 25137, 25138, 25139, 25140, 25141, 25142, 25143, 25144, 25145, 25146,
            25147, 25148, 25149, 25150, 25151, 25152, 25153, 25154, 25155, 25156, 25157, 25158,
            25159, 25160, 25161, 25162, 25163, 25164, 25165, 25166, 25167, 25168, 25169, 25170,
            25171, 25172, 25173, 25174, 25175, 25176, 25177, 25178, 25179, 25180, 25181, 25182,
            25183, 25184, 25185, 25186, 25187, 25188, 25189, 25190, 25191, 25192, 25193, 25194,
            25195, 25196, 25197, 25198, 25199, 25200, 25201, 25202, 25203, 25204, 25205, 25206,
            25207, 25208, 25209, 25210, 25211, 25212, 25213, 25214, 25215, 25216, 25217, 25218,
            25219, 25220, 25221, 25222, 25223, 25224, 25225, 25226, 25227, 25228, 25653, 25654,
            25655, 25656, 25657, 20000018, 20000019, 20000021,
        };

        private static readonly HashSet<uint> Extra1 = new HashSet<uint>
        {
            25330, 25334, 25335, 25336, 25337, 25338, 25339, 25340, 25341, 25342, 25343, 25344,
        };

        private static readonly HashSet<uint> Extra2 = new HashSet<uint>
        {
            25330, 25334, 25335, 25336, 25337, 25338, 25339, 25340, 25341, 25342, 25343, 25344,
            30576, 30577, 30578, 30579, 30580,
        };

        private static readonly HashSet<uint> Extra3 = new HashSet<uint>
        {
            25330, 25334, 25335, 25336, 25337, 25338, 25339, 25340, 25341, 25342, 25343, 25344,
            30576, 30577, 30578, 30579, 30580, 30581, 30582, 30583, 30584, 30585,
        };

        private static readonly HashSet<uint> Extra4 = new HashSet<uint>
        {
            25330, 25334, 25335, 25336, 25337, 25338, 25339, 25340, 25341, 25342, 25343, 25344,
            30576, 30577, 30578, 30579, 30580, 30581, 30582, 30583, 30584, 10000070,
        };

        private static readonly HashSet<uint> Only1 = new HashSet<uint>
        {
            29214, 29216, 29217,
        };

        private static readonly HashSet<uint> None = new HashSet<uint>();

        /// <summary>Paint class to what it may colour: the shared armour classes or not, and its own.</summary>
        private static readonly Dictionary<uint, (bool Armor, HashSet<uint> Own)> Restrictions = new Dictionary<uint, (bool, HashSet<uint>)>
        {
            [3600] = (true, None),
            [3741] = (true, None),
            [3742] = (true, None),
            [3743] = (true, None),
            [3744] = (true, None),
            [3745] = (true, None),
            [3746] = (true, None),
            [9587] = (true, None),
            [24611] = (true, Extra3),
            [24612] = (true, Extra2),
            [24613] = (true, Extra2),
            [24614] = (true, Extra3),
            [24615] = (true, Extra3),
            [24616] = (true, Extra3),
            [24617] = (true, Extra3),
            [24618] = (true, Extra3),
            [24619] = (true, Extra4),
            [24620] = (true, Extra3),
            [24621] = (true, Extra3),
            [24622] = (true, Extra3),
            [24623] = (true, Extra3),
            [24624] = (true, Extra3),
            [24625] = (true, Extra2),
            [24626] = (true, Extra2),
            [24627] = (true, Extra2),
            [24628] = (true, Extra2),
            [24629] = (true, Extra2),
            [24630] = (true, Extra2),
            [24631] = (true, Extra2),
            [24632] = (true, Extra2),
            [24633] = (true, Extra2),
            [24634] = (true, Extra2),
            [24635] = (true, Extra2),
            [24636] = (true, Extra2),
            [24637] = (true, Extra2),
            [24638] = (true, Extra2),
            [24639] = (true, Extra2),
            [24640] = (true, Extra2),
            [24641] = (true, Extra2),
            [24642] = (true, Extra2),
            [24643] = (true, Extra2),
            [24644] = (true, Extra2),
            [24645] = (true, Extra2),
            [24646] = (true, Extra2),
            [24647] = (true, Extra2),
            [24648] = (true, Extra2),
            [24649] = (true, Extra2),
            [24650] = (true, Extra2),
            [24651] = (true, Extra2),
            [24652] = (true, Extra2),
            [24653] = (true, Extra2),
            [24654] = (true, Extra2),
            [26380] = (true, Extra1),
            [26381] = (true, Extra1),
            [26382] = (true, Extra1),
            [26383] = (true, Extra1),
            [29218] = (false, Only1),
        };

        public static CustomizationType? TypeOf(uint classId) =>
            Types.TryGetValue(classId, out var type) ? type : (CustomizationType?)null;

        /// <summary>Whether the customization item may be used on an item of the target class.</summary>
        public static bool MayApply(uint customizationClassId, uint targetClassId)
        {
            if (!Restrictions.TryGetValue(customizationClassId, out var allowed))
                return true;

            return (allowed.Armor && Armor.Contains(targetClassId)) || allowed.Own.Contains(targetClassId);
        }

        /// <summary>
        /// The swatch of this paint the colour asked for is: the nearest of its 25 with every
        /// channel within <see cref="HueTolerance"/>, as that swatch's own packed colour.
        /// </summary>
        public static bool TryMatchHue(uint customizationClassId, Color asked, out uint hue) =>
            TryMatch(Hues, HueTolerance, customizationClassId, asked, out hue);

        /// <summary>
        /// Whether the colour asked for is one of this hair or skin colour item's palette: every
        /// channel within <see cref="PaletteTolerance"/> of one of its colours. The colour kept
        /// is the one asked for, which is the one the window showed on the player.
        /// </summary>
        public static bool IsOnPalette(uint customizationClassId, Color asked) =>
            TryMatch(Palettes, PaletteTolerance, customizationClassId, asked, out _);

        /// <summary>The hair or face this modification item offers under the item template picked.</summary>
        public static bool TryGetChoice(uint customizationClassId, uint templateId, out uint classId)
        {
            classId = 0;

            if (templateId == 0 || !Choices.TryGetValue(customizationClassId, out var choices))
                return false;

            foreach (var choice in choices)
                if (choice.TemplateId == templateId)
                {
                    classId = choice.ClassId;
                    return true;
                }

            return false;
        }

        private static bool TryMatch(IReadOnlyDictionary<uint, uint[]> offered, int tolerance, uint customizationClassId, Color asked, out uint hue)
        {
            hue = 0;

            if (asked == null || !offered.TryGetValue(customizationClassId, out var choices))
                return false;

            var best = int.MaxValue;

            foreach (var choice in choices)
            {
                var swatch = new Color(choice);
                var red = Math.Abs(swatch.Red - asked.Red);
                var green = Math.Abs(swatch.Green - asked.Green);
                var blue = Math.Abs(swatch.Blue - asked.Blue);

                if (red > tolerance || green > tolerance || blue > tolerance || red + green + blue >= best)
                    continue;

                best = red + green + blue;
                hue = choice;
            }

            return best != int.MaxValue;
        }
    }
}
