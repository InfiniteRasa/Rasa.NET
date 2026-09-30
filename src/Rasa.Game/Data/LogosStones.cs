using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// The Logos stone items and the Logos each one teaches: generated.client.logosstone.classlookup,
    /// item entity class to Logos stone id - the id LogosStoneAdded, the Tabula, the shrines
    /// (LogosEntry) and logosstonelanguage all use. 361 items, one for each Logos.
    ///
    /// ItemElohLogosAppearance (26835), ...Function (26836) and ...Lightness (26837) are in the
    /// client's table but their classes lack the LOGOSSTONE augmentation (54), so the client never
    /// offers them for use; they are listed all the same, the table being the client's.
    /// </summary>
    public static class LogosStones
    {
        public static readonly IReadOnlyDictionary<uint, uint> LogosOfClass = new Dictionary<uint, uint>
        {
            { 7376, 1 },          // ItemElohLogosArea, AREA
            { 7474, 2 },          // ItemElohLogosAttack, ATTACK
            { 7377, 3 },          // ItemElohLogosBackward, BACKWARD
            { 7460, 4 },          // ItemElohLogosChaos, CHAOS
            { 7505, 5 },          // ItemElohLogosCloud, CLOUD
            { 7378, 6 },          // ItemElohLogosDamage, DAMAGE
            { 7475, 7 },          // ItemElohLogosDefend, DEFEND
            { 7436, 8 },          // ItemElohLogosEnd, END
            { 7437, 9 },          // ItemElohLogosEnemy, ENEMY
            { 7438, 10 },         // ItemElohLogosEnhance, ENHANCE
            { 7440, 12 },         // ItemElohLogosFear, FEAR
            { 7441, 13 },         // ItemElohLogosFeeling, FEELING
            { 7457, 14 },         // ItemElohLogosFriend, FRIEND
            { 7442, 15 },         // ItemElohLogosGive, GIVE
            { 7443, 16 },         // ItemElohLogosHarmony, HARMONY
            { 7444, 17 },         // ItemElohLogosHeal, HEAL
            { 7445, 18 },         // ItemElohLogosIncrease, INCREASE
            { 7446, 19 },         // ItemElohLogosJourney, JOURNEY
            { 7447, 20 },         // ItemElohLogosMany, MANY
            { 7448, 21 },         // ItemElohLogosMixture, MIXTURE
            { 7449, 22 },         // ItemElohLogosPoison, POISON
            { 7451, 23 },         // ItemElohLogosPower, POWER
            { 7452, 24 },         // ItemElohLogosProjectile, PROJECTILE
            { 7453, 25 },         // ItemElohLogosReturn, RETURN
            { 7454, 26 },         // ItemElohLogosSecret, SECRET
            { 7455, 27 },         // ItemElohLogosTake, TAKE
            { 7461, 28 },         // ItemElohLogosTime, TIME
            { 7456, 29 },         // ItemElohLogosWind, WIND
            { 10385, 30 },        // ItemElohLogosDisperse, DISPERSE
            { 10386, 31 },        // ItemElohLogosLightning, LIGHTNING
            { 10387, 32 },        // ItemElohLogosCreate, CREATE
            { 10388, 33 },        // ItemElohLogosNegative, NEGATIVE
            { 10389, 34 },        // ItemElohLogosMachine, MACHINE
            { 10391, 35 },        // ItemElohLogosWeave, WEAVE
            { 10393, 36 },        // ItemElohLogosEnclosure, ENCLOSURE
            { 10394, 37 },        // ItemElohLogosCorpse, CORPSE
            { 10395, 38 },        // ItemElohLogosSelf, SELF_I_ME
            { 10396, 39 },        // ItemElohLogosSeparate, SEPARATE
            { 10397, 40 },        // ItemElohLogosPlant, PLANT
            { 10398, 41 },        // ItemElohLogosControl, CONTROL
            { 10399, 42 },        // ItemElohLogosManifest, MANIFEST
            { 10400, 43 },        // ItemElohLogosLife, LIFE
            { 10401, 44 },        // ItemElohLogosBomb, BOMB
            { 10402, 45 },        // ItemElohLogosAround, AROUND
            { 10403, 46 },        // ItemElohLogosGround, GROUND
            { 10404, 47 },        // ItemElohLogosForward, FORWARD
            { 10405, 48 },        // ItemElohLogosVortex, VORTEX
            { 11196, 49 },        // ItemElohLogosTarget, TARGET
            { 11198, 50 },        // ItemElohLogosMovement, MOVEMENT
            { 11199, 51 },        // ItemElohLogosCommunication, COMMUNICATION
            { 11200, 52 },        // ItemElohLogosSummon, SUMMON
            { 11201, 53 },        // ItemElohLogosHere, HERE
            { 11202, 54 },        // ItemElohLogosSpeed, SPEED
            { 11203, 55 },        // ItemElohLogosTrap, TRAP
            { 11204, 56 },        // ItemElohLogosMind, MIND
            { 22841, 57 },        // ItemElohLogosPast, PAST
            { 22753, 58 },        // ItemElohLogosFuture, FUTURE
            { 22711, 63 },        // ItemElohLogosDistance, DISTANCE
            { 22741, 64 },        // ItemElohLogosFar, FAR
            { 22795, 65 },        // ItemElohLogosLogos, LOGOS
            { 22728, 66 },        // ItemElohLogosEnlighten, ENLIGHTEN
            { 22763, 67 },        // ItemElohLogosHand, HAND
            { 22883, 68 },        // ItemElohLogosStar, STAR
            { 22645, 70 },        // ItemElohLogosAir, AIR
            { 22646, 71 },        // ItemElohLogosAll_of_yours, ALL_OF_YOURS
            { 22647, 72 },        // ItemElohLogosAncestor, ANCESTOR
            { 22648, 73 },        // ItemElohLogosAnd, AND
            { 22649, 74 },        // ItemElohLogosApplaud, APPLAUD
            { 22650, 75 },        // ItemElohLogosArrive, ARRIVE
            { 22651, 76 },        // ItemElohLogosAsleep, ASLEEP
            { 22652, 77 },        // ItemElohLogosAt, AT
            { 22653, 78 },        // ItemElohLogosAwake, AWAKE
            { 22654, 79 },        // ItemElohLogosBaby, BABY
            { 22655, 80 },        // ItemElohLogosBabyboy, BABYBOY
            { 22656, 81 },        // ItemElohLogosBabygirl, BABYGIRL
            { 22657, 82 },        // ItemElohLogosBalanced, BALANCED
            { 22658, 83 },        // ItemElohLogosBank, BANK
            { 22659, 84 },        // ItemElohLogosBecause, BECAUSE
            { 22660, 85 },        // ItemElohLogosBefore, BEFORE
            { 22661, 86 },        // ItemElohLogosBegin, BEGIN
            { 22662, 87 },        // ItemElohLogosBehind, BEHIND
            { 22663, 88 },        // ItemElohLogosBeing, BEING
            { 22664, 89 },        // ItemElohLogosBelieve, BELIEVE
            { 22665, 90 },        // ItemElohLogosBeside, BESIDE
            { 22666, 91 },        // ItemElohLogosBig, BIG
            { 22667, 92 },        // ItemElohLogosBlind, BLIND
            { 22668, 93 },        // ItemElohLogosBody, BODY
            { 22669, 94 },        // ItemElohLogosBoo, BOO
            { 22670, 95 },        // ItemElohLogosBook, BOOK
            { 22671, 96 },        // ItemElohLogosBoundary, BOUNDARY
            { 22672, 97 },        // ItemElohLogosBoy, BOY
            { 22673, 98 },        // ItemElohLogosBreak, BREAK
            { 22674, 99 },        // ItemElohLogosBridge, BRIDGE
            { 22675, 100 },       // ItemElohLogosBuff, BUFF
            { 22676, 101 },       // ItemElohLogosBuild, BUILD
            { 22677, 102 },       // ItemElohLogosBut, BUT
            { 22678, 103 },       // ItemElohLogosBuy, BUY
            { 22679, 104 },       // ItemElohLogosChild, CHILD
            { 22680, 105 },       // ItemElohLogosChoice, CHOICE
            { 22681, 106 },       // ItemElohLogosCivilization, CIVILIZATION
            { 22682, 107 },       // ItemElohLogosClarity, CLARITY
            { 22683, 108 },       // ItemElohLogosClose, CLOSE
            { 22684, 109 },       // ItemElohLogosColliseum, COLLISEUM
            { 22685, 110 },       // ItemElohLogosComet, COMET
            { 22686, 111 },       // ItemElohLogosComing, COMING
            { 22687, 112 },       // ItemElohLogosCommunicate, COMMUNICATE
            { 22688, 113 },       // ItemElohLogosConflict, CONFLICT
            { 22689, 114 },       // ItemElohLogosConfront, CONFRONT
            { 22690, 115 },       // ItemElohLogosConfusion, CONFUSION
            { 22691, 116 },       // ItemElohLogosConstellation, CONSTELLATION
            { 22692, 117 },       // ItemElohLogosContainer, CONTAINER
            { 22693, 118 },       // ItemElohLogosCosmos, COSMOS
            { 22694, 119 },       // ItemElohLogosCraftworks, CRAFTWORKS
            { 22696, 121 },       // ItemElohLogosCrop, CROP
            { 22697, 122 },       // ItemElohLogosCrush, CRUSH
            { 22698, 123 },       // ItemElohLogosCry, CRY
            { 22699, 124 },       // ItemElohLogosDay, DAY
            { 22700, 125 },       // ItemElohLogosDeath, DEATH
            { 22701, 126 },       // ItemElohLogosDebuff, DEBUFF
            { 22702, 127 },       // ItemElohLogosDecrease, DECREASE
            { 22703, 128 },       // ItemElohLogosDefeat, DEFEAT
            { 22704, 129 },       // ItemElohLogosDepart, DEPART
            { 22705, 130 },       // ItemElohLogosDescendent, DESCENDENT
            { 22706, 131 },       // ItemElohLogosDestruction, DESTRUCTION
            { 22707, 132 },       // ItemElohLogosDiscover, DISCOVER
            { 22708, 133 },       // ItemElohLogosDishonor, DISHONOR
            { 22709, 134 },       // ItemElohLogosDislike, DISLIKE
            { 22710, 135 },       // ItemElohLogosDispleased, DISPLEASED
            { 22712, 136 },       // ItemElohLogosDivide, DIVIDE
            { 22713, 137 },       // ItemElohLogosDivision, DIVISION
            { 22714, 138 },       // ItemElohLogosDominate, DOMINATE
            { 22715, 139 },       // ItemElohLogosDoor, DOOR
            { 22716, 140 },       // ItemElohLogosDown, DOWN
            { 22717, 141 },       // ItemElohLogosDrop, DROP
            { 22718, 142 },       // ItemElohLogosDroplet, DROPLET
            { 22719, 143 },       // ItemElohLogosEar, EAR
            { 22720, 144 },       // ItemElohLogosEast, EAST
            { 22721, 145 },       // ItemElohLogosEating, EATING
            { 22722, 146 },       // ItemElohLogosEffect, EFFECT
            { 22723, 147 },       // ItemElohLogosEither, EITHER
            { 22724, 148 },       // ItemElohLogosEloh, ELOH
            { 22725, 149 },       // ItemElohLogosEmote, EMOTE
            { 22726, 150 },       // ItemElohLogosEmpower, EMPOWER
            { 22727, 151 },       // ItemElohLogosEnlarging, ENLARGING
            { 22729, 152 },       // ItemElohLogosEntrance, ENTRANCE
            { 22730, 153 },       // ItemElohLogosEternal, ETERNAL
            { 22732, 155 },       // ItemElohLogosEvery, EVERY
            { 22733, 156 },       // ItemElohLogosEveryones, EVERYONES
            { 22734, 157 },       // ItemElohLogosEvil, EVIL
            { 22735, 158 },       // ItemElohLogosEvolved, EVOLVED
            { 22736, 159 },       // ItemElohLogosExamine, EXAMINE
            { 22737, 160 },       // ItemElohLogosExchange, EXCHANGE
            { 22738, 161 },       // ItemElohLogosExit, EXIT
            { 22739, 162 },       // ItemElohLogosEye, EYE
            { 22740, 163 },       // ItemElohLogosFall, FALL
            { 22742, 164 },       // ItemElohLogosFarewell, FAREWELL
            { 22743, 165 },       // ItemElohLogosFarm, FARM
            { 22744, 166 },       // ItemElohLogosFather, FATHER
            { 22745, 167 },       // ItemElohLogosFew, FEW
            { 22746, 168 },       // ItemElohLogosFinger, FINGER
            { 22747, 169 },       // ItemElohLogosFire, FIRE
            { 22748, 170 },       // ItemElohLogosFlower, FLOWER
            { 22749, 171 },       // ItemElohLogosFounded, FOUNDED
            { 22750, 172 },       // ItemElohLogosFragile, FRAGILE
            { 22751, 173 },       // ItemElohLogosFrequency, FREQUENCY
            { 22752, 174 },       // ItemElohLogosFrom, FROM
            { 22754, 175 },       // ItemElohLogosGalaxy, GALAXY
            { 22755, 176 },       // ItemElohLogosGaming_hall, GAMING_HALL
            { 22756, 177 },       // ItemElohLogosGather, GATHER
            { 22757, 178 },       // ItemElohLogosGirl, GIRL
            { 22758, 179 },       // ItemElohLogosGoing, GOING
            { 22759, 180 },       // ItemElohLogosGood, GOOD
            { 22761, 182 },       // ItemElohLogosGreeting, GREETING
            { 22762, 183 },       // ItemElohLogosGrowth, GROWTH
            { 22764, 184 },       // ItemElohLogosHappy, HAPPY
            { 22765, 185 },       // ItemElohLogosHate, HATE
            { 22766, 186 },       // ItemElohLogosHave, HAVE
            { 22767, 187 },       // ItemElohLogosHearing, HEARING
            { 22768, 188 },       // ItemElohLogosHide, HIDE
            { 22770, 190 },       // ItemElohLogosHold, HOLD
            { 22771, 191 },       // ItemElohLogosHonor, HONOR
            { 22772, 192 },       // ItemElohLogosHow, HOW
            { 22773, 193 },       // ItemElohLogosIce, ICE
            { 22774, 194 },       // ItemElohLogosIf, IF
            { 22775, 195 },       // ItemElohLogosIgnorance, IGNORANCE
            { 22776, 196 },       // ItemElohLogosImbalance, IMBALANCE
            { 22777, 197 },       // ItemElohLogosImmortality, IMMORTALITY
            { 22778, 198 },       // ItemElohLogosIn, IN
            { 22779, 199 },       // ItemElohLogosInfinity, INFINITY
            { 22780, 200 },       // ItemElohLogosInfront, INFRONT
            { 22781, 201 },       // ItemElohLogosIntelligent, INTELLIGENT
            { 22782, 202 },       // ItemElohLogosIntertwined, INTERTWINED
            { 22783, 203 },       // ItemElohLogosIs, IS
            { 22784, 204 },       // ItemElohLogosIt, IT
            { 22785, 205 },       // ItemElohLogosIts, ITS
            { 22786, 206 },       // ItemElohLogosJoined, JOINED
            { 22787, 207 },       // ItemElohLogosJumbled, JUMBLED
            { 22788, 208 },       // ItemElohLogosKnowledge, KNOWLEDGE
            { 22789, 209 },       // ItemElohLogosLanguage, LANGUAGE
            { 22790, 210 },       // ItemElohLogosLeft, LEFT
            { 22791, 211 },       // ItemElohLogosLight_speed, LIGHT_SPEED
            { 22792, 212 },       // ItemElohLogosLike, LIKE
            { 22794, 214 },       // ItemElohLogosLocation, LOCATION
            { 22796, 215 },       // ItemElohLogosLogos_element, LOGOS_ELEMENT
            { 22797, 216 },       // ItemElohLogosLooking, LOOKING
            { 22798, 217 },       // ItemElohLogosLost, LOST
            { 22799, 218 },       // ItemElohLogosLove, LOVE
            { 22800, 219 },       // ItemElohLogosMan, MAN
            { 22801, 220 },       // ItemElohLogosMarket, MARKET
            { 22802, 221 },       // ItemElohLogosMay, MAY
            { 22804, 223 },       // ItemElohLogosMeet, MEET
            { 22805, 224 },       // ItemElohLogosMight, MIGHT
            { 22806, 225 },       // ItemElohLogosMine, MINE
            { 22807, 226 },       // ItemElohLogosMoon, MOON
            { 22808, 227 },       // ItemElohLogosMoon_crescent_waning, MOON_CRESCENT_WANING
            { 22809, 228 },       // ItemElohLogosMoon_crescent_waxing, MOON_CRESCENT_WAXING
            { 22810, 229 },       // ItemElohLogosMoon_full, MOON_FULL
            { 22811, 230 },       // ItemElohLogosMoon_gibbous_waning, MOON_GIBBOUS_WANING
            { 22812, 231 },       // ItemElohLogosMoon_gibbous_waxing, MOON_GIBBOUS_WAXING
            { 22813, 232 },       // ItemElohLogosMoon_half, MOON_HALF_WAXING
            { 22814, 233 },       // ItemElohLogosMoon_new, MOON_NEW
            { 22815, 234 },       // ItemElohLogosMother, MOTHER
            { 22816, 235 },       // ItemElohLogosMountain, MOUNTAIN
            { 22817, 236 },       // ItemElohLogosMouth, MOUTH
            { 22818, 237 },       // ItemElohLogosMultiply, MULTIPLY
            { 22819, 238 },       // ItemElohLogosMuseum, MUSEUM
            { 22820, 239 },       // ItemElohLogosNeph, NEPH
            { 22821, 240 },       // ItemElohLogosNight, NIGHT
            { 22822, 241 },       // ItemElohLogosNone, NONE
            { 22823, 242 },       // ItemElohLogosNor, NOR
            { 22824, 243 },       // ItemElohLogosNorth, NORTH
            { 22825, 244 },       // ItemElohLogosNose, NOSE
            { 22826, 245 },       // ItemElohLogosNot, NOT
            { 22827, 246 },       // ItemElohLogosNothing, NOTHING
            { 22828, 247 },       // ItemElohLogosNow, NOW
            { 22829, 248 },       // ItemElohLogosOf, OF
            { 22830, 249 },       // ItemElohLogosOn, ON
            { 22831, 250 },       // ItemElohLogosOnly, ONLY
            { 22832, 251 },       // ItemElohLogosOpen, OPEN
            { 22833, 252 },       // ItemElohLogosOpposite, OPPOSITE
            { 22834, 253 },       // ItemElohLogosOr, OR
            { 22835, 254 },       // ItemElohLogosOrion, ORION
            { 22836, 255 },       // ItemElohLogosOthers, OTHERS
            { 22837, 256 },       // ItemElohLogosOurs, OURS
            { 22838, 257 },       // ItemElohLogosOut, OUT
            { 22839, 258 },       // ItemElohLogosPage, PAGE
            { 22840, 259 },       // ItemElohLogosParent, PARENT
            { 22842, 260 },       // ItemElohLogosPeople, PEOPLE
            { 22843, 261 },       // ItemElohLogosPermit, PERMIT
            { 22844, 262 },       // ItemElohLogosPerson, PERSON
            { 22845, 263 },       // ItemElohLogosPhi, PHI
            { 22846, 264 },       // ItemElohLogosPickup, PICKUP
            { 22847, 265 },       // ItemElohLogosPlace, PLACE
            { 22848, 266 },       // ItemElohLogosPlanet, PLANET
            { 22849, 267 },       // ItemElohLogosPleased, PLEASED
            { 22850, 268 },       // ItemElohLogosPonder, PONDER
            { 22851, 269 },       // ItemElohLogosProgress, PROGRESS
            { 22852, 270 },       // ItemElohLogosQuestion, QUESTION
            { 22853, 271 },       // ItemElohLogosRain, RAIN
            { 22854, 272 },       // ItemElohLogosRead, READ
            { 22855, 273 },       // ItemElohLogosRebirth, REBIRTH
            { 22856, 274 },       // ItemElohLogosRelease, RELEASE
            { 22857, 275 },       // ItemElohLogosRepair, REPAIR
            { 22858, 276 },       // ItemElohLogosRepeat, REPEAT
            { 22859, 277 },       // ItemElohLogosReply, REPLY
            { 22860, 278 },       // ItemElohLogosRetail, RETAIL
            { 22861, 279 },       // ItemElohLogosRight, RIGHT
            { 22862, 280 },       // ItemElohLogosRocket, ROCKET
            { 22863, 281 },       // ItemElohLogosSad, SAD
            { 22864, 282 },       // ItemElohLogosSearch, SEARCH
            { 22865, 283 },       // ItemElohLogosSecular, SECULAR
            { 22866, 284 },       // ItemElohLogosSeeds, SEEDS
            { 22867, 285 },       // ItemElohLogosSell, SELL
            { 22868, 286 },       // ItemElohLogosShip, SHIP
            { 22869, 287 },       // ItemElohLogosShrine, SHRINE
            { 22870, 288 },       // ItemElohLogosShrinking, SHRINKING
            { 22871, 289 },       // ItemElohLogosSign, SIGN
            { 22872, 290 },       // ItemElohLogosSigns, SIGNS
            { 22873, 291 },       // ItemElohLogosSimilar, SIMILAR
            { 22874, 292 },       // ItemElohLogosSine, SINE
            { 22875, 293 },       // ItemElohLogosSky, SKY
            { 22876, 294 },       // ItemElohLogosSmall, SMALL
            { 22877, 295 },       // ItemElohLogosSmelling, SMELLING
            { 22878, 296 },       // ItemElohLogosSnow, SNOW
            { 22879, 297 },       // ItemElohLogosSolarsys, SOLARSYS
            { 22880, 298 },       // ItemElohLogosSouth, SOUTH
            { 22881, 299 },       // ItemElohLogosSpeaking, SPEAKING
            { 22882, 300 },       // ItemElohLogosSpirit, SPIRIT
            { 22884, 301 },       // ItemElohLogosStore, STORE
            { 22885, 302 },       // ItemElohLogosStrong, STRONG
            { 22886, 303 },       // ItemElohLogosSubmit, SUBMIT
            { 22887, 304 },       // ItemElohLogosSubtract, SUBTRACT
            { 22888, 305 },       // ItemElohLogosSummer, SUMMER
            { 22889, 306 },       // ItemElohLogosSun, SUN
            { 22890, 307 },       // ItemElohLogosSunrise, SUNRISE
            { 22891, 308 },       // ItemElohLogosSunset, SUNSET
            { 22892, 309 },       // ItemElohLogosSurround, SURROUND
            { 22893, 310 },       // ItemElohLogosSymbol, SYMBOL
            { 22894, 311 },       // ItemElohLogosSymbology, SYMBOLOGY
            { 22895, 313 },       // ItemElohLogosThat, THAT
            { 22896, 314 },       // ItemElohLogosThe, THE
            { 22897, 315 },       // ItemElohLogosTheater, THEATER
            { 22898, 316 },       // ItemElohLogosTheirs, THEIRS
            { 22899, 317 },       // ItemElohLogosThem, THEM
            { 22900, 318 },       // ItemElohLogosThere, THERE
            { 22901, 319 },       // ItemElohLogosThese, THESE
            { 22902, 320 },       // ItemElohLogosThing, THING
            { 22903, 321 },       // ItemElohLogosThose, THOSE
            { 22904, 322 },       // ItemElohLogosThrough, THROUGH
            { 22905, 323 },       // ItemElohLogosTired, TIRED
            { 22906, 324 },       // ItemElohLogosToday, TODAY
            { 22907, 325 },       // ItemElohLogosTofar, TOFAR
            { 22908, 326 },       // ItemElohLogosTogether, TOGETHER
            { 22909, 327 },       // ItemElohLogosTomorrow, TOMORROW
            { 22910, 328 },       // ItemElohLogosTouch, TOUCH
            { 22911, 329 },       // ItemElohLogosTough, TOUGH
            { 22912, 330 },       // ItemElohLogosTrade, TRADE
            { 22913, 331 },       // ItemElohLogosTransform, TRANSFORM
            { 22914, 332 },       // ItemElohLogosTree, TREE
            { 22915, 333 },       // ItemElohLogosTrue, TRUE
            { 22916, 334 },       // ItemElohLogosUnfriendly, UNFRIENDLY
            { 22917, 335 },       // ItemElohLogosUnintelligent, UNINTELLIGENT
            { 22918, 336 },       // ItemElohLogosUnit_of_value, UNIT_OF_VALUE
            { 22919, 337 },       // ItemElohLogosUp, UP
            { 22921, 339 },       // ItemElohLogosValley, VALLEY
            { 22922, 340 },       // ItemElohLogosVehicle, VEHICLE
            { 22923, 341 },       // ItemElohLogosVictory, VICTORY
            { 22924, 342 },       // ItemElohLogosVisit, VISIT
            { 22925, 343 },       // ItemElohLogosWar, WAR
            { 22926, 344 },       // ItemElohLogosWas, WAS
            { 22927, 345 },       // ItemElohLogosWas_not, WAS_NOT
            { 22928, 346 },       // ItemElohLogosWater, WATER
            { 22929, 347 },       // ItemElohLogosWeak, WEAK
            { 22930, 348 },       // ItemElohLogosEveryone, EVERYONE
            { 22931, 349 },       // ItemElohLogosWest, WEST
            { 22932, 350 },       // ItemElohLogosWhat, WHAT
            { 22933, 351 },       // ItemElohLogosWheel, WHEEL
            { 22934, 352 },       // ItemElohLogosWhen, WHEN
            { 22935, 353 },       // ItemElohLogosWhere, WHERE
            { 22936, 354 },       // ItemElohLogosWhich, WHICH
            { 22937, 355 },       // ItemElohLogosWill_be, WILL_BE
            { 22938, 356 },       // ItemElohLogosWill_not, WILL_NOT
            { 22939, 357 },       // ItemElohLogosWinter, WINTER
            { 22940, 358 },       // ItemElohLogosWoman, WOMAN
            { 22941, 359 },       // ItemElohLogosWork, WORK
            { 22942, 360 },       // ItemElohLogosWriting, WRITING
            { 22943, 361 },       // ItemElohLogosYear, YEAR
            { 22944, 362 },       // ItemElohLogosYears, YEARS
            { 22945, 363 },       // ItemElohLogosYesterday, YESTERDAY
            { 22946, 364 },       // ItemElohLogosYou, YOU
            { 22947, 365 },       // ItemElohLogosYou_all, YOU_ALL
            { 22948, 366 },       // ItemElohLogosYours, YOURS
            { 26700, 373 },       // ItemElohLogosNear, NEAR
            { 26692, 384 },       // ItemElohLogosTeleport, TELEPORT
            { 26699, 393 },       // ItemElohLogosAdd, ADD
            { 22731, 398 },       // ItemElohLogosEthical_hedonism, ETHICAL_HEDONISM
            { 26718, 400 },       // ItemElohLogosJump, JUMP
            { 26836, 402 },       // ItemElohLogosFunction, FUNCTION
            { 26835, 404 },       // ItemElohLogosAppearance, APPEARANCE
            { 26837, 407 },       // ItemElohLogosLightness, LIGHTNESS
            { 30407, 408 },       // ItemElohLogosEarth, EARTH
        };

        /// <summary>The Logos an item class teaches, or 0 for a class that is no Logos stone.</summary>
        public static uint LogosOf(uint itemClassId) => LogosOfClass.TryGetValue(itemClassId, out var logosId) ? logosId : 0;

        /// <summary>
        /// Every Logos the client knows: generated.client.logosstone.lookup, Logos id to (icon,
        /// Tabula slot, texture index), 390 ids from 1 to 408 with gaps. The Tabula window lists
        /// these, logosstonelanguage names them, and an id outside them is shown as a missing
        /// translation. 29 have no stone in the table above: 312, and most of 368 to 405.
        /// </summary>
        public static readonly HashSet<uint> KnownLogosIds = new HashSet<uint>
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16,
            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46,
            47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 63, 64, 65,
            66, 67, 68, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81,
            82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96,
            97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
            112, 113, 114, 115, 116, 117, 118, 119, 121, 122, 123, 124, 125, 126, 127,
            128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142,
            143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 155, 156, 157, 158,
            159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173,
            174, 175, 176, 177, 178, 179, 180, 182, 183, 184, 185, 186, 187, 188, 190,
            191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205,
            206, 207, 208, 209, 210, 211, 212, 214, 215, 216, 217, 218, 219, 220, 221,
            223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237,
            238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252,
            253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267,
            268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282,
            283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297,
            298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312,
            313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327,
            328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 339, 340, 341, 342, 343,
            344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358,
            359, 360, 361, 362, 363, 364, 365, 366, 368, 369, 370, 371, 372, 373, 374,
            375, 376, 377, 378, 380, 381, 382, 383, 384, 386, 387, 388, 389, 390, 391,
            392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 404, 405, 407, 408
        };

        /// <summary>Whether the client has a Logos by this id.</summary>
        public static bool IsKnownLogos(uint logosId) => KnownLogosIds.Contains(logosId);
    }
}
