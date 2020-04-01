DROP TABLE IF EXISTS `rasachar`.`clan_lockbox`;
CREATE TABLE IF NOT EXISTS `clan_lockbox` (
  `clanId` int(10) unsigned NOT NULL,
  `credits` int(10) unsigned NOT NULL DEFAULT '0',
  `prestige` int(10) unsigned NOT NULL DEFAULT '0',
  `purchasedTabs` int(10) unsigned NOT NULL DEFAULT '1',
  PRIMARY KEY (`clanId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `rasachar`.`clan_inventory`;
CREATE TABLE IF NOT EXISTS `clan_inventory` (
  `clanid` int(10) unsigned NOT NULL,
  `slotId` int(10) NOT NULL,
  `itemId` int(10) unsigned NOT NULL, 
  UNIQUE KEY `itemId` (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE `rasachar`.`clan_ranks` (
  `id` INT NOT NULL,
  `rank_number` INT NULL,
  `title` VARCHAR(16) NULL,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `id_UNIQUE` (`id` ASC),
  UNIQUE INDEX `rank_number_UNIQUE` (`rank_number` ASC) );

INSERT INTO `rasachar`.`clan_ranks` (`id`, `rank_number`, `title`) VALUES ('1', '0', 'Grunt');
INSERT INTO `rasachar`.`clan_ranks` (`id`, `rank_number`, `title`) VALUES ('2', '1', 'Soldier');
INSERT INTO `rasachar`.`clan_ranks` (`id`, `rank_number`, `title`) VALUES ('3', '2', 'Officer');
INSERT INTO `rasachar`.`clan_ranks` (`id`, `rank_number`, `title`) VALUES ('4', '3', 'Clan Leader');

ALTER TABLE `rasachar`.`clan` 
ADD COLUMN `ispvp` TINYINT NOT NULL AFTER `created_at`,
ADD COLUMN `rank_title_0` VARCHAR(16) NOT NULL AFTER `ispvp`,
ADD COLUMN `rank_title_1` VARCHAR(16) NOT NULL AFTER `rank_title_0`,
ADD COLUMN `rank_title_2` VARCHAR(16) NOT NULL AFTER `rank_title_1`,
ADD COLUMN `rank_title_3` VARCHAR(16) NOT NULL AFTER `rank_title_2`;

SET foreign_key_checks = 0;
ALTER TABLE `rasachar`.`clan` 
CHANGE COLUMN `id` `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT ;
SET foreign_key_checks = 1;

ALTER TABLE `rasachar`.`clan_member` 
ADD COLUMN `note` VARCHAR(45) NULL AFTER `rank`;

ALTER TABLE `rasachar`.`character` 
ADD COLUMN `last_pvp_clan` TIMESTAMP NULL AFTER `created_at`;

CREATE TABLE `rasachar`.`censor_words` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `word` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`id`));

LOCK TABLES `rasachar`.`censor_words` WRITE;
/*!40000 ALTER TABLE `censor_words` DISABLE KEYS */;
INSERT INTO `rasachar`.`censor_words` (`id`, `word`) VALUES (1,'anal'),(2,'anus'),(3,'arse'),(4,'ass'),(5,'ballsack'),(6,'balls'),(7,'bastard'),(8,'bitch'),(9,'biatch'),(10,'bloody'),(11,'blowjob'),(12,'blow job'),(13,'bollock'),(14,'bollok'),(15,'boner'),(16,'boob'),(17,'bugger'),(18,'bum'),(19,'butt'),(20,'buttplug'),(21,'clitoris'),(22,'cock'),(23,'coon'),(24,'crap'),(25,'cunt'),(26,'damn'),(27,'dick'),(28,'dildo'),(29,'dyke'),(30,'fag'),(31,'feck'),(32,'fellate'),(33,'fellatio'),(34,'felching'),(35,'fuck'),(36,'f u c k'),(37,'fudgepacker'),(38,'fudge packer'),(39,'flange'),(40,'Goddamn'),(41,'God damn'),(42,'hell'),(43,'homo'),(44,'jerk'),(45,'jizz'),(46,'knobend'),(47,'knob end'),(48,'labia'),(49,'lmao'),(50,'lmfao'),(51,'muff'),(52,'nigger'),(53,'nigga'),(54,'omg'),(55,'penis'),(56,'piss'),(57,'poop'),(58,'prick'),(59,'pube'),(60,'pussy'),(61,'queer'),(62,'scrotum'),(63,'sex'),(64,'shit'),(65,'s hit'),(66,'sh1t'),(67,'slut'),(68,'smegma'),(69,'spunk'),(70,'tit'),(71,'tosser'),(72,'turd'),(73,'twat'),(74,'vagina'),(75,'wank'),(76,'whore'),(77,'wtf');
/*!40000 ALTER TABLE `censor_words` ENABLE KEYS */;
UNLOCK TABLES;

INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUE ('rasachar_2020_04_01_21_00_add_clan_data');