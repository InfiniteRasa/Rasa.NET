CREATE TABLE IF NOT EXISTS `character_options` (
  `character_id` int(11) DEFAULT NULL,
  `option_id` int(11) DEFAULT NULL,
  `value` varchar(50) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

INSERT INTO `applied_patches` (`patch_name`) VALUES ('rasachar_2018_11_26_16_58_added_character_options');