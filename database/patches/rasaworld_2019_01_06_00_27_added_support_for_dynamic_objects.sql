DROP TABLE IF EXISTS `rasaworld`.`teleporter`;
CREATE TABLE IF NOT EXISTS `rasaworld`.`teleporter` (
  `id` int(11) unsigned NOT NULL,
  `entity_class_id` int(11) unsigned NOT NULL,
  `type` int(11) unsigned NOT NULL,
  `nameId` int(11) unsigned NOT NULL,
  `description` varchar(50) NOT NULL,
  `coord_x` float NOT NULL,
  `coord_y` float NOT NULL,
  `coord_z` float NOT NULL,
  `rotation` float NOT NULL,
  `map_context_id` int(11) unsigned NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='table with all local_teleporters, waypoints and wormholelocations';


INSERT INTO `rasaworld`.`teleporter` (`id`, `entity_class_id`, `type`, `nameId`, `description`, `coord_x`, `coord_y`, `coord_z`, `rotation`, `map_context_id`) VALUES
	(42, 25651, 2, 57, 'Waypoint Alia Das', 807.734, 294.105, 391.145, 1.5613, 1220);
	
INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2019_01_06_00_27_added_support_for_dynamic_objects');