# Reference panels

Picture sheets of the game's models, rendered from the 1.16.5.0 client's own assets. They are for seeing what an id looks like before you put it on a creature, an NPC or in a migration. Each panel is a PNG image.

## NPCs

| Panel | What it shows |
|---|---|
| [npc-outfits.png](npc-outfits.png) | Every NPC clothing set (Soldier, Sapper, Stealth, Medic, Ranger, Commando, Corman, Officer 1 to 4, Retread, Refugee 1 to 4), the plain helmets, special and reward headwear, boxing gear, the holiday hats and Halloween masks, and the hybrid Brann, Forean and Thrax heads. |
| [npc-armour-looks.png](npc-armour-looks.png) | Every armour look a human NPC or player can wear: Recruit, Motor Assist 01 to 11, Reflective 1 to 5, Hazmat 1 to 5, Bio 1 to 6, Graviton 1 to 6, Mech 1 to 6 and Stealth 1 to 6, plus AFS Police, Game Master and General (British). |
| [npc-ambient-figures.png](npc-ambient-figures.png) | The posed ambient figure classes: people drinking, sitting, sleeping, repairing, operating consoles or lying injured, plus the sick Foreans, the drill sergeant and the construction mech. |

The outfit and armour panels show each look on a male figure (M) and a female figure (F), standing in a T-pose. Under each pair:

- The first line is the look's name.
- The `suit` line gives the model ids for the male and female versions. A `+` joins the models that make up one look.
- The yellow lines are the client class names that use the look. The number in brackets is how many classes match each name pattern. A class name ending in `_*` stands for its parts (`_Torso`, `_Legs`, `_Boots`, `_Gloves` and so on).

On the ambient panel, each figure is labelled with its class id and its client class name.

## Notes

- **Colour:** each look is shown untinted, as painted. On an NPC, the hue in `creature_appearance` is applied over it. The client's default hue, `0xFF000000`, leaves the colours as painted.
- **NPC clothing helmets:** `NPC_Clothing_Officer_3_Helmet` is a beret. For a real helmet, use the armour sets (`Clothing_Helmet_N` is the Graviton N helmet).
