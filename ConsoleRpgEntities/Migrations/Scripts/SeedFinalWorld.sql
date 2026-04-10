-- ================================================================
-- Week 15 Final World Seed
-- ================================================================
-- Builds the complete world for the Week 15 final project on top of
-- everything that W12 + W13 + W14 seeded. Creates a town area, a maze
-- of "twisting wilds," and a dungeon beyond a locked door.
--
-- WORLD LAYOUT:
--
--   [Ancient Library (14)]     [Training Grounds (15)]
--              |                         |
--         [stone archway]           [open path]
--              |                         |
--     [Northern Gate (13)] ============ (open) ============
--                 \                                         \
--                  \                                         \
--                   [Town Square (8)] --- START HERE
--                         |
--                    [open path]
--                         |
--                  [Forest Edge (9)]  <-- Gray Shadow (Wolf)
--                         |
--                    [overgrown]
--                         |
--                 [Twisting Wilds (10)] <-- Grubnak (Goblin)
--                    /    |    \
--                 [Tangled Grove (16)]
--                 [Hollow Thicket (17)]
--                 (maze rooms loop around)
--                         |
--                  [Ironbound Door LOCKED]
--                  (requires dungeon-key)
--                         |
--                 [Trapped Vault (11)]  <-- Rattlebones (Skeleton)
--                         |
--                  [Marble Panel SECRET]
--                         |
--                 [Hidden Shrine (12)]
--
-- MONSTERS:
--   - Grubnak the Goblin (from W13) -- now placed in Twisting Wilds
--   - Gray Shadow (Wolf, PackSize=2) -- Forest Edge
--   - Rattlebones (Skeleton, HasArmor=true) -- Trapped Vault
--
-- CONTAINER ID RESERVATIONS:
--   1    = Player Inventory        (from W12)
--   2    = Player Equipment        (from W12)
--   3-6  = Chests                  (from W13)
--   7    = Grubnak MonsterLoot     (from W13)
--   8-17 = Rooms for W15           (new in this script)
--   18   = Gray Shadow MonsterLoot (new)
--   19   = Rattlebones MonsterLoot (new)
--
-- PLAYER STARTER KIT:
--   Added to the player's Inventory (Container Id 1):
--     - Training Sword (Weapon +5)
--     - Hardened Leather Vest (Armor +3)
--     - Bent Hairpin (lockpick, KeyId NULL)
--     - 2x Traveler's Ration (Heal +12, Uses=2)
--     - Waterskin (Heal +8, Uses=3)
--
-- Abilities carried over from W12 SeedInitialData: the player already
-- knows "Power Shove" (the ShoveAbility from W10).
-- ================================================================


-- ----------------------------------------------------------------
-- STEP 1: Rooms (Container rows with ContainerType = 'Room')
-- ----------------------------------------------------------------
-- Using Room_Description because Chest (W13) already claimed "Description"
-- on the Containers table and EF disambiguates TPH columns by subclass.
-- ----------------------------------------------------------------
SET IDENTITY_INSERT Containers ON;

-- Town core
INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (8, 'Room',
    'Town Square',
    'The heart of the village. Cobblestone pavers radiate from a bubbling fountain where merchants haggle and children chase pigeons. A notice board stands in the middle advertising reward for daring adventurers, and well-traveled roads lead in every direction.',
    0, 0);

-- Forest path
INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (9, 'Room',
    'Forest Edge',
    'Ancient oaks loom overhead. A narrow dirt trail vanishes south into the overgrowth. The air smells of pine and something older.',
    0, -1);

-- Maze core + branches
INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (10, 'Room',
    'Twisting Wilds',
    'The path splinters into three tangled trails, each indistinguishable from the others. Roots grasp at your boots and the canopy blots out the sky. You are in a maze of twisty little passages, all alike.',
    0, -2);

INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (16, 'Room',
    'Tangled Grove',
    'More twisting paths that look alarmingly similar to the last clearing. You are in a maze of twisty little passages, all alike. Was that west, or north?',
    -1, -2);

INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (17, 'Room',
    'Hollow Thicket',
    'A patch of darker ground where nothing grows. The trails here seem to double back on themselves. You are in a maze of twisty little passages, all alike.',
    1, -2);

-- Dungeon proper
INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (11, 'Room',
    'Trapped Vault',
    'Stacks of tarnished coins shimmer under torchlight. Thin wires catch the light along the walls, and a chill emanates from the far corner. This place has not been disturbed in decades.',
    0, -3);

INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (12, 'Room',
    'Hidden Shrine',
    'A forgotten shrine of polished marble, impossibly clean. Soft starlight filters in from nowhere at all. At the center stands an altar, and on it rests something you should not take lightly.',
    0, -4);

-- Town outskirts
INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (13, 'Room',
    'Northern Gate',
    'A massive iron-bound gate marks the northern edge of town. Guards in burnished mail patrol the ramparts above. Beyond the gate, you can faintly hear the clash of practice weapons to the east.',
    0, 1);

INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (14, 'Room',
    'Ancient Library',
    'Towering bookshelves sag under the weight of a thousand dusty tomes. A single librarian shuffles between the stacks, muttering to herself. Somewhere in here is the history of whatever lies south.',
    -1, 1);

INSERT INTO Containers (Id, ContainerType, Name, Room_Description, X, Y)
VALUES (15, 'Room',
    'Training Grounds',
    'Packed dirt rings scarred by countless bootprints. Weapon racks line the eastern fence. A straw dummy hangs from a post, thoroughly abused.',
    1, 1);

SET IDENTITY_INSERT Containers OFF;

-- ----------------------------------------------------------------
-- STEP 2: Wire up the room navigation
-- ----------------------------------------------------------------
-- Town Square has exits to the Northern Gate (N) and Forest Edge (S).
-- Students are encouraged to extend E/W with their own shops.

UPDATE Containers SET NorthRoomId = 13, SouthRoomId = 9 WHERE Id = 8;  -- Town Square
UPDATE Containers SET NorthRoomId = 8, SouthRoomId = 10 WHERE Id = 9;  -- Forest Edge
UPDATE Containers SET NorthRoomId = 9, SouthRoomId = 11, EastRoomId = 17, WestRoomId = 16 WHERE Id = 10; -- Twisting Wilds

-- Maze loop: the key feature of the wilds is that going west from the
-- wilds leads to the Grove, but going east from the Grove leads to the
-- Thicket, and going east from the Thicket brings you back to the wilds.
-- Classic Adventure "maze of twisty little passages" behavior.
UPDATE Containers SET EastRoomId = 17, WestRoomId = 10 WHERE Id = 16;  -- Tangled Grove
UPDATE Containers SET EastRoomId = 10, WestRoomId = 16 WHERE Id = 17;  -- Hollow Thicket

UPDATE Containers SET NorthRoomId = 10, SouthRoomId = 12 WHERE Id = 11; -- Trapped Vault
UPDATE Containers SET NorthRoomId = 11 WHERE Id = 12;                   -- Hidden Shrine

UPDATE Containers SET SouthRoomId = 8, EastRoomId = 15, WestRoomId = 14 WHERE Id = 13; -- Northern Gate
UPDATE Containers SET EastRoomId = 13 WHERE Id = 14;                                    -- Ancient Library
UPDATE Containers SET WestRoomId = 13 WHERE Id = 15;                                    -- Training Grounds

-- ----------------------------------------------------------------
-- STEP 3: Doors (state-bearing passages between rooms)
-- ----------------------------------------------------------------
SET IDENTITY_INSERT Doors ON;

INSERT INTO Doors
    (Id, Name, RoomAId, RoomBId,
     IsLocked, IsTrapped, IsPickable, RequiredKeyId,
     TrapDamage, TrapDisarmed, IsSecret, IsDiscovered)
