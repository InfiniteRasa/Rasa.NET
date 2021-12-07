UPDATE `rasaworld`.`teleporter` SET `type`='1' WHERE  `id`=49;
UPDATE `rasaworld`.`teleporter` SET `type`='1' WHERE  `id`=50;
UPDATE `rasaworld`.`teleporter` SET `type`='1' WHERE  `id`=51;
UPDATE `rasaworld`.`teleporter` SET `type`='1' WHERE  `id`=57;
UPDATE `rasaworld`.`teleporter` SET `type`='1' WHERE  `id`=61;
UPDATE `rasaworld`.`teleporter` SET `type`='1' WHERE  `id`=73;
UPDATE `rasaworld`.`teleporter` SET `type`='1' WHERE  `id`=156;

UPDATE `rasaworld`.`teleporter` SET `type`='2', `coord_x`='-225.353', `coord_y`='99.597', `coord_z`='-70.5246', `map_context_id`='1985' WHERE  `id`=99;
UPDATE `rasaworld`.`teleporter` SET `type`='2', `coord_x`='-81', `coord_y`='119.5', `coord_z`='640.5', `map_context_id`='1148' WHERE  `id`=249;
UPDATE `rasaworld`.`teleporter` SET `type`='2', `coord_x`='-60', `coord_y`='221.269', `coord_z`='-471', `map_context_id`='1220' WHERE  `id`=267;

INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2021_12_07_21_30_added_dropship_teleporters');