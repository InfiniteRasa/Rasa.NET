CREATE TABLE IF NOT EXISTS `itemtemplate_resistance` (
  `itemtemplate_id` int(11) DEFAULT NULL,
  `resistance_type` tinyint(4) DEFAULT NULL,
  `resistance_value` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

INSERT INTO `itemtemplate_resistance` (`itemtemplate_id`, `resistance_type`, `resistance_value`) VALUES
('13066', '2', '2'),
('13096', '3', '3'),
('13126', '4', '4'),
('13156', '5', '5'),
('13156', '6', '6'),
('13186', '7', '7'),
('13186', '13', '13');

INSERT INTO `applied_patches` (`patch_name`) VALUES ('rasaworld_2018_11_29_15_26_added_itemtemplate_resistance');