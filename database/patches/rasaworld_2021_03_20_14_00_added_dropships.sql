-- Update spwanpool table
UPDATE `rasaworld`.`spawnpool` SET `animType`='1' WHERE  `dbId`=1;
UPDATE `rasaworld`.`spawnpool` SET `animType`='1' WHERE  `dbId`=3;
UPDATE `rasaworld`.`spawnpool` SET `animType`='2' WHERE  `dbId`=10;
UPDATE `rasaworld`.`spawnpool` SET `animType`='2' WHERE  `dbId`=16;

UPDATE `rasaworld`.`spawnpool` SET `creatureMinCount1`='2', `creatureMaxCount1`='5' WHERE  `dbId`=2;

-- Update creature table
UPDATE `rasaworld`.`creatures` SET `classId`='4313' WHERE  `dbId`=1;
UPDATE `rasaworld`.`creatures` SET `classId`='9244' WHERE  `dbId`=2;
UPDATE `rasaworld`.`creatures` SET `classId`='20757' WHERE  `dbId`=3;

UPDATE `rasaworld`.`creatures` SET `maxHitPoints`='10000' WHERE  `dbId`=1;
UPDATE `rasaworld`.`creatures` SET `maxHitPoints`='12000' WHERE  `dbId`=2;
UPDATE `rasaworld`.`creatures` SET `maxHitPoints`='8000' WHERE  `dbId`=3;

INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2021_03_20_14_00_added_dropships');