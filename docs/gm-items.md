# GM item list

Every item template in the world database, 30,225 of them, with the command that creates it. Generated from a world database built by the migrations at `67311835`, with names from the client's own language tables, so a new or changed item template means regenerating these pages rather than editing them.

## The command

```
.giveitem <itemTemplateId> [quantity]
```

- **Admin** (account level 10). See [GM commands](gm-commands.md) for levels and how to grant one.
- The item goes into **your own** inventory; there is no form that gives to another player.
- With no quantity you get a full stack: the item class's stack size, which is 1 for gear. Leave the quantity off for gear; give one for ammo, consumables and materials when you want less than a stack.
- You are recorded as the item's crafter.
- Character Unique is not checked on this path, so `.giveitem` will give a second copy of a unique item.
- Every id on these pages is in the table; an id that is not makes the command fail.

## Finding an item

Search a page for the name as the game shows it, or for part of the internal class name. Most names have many templates: the same piece exists for each level band, quality, variant and requirement. The **Class** column tells them apart - `Armor_T1_MotorAssist_V02_CMN_Vest_23_to_27` is tier 1, variant 2, common, for levels 23 to 27.

| Column | Meaning |
|---|---|
| Command | What to type in chat. |
| Name | The name the client shows: the template's own name when it has one, otherwise the item class's. Where it reads "no display text", that is what the client's own table holds for the class. |
| Quality | Mission, Normal, Uncommon, Rare, Epic, Legendary or Junk. |
| Levels | The level band from the class name (`_23_to_27`), where it has one. |
| Req. level | The minimum character level the template itself requires, where it has one. |
| Flags | Unique (character unique), Acct unique, BoE (bind on equip), Bound (bound to the character), No trade. |
| Class | The internal item class name. |

## Pages

| Page | Templates | Names |
|---|---|---|
| [Weapons](gm-items/weapons.md) | 4,857 | 295 |
| [Tools](gm-items/tools.md) | 700 | 9 |
| [Armor: helmets](gm-items/armor-helmets.md) | 2,855 | 45 |
| [Armor: vests](gm-items/armor-vests.md) | 2,816 | 40 |
| [Armor: legs](gm-items/armor-legs.md) | 2,781 | 35 |
| [Armor: gloves](gm-items/armor-gloves.md) | 2,808 | 44 |
| [Armor: boots](gm-items/armor-boots.md) | 2,783 | 37 |
| [Shields](gm-items/shields.md) | 665 | 201 |
| [Clothing and costumes](gm-items/clothing.md) | 1,680 | 384 |
| [Other equipment](gm-items/equipment-other.md) | 15 | 12 |
| [Consumables and ammo](gm-items/consumables.md) | 267 | 250 |
| [Crafting: recipes and schematics](gm-items/crafting-recipes.md) | 5,007 | 156 |
| [Crafting: materials and components](gm-items/crafting-materials.md) | 500 | 499 |
| [Mission items](gm-items/mission.md) | 1,022 | 820 |
| [Miscellaneous](gm-items/misc.md) | 711 | 695 |
| [Test, NPC and unused](gm-items/internal.md) | 758 | 299 |
| **Total** | **30,225** | |
