ALTER TABLE `rasaworld`.`teleporter` ALTER `rotation` DROP DEFAULT;
ALTER TABLE `rasaworld`.`teleporter` CHANGE COLUMN `rotation` `orientation` FLOAT NOT NULL AFTER `coord_z`;

ALTER TABLE `rasaworld`.`spawnpool` ALTER `rotation` DROP DEFAULT;
ALTER TABLE `rasaworld`.`spawnpool` CHANGE COLUMN `rotation` `orientation` FLOAT NOT NULL AFTER `posZ`;


INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2019_01_23_13_43_orientation_fix');
