-- Rollback for SeedFinalWorld - removes everything the up script added
-- in reverse order to respect foreign key constraints.

-- 0. Clear chest location references and drop new W15 chests
UPDATE Containers SET LocationRoomId = NULL WHERE Id IN (3, 4, 5, 6);
DELETE FROM Items WHERE ContainerId IN (20, 21);
DELETE FROM Containers WHERE Id IN (20, 21);

-- 1. Clear player / existing monster location references that point at W15 rooms
UPDATE Players SET CurrentRoomId = NULL WHERE Id = 1;
UPDATE Monsters SET CurrentRoomId = NULL WHERE Id = 1;

-- 2. Remove starter kit items from the player's inventory
DELETE FROM Items
WHERE ContainerId = 1
  AND Name IN ('Training Sword', 'Hardened Leather Vest', 'Waterskin', 'Traveler''s Ration', 'Bent Hairpin');

-- 3. Remove items from the new monster loot containers
DELETE FROM Items WHERE ContainerId IN (18, 19);

-- 4. Remove the new monsters
DELETE FROM Monsters WHERE Id IN (2, 3);

-- 5. Remove the new monster loot containers
DELETE FROM Containers WHERE Id IN (18, 19);

-- 6. Remove doors
DELETE FROM Doors WHERE Id IN (1, 2, 3);

-- 7. Clear self-referencing room links before dropping rooms
UPDATE Containers
SET NorthRoomId = NULL, SouthRoomId = NULL, EastRoomId = NULL, WestRoomId = NULL
WHERE Id IN (8, 9, 10, 11, 12, 13, 14, 15, 16, 17);

-- 8. Drop rooms (nothing should reference them at this point)
DELETE FROM Containers WHERE Id IN (8, 9, 10, 11, 12, 13, 14, 15, 16, 17);
