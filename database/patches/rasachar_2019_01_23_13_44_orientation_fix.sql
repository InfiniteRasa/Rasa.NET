ALTER TABLE `rasachar`.`character` ALTER `rotation` DROP DEFAULT;
ALTER TABLE `rasachar`.`character` CHANGE COLUMN `rotation` `orientation` FLOAT NOT NULL AFTER `coord_z`;

INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUES ('rasachar_2019_01_23_13_44_orientation_fix');
