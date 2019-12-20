ALTER TABLE `rasachar`.`character_lockbox`
	CHANGE COLUMN `credits` `credits` INT(10) NOT NULL DEFAULT '0' AFTER `accountId`,
	CHANGE COLUMN `purashedTabs` `purashedTabs` INT(10) NOT NULL DEFAULT '1' AFTER `credits`;

INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUES ('rasachar_2019_12_20_14_05_changed_all_credits_to_int');