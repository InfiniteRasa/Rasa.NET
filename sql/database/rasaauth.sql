-- Account table
CREATE TABLE `account` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `email` varchar(255) NOT NULL,
  `username` varchar(64) NOT NULL,
  `password` varchar(64) NOT NULL,
  `salt` varchar(40) NOT NULL,
  `level` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `last_ip` varchar(45) NOT NULL DEFAULT '0.0.0.0',
  `last_server_id` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `last_login` datetime DEFAULT NULL,
  `join_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `locked` bit(1) NOT NULL DEFAULT b'0',
  `validated` bit(1) NOT NULL DEFAULT b'0',
  `validation_token` varchar(40) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email_UNIQUE` (`email`),
  UNIQUE KEY `username_UNIQUE` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
