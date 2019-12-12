-- Dumping structure for table rasaworld.footlockers
CREATE TABLE IF NOT EXISTS `rasaworld`.`footlockers` (
  `id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `entity_class_id` int(11) unsigned NOT NULL DEFAULT 0,
  `map_context_id` int(10) unsigned DEFAULT NULL,
  `coord_x` float DEFAULT NULL,
  `coord_y` float DEFAULT NULL,
  `coord_z` float DEFAULT NULL,
  `orientation` float DEFAULT NULL,
  `comment` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1;

-- Dumping data for table rasaworld.footlockers: ~1 rows (approximately)
INSERT INTO `rasaworld`.`footlockers` (`id`, `entity_class_id`, `map_context_id`, `coord_x`, `coord_y`, `coord_z`, `orientation`, `comment`) VALUES
	(1, 21030, 1220, 756.3, 294.2, 379.3, 0, 'Alia Das Footlocker'),
	(2, 21030, 1220, -91.5, 221.3, -529.5, 0, 'Twin Pillars Footlocker');
	
INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2019_12_12_23_12_added_footlockers_in_wilderness');
