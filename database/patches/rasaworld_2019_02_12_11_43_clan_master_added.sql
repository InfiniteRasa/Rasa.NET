INSERT INTO `rasaworld`.`creatures` (`dbId`, `comment`, `classId`, `faction`, `level`, `maxHitPoints`, `nameId`, `run_speed`, `walk_speed`, `action1`)
	VALUES ('30', 'Clan Master', '3846', '1', '5', '120', '8838', '0', '0', '2');

INSERT INTO `rasaworld`.`creature_stats` (`creatureDbId`, `body`, `mind`, `spirit`, `health`, `armor`)
	VALUES ('30', '15', '15', '15', '120', '70');
	
INSERT INTO `rasaworld`.`creature_appearance` (`creature_id`, `slot_id`, `class_id`, `color`) VALUES
('30', '14', '9781', '4278655809'),
('30', '17', '24008', '4286690539'),
('30', '13', '6271', '0'),
('30', '15', '4023', '4294934528'),
('30', '16', '4022', '4294934528'),
('30', '2', '4021', '4294934528');

INSERT INTO `rasaworld`.`spawnpool` (`dbId`, `respawnTime`, `posX`, `posY`, `posZ`, `orientation`, `contextId`, `creatureId1`, `creatureMinCount1`, `creatureMaxCount1`, `creatureId2`, `creatureId3`, `creatureId4`, `creatureId5`, `creatureId6`)
	VALUES ('30', '20', '810.12', '294.11', '376.28', '4.4', '1220', '30', '1', '1', '0', '0', '0', '0', '0');

INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2019_02_12_11_43_clan_master_added');