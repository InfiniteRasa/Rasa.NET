ALTER TABLE `character_titles`
	ALTER `titleId` DROP DEFAULT;
ALTER TABLE `character_titles`
	CHANGE COLUMN `titleId` `titleId` INT(11) UNSIGNED NOT NULL AFTER `characterSlot`;