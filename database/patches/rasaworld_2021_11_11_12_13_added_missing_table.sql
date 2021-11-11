CREATE TABLE IF NOT EXISTS `rasaworld`.`control_point` (
  `control_point_id` int(11) NOT NULL,
  PRIMARY KEY (`control_point_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2021_11_11_12_13_added_missing_table');
