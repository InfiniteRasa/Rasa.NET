CREATE TABLE `rasachar`.`character_teleporters` (
	`character_id` INT UNSIGNED NULL,
	`waypoint_id` INT UNSIGNED NULL
)
COMMENT='holds data about all waypoints and wormholes that character gained'
COLLATE='latin1_swedish_ci'
ENGINE=InnoDB
;

INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUES ('rasachar_2019_01_06_21_04_added_character_teleporters');
