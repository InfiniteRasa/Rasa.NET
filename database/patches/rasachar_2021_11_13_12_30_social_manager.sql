CREATE TABLE `rasachar`.`friends` (
	`account_id` INT UNSIGNED NOT NULL,
	`friend_account_id` INT UNSIGNED NOT NULL
);

CREATE TABLE `rasachar`.`ignored` (
	`account_id` INT UNSIGNED NOT NULL,
	`ignored_account_id` INT UNSIGNED NOT NULL
);

ALTER TABLE `rasachar`.`character`
	ADD COLUMN `is_online` BIT NOT NULL AFTER `last_pvp_clan`;

INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUE ('rasachar_2021_11_13_12_30_social_manager');