VALUES
    -- Id 1: Ironbound Door - locked, requires the dungeon-key that Grubnak carries
    (1, 'Ironbound Door', 10, 11,
        1, 0, 0, 'dungeon-key',
        0, 0, 0, 0),

    -- Id 2: Marble Panel - secret door to the Hidden Shrine
    (2, 'Marble Panel', 11, 12,
        0, 0, 0, NULL,
        0, 0, 1, 0),

    -- Id 3: Stone Archway - trapped passage between Northern Gate and Library
    (3, 'Stone Archway', 13, 14,
        0, 1, 1, NULL,
        8, 0, 0, 0);

SET IDENTITY_INSERT Doors OFF;

-- ----------------------------------------------------------------
-- STEP 4: Place Grubnak (the W13 goblin) into the maze
-- ----------------------------------------------------------------
UPDATE Monsters SET CurrentRoomId = 10 WHERE Id = 1;

-- ----------------------------------------------------------------
-- STEP 5: New monsters and their loot containers
-- ----------------------------------------------------------------
SET IDENTITY_INSERT Containers ON;
INSERT INTO Containers (Id, ContainerType) VALUES (18, 'MonsterLoot');
INSERT INTO Containers (Id, ContainerType) VALUES (19, 'MonsterLoot');
SET IDENTITY_INSERT Containers OFF;

SET IDENTITY_INSERT Monsters ON;

-- Gray Shadow: a wolf in the Forest Edge with a pack size of 2
INSERT INTO Monsters
    (Id, Name, Health, AggressionLevel, MonsterType, CurrentRoomId, LootId, IsLooted, PackSize)
VALUES
    (2, 'Gray Shadow', 25, 7, 'Wolf', 9, 18, 0, 2);

-- Rattlebones: an armored skeleton in the Trapped Vault
INSERT INTO Monsters
    (Id, Name, Health, AggressionLevel, MonsterType, CurrentRoomId, LootId, IsLooted, HasArmor)
VALUES
    (3, 'Rattlebones', 40, 5, 'Skeleton', 11, 19, 0, 1);

SET IDENTITY_INSERT Monsters OFF;

-- Gray Shadow's drops
INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, Attack, Category)
VALUES ('Wolf Fang Dagger', 'A curved dagger carved from a massive wolf fang.', 'Weapon', 1.00, 45, 18, 3, 'Melee');

INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, EffectType, EffectAmount, Uses)
VALUES ('Forest Herb Poultice', 'A crushed bundle of fresh healing herbs.', 'Consumable', 0.30, 20, 18, 'Heal', 18, 1);

-- Rattlebones' drops
INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, Attack, Category)
VALUES ('Pitted Longsword', 'An ancient blade, pitted with age but still deadly.', 'Weapon', 3.00, 60, 19, 7, 'Melee');

INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, Defense, Slot)
VALUES ('Boneplate Breastplate', 'Heavy armor crafted from fused bones.', 'Armor', 7.00, 120, 19, 6, 'Body');

-- ----------------------------------------------------------------
-- STEP 6: Player starter kit (added to Inventory Id 1)
-- ----------------------------------------------------------------
INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, Attack, Category)
VALUES ('Training Sword', 'A well-balanced blade used by town recruits.', 'Weapon', 3.00, 40, 1, 5, 'Melee');

INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, Defense, Slot)
VALUES ('Hardened Leather Vest', 'Stiffened leather armor, comfortable enough.', 'Armor', 4.00, 30, 1, 3, 'Body');

INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, EffectType, EffectAmount, Uses)
VALUES ('Waterskin', 'Filled with cool spring water.', 'Consumable', 0.50, 5, 1, 'Heal', 8, 3);

INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, EffectType, EffectAmount, Uses)
VALUES ('Traveler''s Ration', 'Dried meat and hard biscuits.', 'Consumable', 0.50, 10, 1, 'Heal', 12, 2);

INSERT INTO Items (Name, Description, ItemType, Weight, Value, ContainerId, KeyId)
VALUES ('Bent Hairpin', 'A thin metal tool for impromptu lockpicking.', 'KeyItem', 0.10, 5, 1, NULL);

-- ----------------------------------------------------------------
-- STEP 7: Teleport the player to Town Square
-- ----------------------------------------------------------------
UPDATE Players SET CurrentRoomId = 8 WHERE Id = 1;
