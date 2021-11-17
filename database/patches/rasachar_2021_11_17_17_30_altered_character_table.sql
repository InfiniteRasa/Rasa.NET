ALTER TABLE `rasachar`.`character`
	DROP COLUMN `is_online`;
	
INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUES ('rasachar_2021_11_17_17_30_altered_character_table');
