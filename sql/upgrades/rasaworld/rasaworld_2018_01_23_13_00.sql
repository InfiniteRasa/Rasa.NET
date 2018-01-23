ALTER TABLE `starter_items`
	CHANGE COLUMN `itemTemplateId` `classId` INT(11) NOT NULL FIRST,
	CHANGE COLUMN `classId` `itemTemplateId` INT(11) NOT NULL AFTER `classId`;