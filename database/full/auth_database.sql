-- Depends on: nothing
CREATE TABLE `applied_patches` (
  `patch_name` VARCHAR(128) NOT NULL,
  `apply_time` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`patch_name`)
)
ENGINE=InnoDB
DEFAULT CHARACTER SET=utf8
COLLATE = utf8_general_ci;

-- Depends on: nothing
CREATE TABLE `account` (
  `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
  `email` varchar(255) NOT NULL,
  `username` varchar(64) NOT NULL,
  `password` varchar(64) NOT NULL,
  `salt` varchar(40) NOT NULL,
  `level` tinyint(3) UNSIGNED NOT NULL DEFAULT '0',
  `last_ip` varchar(45) NOT NULL DEFAULT '0.0.0.0',
  `last_server_id` tinyint(3) UNSIGNED NOT NULL DEFAULT '0',
  `last_login` datetime DEFAULT NULL,
  `join_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `locked` bit(1) NOT NULL DEFAULT b'0',
  `validated` bit(1) NOT NULL DEFAULT b'0',
  `validation_token` varchar(40) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email_UNIQUE` (`email`),
  UNIQUE KEY `username_UNIQUE` (`username`)
)
ENGINE=InnoDB
DEFAULT CHARACTER SET=utf8
COLLATE = utf8_general_ci;